using FluentAssertions;
using Infrastructure.Messaging;
using MassTransit;
using Moq;
using Soat.Contracts.Saga;

namespace Tests.Infrastructure.Messaging;

public class MassTransitSagaEventPublisherTests
{
    private readonly Mock<IPublishEndpoint> _publishEndpoint = new();

    private MassTransitSagaEventPublisher CriarPublisher() => new(_publishEndpoint.Object);

    [Fact]
    public async Task PublicarOrcamentoGerado_DevePublicarEventoComOsDadosInformados()
    {
        var idOrdemServico = Guid.NewGuid();
        var idOrcamento = Guid.NewGuid();

        await CriarPublisher().PublicarOrcamentoGerado(idOrdemServico, idOrcamento, 150m, "https://link", CancellationToken.None);

        _publishEndpoint.Verify(p => p.Publish(
            It.Is<OrcamentoGerado>(e =>
                e.IdOrdemServico == idOrdemServico &&
                e.IdOrcamento == idOrcamento &&
                e.ValorTotal == 150m &&
                e.LinkPagamento == "https://link"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublicarPagamentoAprovado_DevePublicarEventoComOsDadosInformados()
    {
        var idOrdemServico = Guid.NewGuid();
        var idPagamento = Guid.NewGuid();

        await CriarPublisher().PublicarPagamentoAprovado(idOrdemServico, idPagamento, CancellationToken.None);

        _publishEndpoint.Verify(p => p.Publish(
            It.Is<PagamentoAprovado>(e => e.IdOrdemServico == idOrdemServico && e.IdPagamento == idPagamento),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublicarPagamentoRecusado_DevePublicarEventoComOsDadosInformados()
    {
        var idOrdemServico = Guid.NewGuid();
        var idPagamento = Guid.NewGuid();

        await CriarPublisher().PublicarPagamentoRecusado(idOrdemServico, idPagamento, "motivo qualquer", CancellationToken.None);

        _publishEndpoint.Verify(p => p.Publish(
            It.Is<PagamentoRecusado>(e => e.IdOrdemServico == idOrdemServico && e.IdPagamento == idPagamento && e.Motivo == "motivo qualquer"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
