using Application.Common.Interfaces;
using MassTransit;
using Soat.Contracts.Saga;

namespace Infrastructure.Messaging;

public class MassTransitSagaEventPublisher : ISagaEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public MassTransitSagaEventPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public Task PublicarOrcamentoGerado(Guid idOrdemServico, Guid idOrcamento, decimal valorTotal, string linkPagamento, CancellationToken ct = default) =>
        _publishEndpoint.Publish(new OrcamentoGerado(idOrdemServico, idOrcamento, valorTotal, linkPagamento), ct);

    public Task PublicarOrcamentoFalhou(Guid idOrdemServico, string motivo, CancellationToken ct = default) =>
        _publishEndpoint.Publish(new OrcamentoFalhou(idOrdemServico, motivo), ct);

    public Task PublicarPagamentoAprovado(Guid idOrdemServico, Guid idPagamento, CancellationToken ct = default) =>
        _publishEndpoint.Publish(new PagamentoAprovado(idOrdemServico, idPagamento), ct);

    public Task PublicarPagamentoRecusado(Guid idOrdemServico, Guid idPagamento, string motivo, CancellationToken ct = default) =>
        _publishEndpoint.Publish(new PagamentoRecusado(idOrdemServico, idPagamento, motivo), ct);
}
