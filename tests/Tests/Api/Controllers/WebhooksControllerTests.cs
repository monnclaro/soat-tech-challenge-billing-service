using Api.Controllers.Webhooks;
using Api.Controllers.Webhooks.Requests;
using Api.Presenters.Pagamentos;
using Application.Orcamentos.Gateways;
using Application.Pagamentos.Controllers;
using Application.Pagamentos.UseCases.BuscarPagamento;
using Application.Pagamentos.UseCases.BuscarPagamentoPorOrcamento;
using Application.Pagamentos.UseCases.Interfaces;
using Application.Pagamentos.UseCases.RegistrarWebhookPagamento;
using Domain.Orcamentos.Gateways;
using Domain.Pagamentos.Gateways;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Tests.Api.Controllers;

public class WebhooksControllerTests
{
    private readonly Mock<IPagamentoGateway> _pagamentoGateway = new();
    private readonly Mock<IOrcamentoGateway> _orcamentoGateway = new();
    private readonly Mock<IMercadoPagoGateway> _mercadoPagoGateway = new();
    private readonly Mock<IWebhookSignatureValidator> _signatureValidator = new();
    private readonly RegistrarWebhookPagamentoPresenter _webhookPresenter = new();

    private WebhooksController CriarController()
    {
        var buscarUseCase = new BuscarPagamentoUseCase(_pagamentoGateway.Object, new BuscarPagamentoPresenter());
        var buscarPorOrcamentoUseCase = new BuscarPagamentoPorOrcamentoUseCase(_pagamentoGateway.Object, new BuscarPagamentoPorOrcamentoPresenter());
        var registrarWebhookUseCase = new RegistrarWebhookPagamentoUseCase(
            _signatureValidator.Object, _mercadoPagoGateway.Object, _orcamentoGateway.Object, _pagamentoGateway.Object, _webhookPresenter);

        var pagamentoController = new PagamentoController(buscarUseCase, buscarPorOrcamentoUseCase, registrarWebhookUseCase);

        var controller = new WebhooksController(pagamentoController, _webhookPresenter)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        return controller;
    }

    [Fact]
    public async Task Receber_ComCorpoValidoESemSecretConfigurado_DeveRetornarOk()
    {
        _mercadoPagoGateway.Setup(g => g.BuscarPagamentoPorId(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PagamentoConsultadoOutput?)null);
        _signatureValidator.Setup(v => v.EhValida(It.IsAny<WebhookSignatureContext>())).Returns(true);

        var controller = CriarController();
        var body = new MercadoPagoWebhookRequest("payment", null, new MercadoPagoWebhookDataRequest("123"));

        var resultado = await controller.Receber(body, null, null, CancellationToken.None);

        resultado.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task Receber_ComDataIdViaQueryString_DeveUsarOValorDaQuery()
    {
        _mercadoPagoGateway.Setup(g => g.BuscarPagamentoPorId("data-id-da-query", It.IsAny<CancellationToken>()))
            .ReturnsAsync((PagamentoConsultadoOutput?)null);
        _signatureValidator.Setup(v => v.EhValida(It.IsAny<WebhookSignatureContext>())).Returns(true);

        var controller = CriarController();

        var resultado = await controller.Receber(null, "data-id-da-query", "payment", CancellationToken.None);

        resultado.Should().BeOfType<OkResult>();
        _mercadoPagoGateway.Verify(g => g.BuscarPagamentoPorId("data-id-da-query", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Receber_ComPayloadInvalido_DeveRetornarBadRequest()
    {
        var controller = CriarController();

        var resultado = await controller.Receber(null, null, null, CancellationToken.None);

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }
}
