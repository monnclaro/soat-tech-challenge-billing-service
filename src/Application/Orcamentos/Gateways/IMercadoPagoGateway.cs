namespace Application.Orcamentos.Gateways;

// Porta para a integração com o Mercado Pago (Checkout Pro). Chamado "IMercadoPagoGateway"
// (em vez de "IPagamentoGateway") para não colidir com Domain.Pagamentos.Gateways.IPagamentoGateway,
// que é o gateway de persistência da entidade Pagamento — aqui em Application porque isto é
// uma integração com um sistema externo, não uma regra de negócio nem uma preocupação de
// persistência (mesma distinção que o OS Service já faz entre gateways de persistência,
// terminados em "Gateway", e integrações de infraestrutura como ITokenProvider/IPasswordHasher).
public interface IMercadoPagoGateway
{
    Task<PreferenciaCriadaOutput> CriarPreferencia(CriarPreferenciaInput input, CancellationToken ct = default);

    Task<PagamentoConsultadoOutput?> BuscarPagamentoPorId(string paymentId, CancellationToken ct = default);
}

public record CriarPreferenciaInput(
    Guid IdOrdemServico,
    IReadOnlyList<ItemPreferenciaInput> Itens,
    string? NotificationUrl = null);

public record ItemPreferenciaInput(string Nome, decimal Valor);

public record PreferenciaCriadaOutput(string PreferenceId, string InitPoint);

// external_reference do pagamento no Mercado Pago é sempre o IdOrdemServico (definido na
// criação da preferência) — é assim que conseguimos religar a notificação do webhook (que só
// traz o id do pagamento) ao Orcamento/Pagamento correspondente sem acessar banco de outro serviço.
public record PagamentoConsultadoOutput(
    string PaymentId,
    string Status,
    Guid IdOrdemServico,
    decimal Valor);
