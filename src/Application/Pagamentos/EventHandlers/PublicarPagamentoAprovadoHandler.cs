using Application.Common.Interfaces;
using Domain.Orcamentos.Gateways;
using Domain.Pagamentos.Events;

namespace Application.Pagamentos.EventHandlers;

// Passo 5 -> 6 da saga: ao aprovar o pagamento (webhook do Mercado Pago), publica o
// evento para o OS Service avançar (comandar IniciarExecucao no Execução Service).
// O domain event só carrega IdOrcamento; busca o Orcamento para obter o
// IdOrdemServico que a saga precisa.
internal sealed class PublicarPagamentoAprovadoHandler : IDomainEventHandler<PagamentoAprovadoDomainEvent>
{
    private readonly IOrcamentoGateway _orcamentoGateway;
    private readonly ISagaEventPublisher _publisher;

    public PublicarPagamentoAprovadoHandler(IOrcamentoGateway orcamentoGateway, ISagaEventPublisher publisher)
    {
        _orcamentoGateway = orcamentoGateway;
        _publisher = publisher;
    }

    public async Task Handle(PagamentoAprovadoDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var orcamento = await _orcamentoGateway.BuscarPorId(domainEvent.IdOrcamento, cancellationToken);
        if (orcamento is null)
        {
            return;
        }

        await _publisher.PublicarPagamentoAprovado(orcamento.IdOrdemServico, domainEvent.IdPagamento, cancellationToken);
    }
}
