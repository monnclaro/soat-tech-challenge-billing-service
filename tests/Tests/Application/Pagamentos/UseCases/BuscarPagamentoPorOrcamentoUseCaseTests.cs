using Application.Pagamentos.UseCases;
using Application.Pagamentos.UseCases.BuscarPagamentoPorOrcamento;
using Domain.Pagamentos;
using Domain.Pagamentos.Gateways;
using FluentAssertions;
using Moq;

namespace Tests.Application.Pagamentos.UseCases;

public class BuscarPagamentoPorOrcamentoUseCaseTests
{
    private readonly Mock<IPagamentoGateway> _gateway = new();
    private readonly Mock<IBuscarPagamentoPorOrcamentoOutputPort> _outputPort = new();

    private BuscarPagamentoPorOrcamentoUseCase CriarUseCase() => new(_gateway.Object, _outputPort.Object);

    [Fact]
    public async Task Execute_QuandoPagamentoExiste_DeveRetornarOk()
    {
        var idOrcamento = Guid.NewGuid();
        var pagamento = new Pagamento();
        pagamento.Criar(idOrcamento, "preference-123", 100m);

        _gateway.Setup(g => g.BuscarPorIdOrcamento(idOrcamento, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagamento);

        await CriarUseCase().Execute(new BuscarPagamentoPorOrcamentoInput(idOrcamento));

        _outputPort.Verify(o => o.Ok(It.Is<PagamentoOutput>(output =>
            output.Id == pagamento.Id && output.IdOrcamento == idOrcamento)), Times.Once);
        _outputPort.Verify(o => o.NaoEncontrado(), Times.Never);
    }

    [Fact]
    public async Task Execute_QuandoPagamentoNaoExiste_DeveRetornarNaoEncontrado()
    {
        var idOrcamento = Guid.NewGuid();

        _gateway.Setup(g => g.BuscarPorIdOrcamento(idOrcamento, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Pagamento?)null);

        await CriarUseCase().Execute(new BuscarPagamentoPorOrcamentoInput(idOrcamento));

        _outputPort.Verify(o => o.NaoEncontrado(), Times.Once);
        _outputPort.Verify(o => o.Ok(It.IsAny<PagamentoOutput>()), Times.Never);
    }
}
