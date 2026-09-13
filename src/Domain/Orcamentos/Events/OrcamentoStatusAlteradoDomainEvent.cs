using Domain.Common.Events;
using Domain.Orcamentos.Enums;

namespace Domain.Orcamentos.Events;

public sealed record OrcamentoStatusAlteradoDomainEvent(
    Guid IdOrcamento,
    Guid IdOrdemServico,
    StatusOrcamento Status
) : IDomainEvent
{
    public DateTime OcurredAt { get; } = DateTime.UtcNow;
}
