namespace Application.Common.Interfaces;

// Port de saída da saga (Application não conhece RabbitMQ/MassTransit — só
// este contrato; a implementação real mora em Infrastructure.Messaging).
public interface ISagaEventPublisher
{
    Task PublicarOrcamentoGerado(Guid idOrdemServico, Guid idOrcamento, decimal valorTotal, string linkPagamento, CancellationToken ct = default);

    Task PublicarOrcamentoFalhou(Guid idOrdemServico, string motivo, CancellationToken ct = default);

    Task PublicarPagamentoAprovado(Guid idOrdemServico, Guid idPagamento, CancellationToken ct = default);

    Task PublicarPagamentoRecusado(Guid idOrdemServico, Guid idPagamento, string motivo, CancellationToken ct = default);
}
