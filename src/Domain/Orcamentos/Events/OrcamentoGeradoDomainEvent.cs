using Domain.Common.Events;

namespace Domain.Orcamentos.Events;

// Publicado (via mensageria, a ser ligado em um follow-up) para o OS Service avançar a
// saga (AguardandoAprovacao) e informar o cliente do link de pagamento gerado.
public sealed record OrcamentoGeradoDomainEvent(
    Guid IdOrcamento,
    Guid IdOrdemServico,
    decimal ValorTotal
) : IDomainEvent
{
    public DateTime OcurredAt { get; } = DateTime.UtcNow;
}
