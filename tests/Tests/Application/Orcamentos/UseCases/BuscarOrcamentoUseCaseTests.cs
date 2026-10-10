using Application.Orcamentos.UseCases;
using Application.Orcamentos.UseCases.BuscarOrcamento;
using Domain.Orcamentos;
using Domain.Orcamentos.Enums;
using Domain.Orcamentos.Gateways;
using Domain.Orcamentos.Itens;
using Domain.Pagamentos;
using Domain.Pagamentos.Gateways;
using FluentAssertions;
using Moq;

namespace Tests.Application.Orcamentos.UseCases;

public class BuscarOrcamentoUseCaseTests
{
    private readonly Mock<IOrcamentoGateway> _gateway = new();
    private readonly Mock<IPagamentoGateway> _pagamentoGateway = new();
    private readonly Mock<IBuscarOrcamentoOutputPort> _outputPort = new();

    private BuscarOrcamentoUseCase CriarUseCase() => new(_gateway.Object, _pagamentoGateway.Object, _outputPort.Object);

    [Fact]
    public async Task Execute_QuandoOrcamentoExisteComPagamento_DeveRetornarOkComLinkPagamento()
    {
        var idOrdemServico = Guid.NewGuid();
        var orcamento = new Orcamento();
        orcamento.Gerar(idOrdemServico, [new OrcamentoItem(Guid.NewGuid(), "Item", 100m, TipoItemOrcamento.Servico)]);

        var pagamento = new Pagamento();
        pagamento.Criar(orcamento.Id, "preference-123", "https://mercadopago.com/checkout/preference-123", 100m);

        _gateway.Setup(g => g.BuscarPorIdOrdemServico(idOrdemServico, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orcamento);
        _pagamentoGateway.Setup(g => g.BuscarPorIdOrcamento(orcamento.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagamento);

        await CriarUseCase().Execute(new BuscarOrcamentoInput(idOrdemServico));

        _outputPort.Verify(o => o.Ok(It.Is<OrcamentoOutput>(output =>
            output.Id == orcamento.Id &&
            output.IdOrdemServico == idOrdemServico &&
            output.ValorTotal == 100m &&
            output.Itens.Count == 1 &&
            output.LinkPagamento == "https://mercadopago.com/checkout/preference-123")), Times.Once);
        _outputPort.Verify(o => o.NaoEncontrado(), Times.Never);
    }

    [Fact]
    public async Task Execute_QuandoOrcamentoExisteSemPagamento_DeveRetornarOkComLinkPagamentoNulo()
    {
        var idOrdemServico = Guid.NewGuid();
        var orcamento = new Orcamento();
        orcamento.Gerar(idOrdemServico, [new OrcamentoItem(Guid.NewGuid(), "Item", 100m, TipoItemOrcamento.Servico)]);

        _gateway.Setup(g => g.BuscarPorIdOrdemServico(idOrdemServico, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orcamento);
        _pagamentoGateway.Setup(g => g.BuscarPorIdOrcamento(orcamento.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Pagamento?)null);

        await CriarUseCase().Execute(new BuscarOrcamentoInput(idOrdemServico));

        _outputPort.Verify(o => o.Ok(It.Is<OrcamentoOutput>(output => output.LinkPagamento == null)), Times.Once);
    }

    [Fact]
    public async Task Execute_QuandoOrcamentoNaoExiste_DeveRetornarNaoEncontrado()
    {
        var idOrdemServico = Guid.NewGuid();

        _gateway.Setup(g => g.BuscarPorIdOrdemServico(idOrdemServico, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Orcamento?)null);

        await CriarUseCase().Execute(new BuscarOrcamentoInput(idOrdemServico));

        _outputPort.Verify(o => o.NaoEncontrado(), Times.Once);
        _outputPort.Verify(o => o.Ok(It.IsAny<OrcamentoOutput>()), Times.Never);
    }
}
