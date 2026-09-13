using Domain.Common.Events;

namespace Domain.Pagamentos.Events;

// Publicado (via mensageria, a ser ligado em um follow-up) — caminho de compensação:
// o OS Service cancela a OS ao consumir este evento.
public sealed record PagamentoRecusadoDomainEvent(
    Guid IdPagamento,
    Guid IdOrcamento,
    string PaymentId
) : IDomainEvent
{
    public DateTime OcurredAt { get; } = DateTime.UtcNow;
}
