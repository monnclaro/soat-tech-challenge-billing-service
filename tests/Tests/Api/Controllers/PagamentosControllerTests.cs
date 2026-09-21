using Api.Controllers.Pagamentos;
using Api.Presenters.Pagamentos;
using Application.Orcamentos.Gateways;
using Application.Pagamentos.Controllers;
using Application.Pagamentos.UseCases.BuscarPagamento;
using Application.Pagamentos.UseCases.BuscarPagamentoPorOrcamento;
using Application.Pagamentos.UseCases.Interfaces;
using Application.Pagamentos.UseCases.RegistrarWebhookPagamento;
using Domain.Orcamentos.Gateways;
using Domain.Pagamentos;
using Domain.Pagamentos.Gateways;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Tests.Api.Controllers;

public class PagamentosControllerTests
{
    private readonly Mock<IPagamentoGateway> _pagamentoGateway = new();
    private readonly Mock<IOrcamentoGateway> _orcamentoGateway = new();
    private readonly Mock<IMercadoPagoGateway> _mercadoPagoGateway = new();
    private readonly Mock<IWebhookSignatureValidator> _signatureValidator = new();
    private readonly BuscarPagamentoPresenter _buscarPresenter = new();
    private readonly BuscarPagamentoPorOrcamentoPresenter _buscarPorOrcamentoPresenter = new();
    private readonly RegistrarWebhookPagamentoPresenter _webhookPresenter = new();

    private PagamentosController CriarController()
    {
        var buscarUseCase = new BuscarPagamentoUseCase(_pagamentoGateway.Object, _buscarPresenter);
        var buscarPorOrcamentoUseCase = new BuscarPagamentoPorOrcamentoUseCase(_pagamentoGateway.Object, _buscarPorOrcamentoPresenter);
        var registrarWebhookUseCase = new RegistrarWebhookPagamentoUseCase(
            _signatureValidator.Object, _mercadoPagoGateway.Object, _orcamentoGateway.Object, _pagamentoGateway.Object, _webhookPresenter);

        var pagamentoController = new PagamentoController(buscarUseCase, buscarPorOrcamentoUseCase, registrarWebhookUseCase);

        return new PagamentosController(pagamentoController, _buscarPresenter, _buscarPorOrcamentoPresenter);
    }

    [Fact]
    public async Task Buscar_QuandoPagamentoExiste_DeveRetornarOk()
    {
        var pagamento = new Pagamento();
        pagamento.Criar(Guid.NewGuid(), "preference-123", 100m);

        _pagamentoGateway.Setup(g => g.BuscarPorId(pagamento.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagamento);

        var resultado = await CriarController().Buscar(pagamento.Id, CancellationToken.None);

        resultado.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Buscar_QuandoPagamentoNaoExiste_DeveRetornarNotFound()
    {
        _pagamentoGateway.Setup(g => g.BuscarPorId(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Pagamento?)null);

        var resultado = await CriarController().Buscar(Guid.NewGuid(), CancellationToken.None);

        resultado.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task BuscarPorOrcamento_QuandoPagamentoExiste_DeveRetornarOk()
    {
        var idOrcamento = Guid.NewGuid();
        var pagamento = new Pagamento();
        pagamento.Criar(idOrcamento, "preference-123", 100m);

        _pagamentoGateway.Setup(g => g.BuscarPorIdOrcamento(idOrcamento, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagamento);

        var resultado = await CriarController().BuscarPorOrcamento(idOrcamento, CancellationToken.None);

        resultado.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task BuscarPorOrcamento_QuandoPagamentoNaoExiste_DeveRetornarNotFound()
    {
        _pagamentoGateway.Setup(g => g.BuscarPorIdOrcamento(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Pagamento?)null);

        var resultado = await CriarController().BuscarPorOrcamento(Guid.NewGuid(), CancellationToken.None);

        resultado.Should().BeOfType<NotFoundResult>();
    }
}
