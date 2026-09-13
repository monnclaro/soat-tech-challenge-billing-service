using Application.Common.Interfaces;
using Application.Orcamentos.Gateways;
using Application.Pagamentos.UseCases.Interfaces;
using Domain.Orcamentos.Gateways;
using Domain.Pagamentos.Gateways;

namespace Application.Pagamentos.UseCases.RegistrarWebhookPagamento;

// Substitui o antigo OrcamentoWebhookController mockado do monolito de origem — aqui a
// notificação é validada (payload mínimo + assinatura HMAC) e o resultado é consultado de
// volta na API do Mercado Pago (nunca confiamos cegamente no corpo da notificação, só ela
// nos diz "algo mudou" — o estado real vem de BuscarPagamentoPorId).
public class RegistrarWebhookPagamentoUseCase : IUseCase
{
    private readonly IWebhookSignatureValidator _signatureValidator;
    private readonly IMercadoPagoGateway _mercadoPagoGateway;
    private readonly IOrcamentoGateway _orcamentoGateway;
    private readonly IPagamentoGateway _pagamentoGateway;
    private readonly IRegistrarWebhookPagamentoOutputPort _outputPort;

    public RegistrarWebhookPagamentoUseCase(
        IWebhookSignatureValidator signatureValidator,
        IMercadoPagoGateway mercadoPagoGateway,
        IOrcamentoGateway orcamentoGateway,
        IPagamentoGateway pagamentoGateway,
        IRegistrarWebhookPagamentoOutputPort outputPort)
    {
        _signatureValidator = signatureValidator;
        _mercadoPagoGateway = mercadoPagoGateway;
        _orcamentoGateway = orcamentoGateway;
        _pagamentoGateway = pagamentoGateway;
        _outputPort = outputPort;
    }

    public async Task Execute(RegistrarWebhookPagamentoInput input, CancellationToken ct = default)
    {
        // Mercado Pago envia outros tópicos (ex.: "merchant_order") no mesmo endpoint de
        // webhook — só nos interessa "payment". Payload mínimo obrigatório: type + data.id.
        if (string.IsNullOrWhiteSpace(input.DataId) ||
            string.IsNullOrWhiteSpace(input.Tipo) ||
            !input.Tipo.Equals("payment", StringComparison.OrdinalIgnoreCase))
        {
            _outputPort.PayloadInvalido("Notificação inválida: campos 'type' (=\"payment\") e 'data.id' são obrigatórios.");
            return;
        }

        var assinaturaValida = _signatureValidator.EhValida(
            new WebhookSignatureContext(input.XSignature, input.XRequestId, input.DataId));

        if (!assinaturaValida)
        {
            _outputPort.AssinaturaInvalida();
            return;
        }

        var pagamentoConsultado = await _mercadoPagoGateway.BuscarPagamentoPorId(input.DataId, ct);
        if (pagamentoConsultado is null)
        {
            _outputPort.NaoEncontrado();
            return;
        }

        var orcamento = await _orcamentoGateway.BuscarPorIdOrdemServico(pagamentoConsultado.IdOrdemServico, ct);
        if (orcamento is null)
        {
            _outputPort.NaoEncontrado();
            return;
        }

        var pagamento = await _pagamentoGateway.BuscarPorIdOrcamento(orcamento.Id, ct);
        if (pagamento is null)
        {
            _outputPort.NaoEncontrado();
            return;
        }

        switch (pagamentoConsultado.Status)
        {
            case "approved":
                if (pagamento.Status == Domain.Pagamentos.Enums.StatusPagamento.Pendente)
                {
                    pagamento.Aprovar(pagamentoConsultado.PaymentId);
                    orcamento.Aprovar();

                    await _pagamentoGateway.Atualizar(pagamento, ct);
                    await _orcamentoGateway.Atualizar(orcamento, ct);
                }
                break;

            case "rejected":
            case "cancelled":
                if (pagamento.Status == Domain.Pagamentos.Enums.StatusPagamento.Pendente)
                {
                    pagamento.Recusar(pagamentoConsultado.PaymentId);
                    orcamento.Reprovar();

                    await _pagamentoGateway.Atualizar(pagamento, ct);
                    await _orcamentoGateway.Atualizar(orcamento, ct);
                }
                break;

            default:
                // "pending"/"in_process"/"authorized" — ainda não é um resultado final,
                // não há nada a persistir; o Mercado Pago reenviará outra notificação
                // quando o status mudar.
                break;
        }

        _outputPort.Processado();
    }
}
