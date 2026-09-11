using Domain.Common.Events;

namespace Domain.Pagamentos.Events;

// Publicado (via mensageria, a ser ligado em um follow-up) para o OS Service comandar
// IniciarExecucao no Execução Service.
public sealed record PagamentoAprovadoDomainEvent(
    Guid IdPagamento,
    Guid IdOrcamento,
    string PaymentId
) : IDomainEvent
{
    public DateTime OcurredAt { get; } = DateTime.UtcNow;
}
