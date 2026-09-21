using Application.Pagamentos.UseCases;
using Application.Pagamentos.UseCases.BuscarPagamento;
using Domain.Pagamentos;
using Domain.Pagamentos.Gateways;
using FluentAssertions;
using Moq;

namespace Tests.Application.Pagamentos.UseCases;

public class BuscarPagamentoUseCaseTests
{
    private readonly Mock<IPagamentoGateway> _gateway = new();
    private readonly Mock<IBuscarPagamentoOutputPort> _outputPort = new();

    private BuscarPagamentoUseCase CriarUseCase() => new(_gateway.Object, _outputPort.Object);

    [Fact]
    public async Task Execute_QuandoPagamentoExiste_DeveRetornarOk()
    {
        var pagamento = new Pagamento();
        pagamento.Criar(Guid.NewGuid(), "preference-123", 100m);

        _gateway.Setup(g => g.BuscarPorId(pagamento.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagamento);

        await CriarUseCase().Execute(new BuscarPagamentoInput(pagamento.Id));

        _outputPort.Verify(o => o.Ok(It.Is<PagamentoOutput>(output =>
            output.Id == pagamento.Id &&
            output.PreferenceId == "preference-123" &&
            output.Valor == 100m)), Times.Once);
        _outputPort.Verify(o => o.NaoEncontrado(), Times.Never);
    }

    [Fact]
    public async Task Execute_QuandoPagamentoNaoExiste_DeveRetornarNaoEncontrado()
    {
        var id = Guid.NewGuid();

        _gateway.Setup(g => g.BuscarPorId(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Pagamento?)null);

        await CriarUseCase().Execute(new BuscarPagamentoInput(id));

        _outputPort.Verify(o => o.NaoEncontrado(), Times.Once);
        _outputPort.Verify(o => o.Ok(It.IsAny<PagamentoOutput>()), Times.Never);
    }
}
