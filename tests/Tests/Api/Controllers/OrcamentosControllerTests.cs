using Api.Common.Exceptions;
using Api.Controllers.Orcamentos;
using Api.Controllers.Orcamentos.Requests;
using Api.Presenters.Orcamentos;
using Application.Common.Interfaces;
using Application.Orcamentos.Controllers;
using Application.Orcamentos.Gateways;
using Application.Orcamentos.UseCases.BuscarOrcamento;
using Application.Orcamentos.UseCases.GerarOrcamento;
using Domain.Orcamentos;
using Domain.Orcamentos.Enums;
using Domain.Orcamentos.Gateways;
using Domain.Orcamentos.Itens;
using Domain.Pagamentos.Gateways;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Tests.Api.Controllers;

public class OrcamentosControllerTests
{
    private readonly Mock<IOrcamentoGateway> _orcamentoGateway = new();
    private readonly Mock<IPagamentoGateway> _pagamentoGateway = new();
    private readonly Mock<IMercadoPagoGateway> _mercadoPagoGateway = new();
    private readonly Mock<ISagaEventPublisher> _sagaEventPublisher = new();
    private readonly GerarOrcamentoPresenter _gerarPresenter = new();
    private readonly BuscarOrcamentoPresenter _buscarPresenter = new();

    private OrcamentosController CriarController()
    {
        var gerarUseCase = new GerarOrcamentoUseCase(
            _orcamentoGateway.Object, _pagamentoGateway.Object, _mercadoPagoGateway.Object, _sagaEventPublisher.Object, _gerarPresenter);
        var buscarUseCase = new BuscarOrcamentoUseCase(_orcamentoGateway.Object, _buscarPresenter);
        var orcamentoController = new OrcamentoController(gerarUseCase, buscarUseCase);

        return new OrcamentosController(orcamentoController, _gerarPresenter, _buscarPresenter);
    }

    [Fact]
    public async Task Buscar_QuandoOrcamentoExiste_DeveRetornarOk()
    {
        var idOrdemServico = Guid.NewGuid();
        var orcamento = new Orcamento();
        orcamento.Gerar(idOrdemServico, [new OrcamentoItem(Guid.NewGuid(), "Item", 100m, TipoItemOrcamento.Servico)]);

        _orcamentoGateway.Setup(g => g.BuscarPorIdOrdemServico(idOrdemServico, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orcamento);

        var resultado = await CriarController().Buscar(idOrdemServico, CancellationToken.None);

        resultado.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Buscar_QuandoOrcamentoNaoExiste_DeveRetornarNotFound()
    {
        _orcamentoGateway.Setup(g => g.BuscarPorIdOrdemServico(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Orcamento?)null);

        var resultado = await CriarController().Buscar(Guid.NewGuid(), CancellationToken.None);

        resultado.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Gerar_ComTiposDeItemValidos_DeveRetornarCreatedAtAction()
    {
        var request = new GerarOrcamentoRequest(
            Guid.NewGuid(),
            [
                new ItemOrcamentoRequest(Guid.NewGuid(), "Troca de óleo", 150m, "Servico"),
                new ItemOrcamentoRequest(Guid.NewGuid(), "Filtro", 50m, "Produto")
            ],
            200m);

        _orcamentoGateway.Setup(g => g.BuscarPorIdOrdemServico(request.IdOrdemServico, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Orcamento?)null);
        _mercadoPagoGateway.Setup(g => g.CriarPreferencia(It.IsAny<CriarPreferenciaInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PreferenciaCriadaOutput("preference-1", "https://link"));

        var resultado = await CriarController().Gerar(request, CancellationToken.None);

        resultado.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Gerar_ComTipoDeItemInvalido_DeveLancarConflictException()
    {
        var request = new GerarOrcamentoRequest(
            Guid.NewGuid(),
            [new ItemOrcamentoRequest(Guid.NewGuid(), "Item desconhecido", 150m, "TipoQueNaoExiste")],
            150m);

        var controller = CriarController();

        var acao = async () => await controller.Gerar(request, CancellationToken.None);

        await acao.Should().ThrowAsync<ConflictException>();
        _orcamentoGateway.Verify(g => g.BuscarPorIdOrdemServico(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
