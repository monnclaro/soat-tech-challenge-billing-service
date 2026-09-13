using Application.Common.Interfaces;
using Domain.Orcamentos.Gateways;
using Domain.Pagamentos.Events;

namespace Application.Pagamentos.EventHandlers;

// Compensação da saga: ao recusar o pagamento, publica o evento para o OS Service
// cancelar a OS.
internal sealed class PublicarPagamentoRecusadoHandler : IDomainEventHandler<PagamentoRecusadoDomainEvent>
{
    private readonly IOrcamentoGateway _orcamentoGateway;
    private readonly ISagaEventPublisher _publisher;

    public PublicarPagamentoRecusadoHandler(IOrcamentoGateway orcamentoGateway, ISagaEventPublisher publisher)
    {
        _orcamentoGateway = orcamentoGateway;
        _publisher = publisher;
    }

    public async Task Handle(PagamentoRecusadoDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var orcamento = await _orcamentoGateway.BuscarPorId(domainEvent.IdOrcamento, cancellationToken);
        if (orcamento is null)
        {
            return;
        }

        await _publisher.PublicarPagamentoRecusado(
            orcamento.IdOrdemServico,
            domainEvent.IdPagamento,
            "Pagamento recusado pelo Mercado Pago.",
            cancellationToken);
    }
}
