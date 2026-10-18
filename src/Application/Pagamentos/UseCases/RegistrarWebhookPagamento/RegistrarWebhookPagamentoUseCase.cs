using Application.Common.Interfaces;
using Application.Orcamentos.Gateways;
using Application.Pagamentos.UseCases.Interfaces;
using Domain.Orcamentos.Gateways;
using Domain.Pagamentos.Gateways;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<RegistrarWebhookPagamentoUseCase> _logger;

    public RegistrarWebhookPagamentoUseCase(
        IWebhookSignatureValidator signatureValidator,
        IMercadoPagoGateway mercadoPagoGateway,
        IOrcamentoGateway orcamentoGateway,
        IPagamentoGateway pagamentoGateway,
        IRegistrarWebhookPagamentoOutputPort outputPort,
        ILogger<RegistrarWebhookPagamentoUseCase> logger)
    {
        _signatureValidator = signatureValidator;
        _mercadoPagoGateway = mercadoPagoGateway;
        _orcamentoGateway = orcamentoGateway;
        _pagamentoGateway = pagamentoGateway;
        _outputPort = outputPort;
        _logger = logger;
    }

    public async Task Execute(RegistrarWebhookPagamentoInput input, CancellationToken ct = default)
    {
        // Mercado Pago envia outros tópicos (ex.: "merchant_order") no mesmo endpoint de
        // webhook — só nos interessa pagamento. Payload mínimo obrigatório: tipo + data.id.
        // "tipo" vem de "type" (tipo de recurso, ex. "payment") OU de "action" (evento do
        // ciclo de vida, ex. "payment.created"/"payment.updated") quando "type" não vem no
        // corpo — por isso aceita tanto o valor exato "payment" quanto qualquer "payment.*".
        var ehNotificacaoDePagamento = input.Tipo is not null &&
            (input.Tipo.Equals("payment", StringComparison.OrdinalIgnoreCase) ||
             input.Tipo.StartsWith("payment.", StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(input.DataId) || !ehNotificacaoDePagamento)
        {
            _outputPort.PayloadInvalido("Notificação inválida: campos 'type'/'action' relacionados a pagamento e 'data.id' são obrigatórios.");
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

        // TODO(diagnostico-temporario): remover junto com o log do WebhooksController depois
        // de confirmar o comportamento em produção — "NaoEncontrado" e "status não-final"
        // devolvem o mesmo 200 pro Mercado Pago, então de fora não dá pra distinguir "pagamento
        // não bateu com nenhum Orçamento nosso" de "ainda está pending, vai vir outro webhook".
        _logger.LogInformation(
            "Webhook Mercado Pago consultado: dataId={DataId} encontradoNoMercadoPago={Encontrado} " +
            "statusMercadoPago={StatusMp} idOrdemServico={IdOrdemServico}",
            input.DataId, pagamentoConsultado is not null, pagamentoConsultado?.Status, pagamentoConsultado?.IdOrdemServico);

        if (pagamentoConsultado is null)
        {
            _outputPort.NaoEncontrado();
            return;
        }

        var orcamento = await _orcamentoGateway.BuscarPorIdOrdemServico(pagamentoConsultado.IdOrdemServico, ct);
        if (orcamento is null)
        {
            _logger.LogInformation("Webhook Mercado Pago: nenhum Orcamento encontrado para idOrdemServico={IdOrdemServico}", pagamentoConsultado.IdOrdemServico);
            _outputPort.NaoEncontrado();
            return;
        }

        var pagamento = await _pagamentoGateway.BuscarPorIdOrcamento(orcamento.Id, ct);
        if (pagamento is null)
        {
            _logger.LogInformation("Webhook Mercado Pago: nenhum Pagamento encontrado para idOrcamento={IdOrcamento}", orcamento.Id);
            _outputPort.NaoEncontrado();
            return;
        }

        _logger.LogInformation(
            "Webhook Mercado Pago: pagamento encontrado idPagamento={IdPagamento} statusAtualNoBanco={StatusAtual} statusMercadoPago={StatusMp}",
            pagamento.Id, pagamento.Status, pagamentoConsultado.Status);

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
