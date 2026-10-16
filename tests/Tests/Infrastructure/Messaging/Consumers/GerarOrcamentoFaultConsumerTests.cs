using Application.Common.Interfaces;
using Infrastructure.Messaging.Consumers;
using MassTransit;
using Moq;
using Soat.Contracts.Saga;

namespace Tests.Infrastructure.Messaging.Consumers;

public class GerarOrcamentoFaultConsumerTests
{
    private readonly Mock<ISagaEventPublisher> _sagaEventPublisher = new();

    private GerarOrcamentoFaultConsumer CriarConsumer() => new(_sagaEventPublisher.Object);

    [Fact]
    public async Task Consume_QuandoRetentativasSeEsgotam_DevePublicarOrcamentoFalhouComOMotivoDaExcecao()
    {
        var idOrdemServico = Guid.NewGuid();
        var mensagemOriginal = new GerarOrcamento(idOrdemServico, [], [], 100m);

        var exceptionInfo = new Mock<ExceptionInfo>();
        exceptionInfo.SetupGet(e => e.Message).Returns("Mercado Pago indisponível");

        var fault = new Mock<Fault<GerarOrcamento>>();
        fault.SetupGet(f => f.Message).Returns(mensagemOriginal);
        fault.SetupGet(f => f.Exceptions).Returns([exceptionInfo.Object]);

        var contexto = new Mock<ConsumeContext<Fault<GerarOrcamento>>>();
        contexto.SetupGet(c => c.Message).Returns(fault.Object);
        contexto.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        await CriarConsumer().Consume(contexto.Object);

        _sagaEventPublisher.Verify(p => p.PublicarOrcamentoFalhou(
            idOrdemServico, "Mercado Pago indisponível", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Consume_SemExceptionsNoFault_DevePublicarOrcamentoFalhouComMotivoPadrao()
    {
        var idOrdemServico = Guid.NewGuid();
        var mensagemOriginal = new GerarOrcamento(idOrdemServico, [], [], 100m);

        var fault = new Mock<Fault<GerarOrcamento>>();
        fault.SetupGet(f => f.Message).Returns(mensagemOriginal);
        fault.SetupGet(f => f.Exceptions).Returns([]);

        var contexto = new Mock<ConsumeContext<Fault<GerarOrcamento>>>();
        contexto.SetupGet(c => c.Message).Returns(fault.Object);
        contexto.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        await CriarConsumer().Consume(contexto.Object);

        _sagaEventPublisher.Verify(p => p.PublicarOrcamentoFalhou(
            idOrdemServico,
            "Falha ao gerar a preferência de pagamento no Mercado Pago.",
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
