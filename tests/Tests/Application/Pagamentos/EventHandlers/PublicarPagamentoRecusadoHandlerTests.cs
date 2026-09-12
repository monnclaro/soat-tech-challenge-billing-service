using Application.Common.Interfaces;
using Application.Pagamentos.EventHandlers;
using Domain.Orcamentos;
using Domain.Orcamentos.Enums;
using Domain.Orcamentos.Gateways;
using Domain.Orcamentos.Itens;
using Domain.Pagamentos.Events;
using FluentAssertions;
using Moq;

namespace Tests.Application.Pagamentos.EventHandlers;

public class PublicarPagamentoRecusadoHandlerTests
{
    private readonly Mock<IOrcamentoGateway> _orcamentoGateway = new();
    private readonly Mock<ISagaEventPublisher> _publisher = new();

    private PublicarPagamentoRecusadoHandler CriarHandler() => new(_orcamentoGateway.Object, _publisher.Object);

    [Fact]
    public async Task Handle_QuandoOrcamentoExiste_DevePublicarPagamentoRecusadoComIdOrdemServicoEMotivoPadrao()
    {
        var idOrdemServico = Guid.NewGuid();
        var orcamento = new Orcamento();
        orcamento.Gerar(idOrdemServico, [new OrcamentoItem(Guid.NewGuid(), "Item", 100m, TipoItemOrcamento.Servico)]);

        var domainEvent = new PagamentoRecusadoDomainEvent(Guid.NewGuid(), orcamento.Id, "mp-1");

        _orcamentoGateway.Setup(g => g.BuscarPorId(orcamento.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orcamento);

        await CriarHandler().Handle(domainEvent, CancellationToken.None);

        _publisher.Verify(p => p.PublicarPagamentoRecusado(
            idOrdemServico,
            domainEvent.IdPagamento,
            "Pagamento recusado pelo Mercado Pago.",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_QuandoOrcamentoNaoExiste_NaoDevePublicarNada()
    {
        var domainEvent = new PagamentoRecusadoDomainEvent(Guid.NewGuid(), Guid.NewGuid(), "mp-1");

        _orcamentoGateway.Setup(g => g.BuscarPorId(domainEvent.IdOrcamento, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Orcamento?)null);

        await CriarHandler().Handle(domainEvent, CancellationToken.None);

        _publisher.Verify(p => p.PublicarPagamentoRecusado(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
