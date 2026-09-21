using Application.Orcamentos.Gateways;
using Application.Pagamentos.UseCases.Interfaces;
using Application.Pagamentos.UseCases.RegistrarWebhookPagamento;
using Domain.Orcamentos;
using Domain.Orcamentos.Enums;
using Domain.Orcamentos.Gateways;
using Domain.Orcamentos.Itens;
using Domain.Pagamentos;
using Domain.Pagamentos.Enums;
using Domain.Pagamentos.Gateways;
using FluentAssertions;
using Moq;

namespace Tests.Application.Pagamentos.UseCases;

public class RegistrarWebhookPagamentoUseCaseTests
{
    private readonly Mock<IWebhookSignatureValidator> _signatureValidator = new();
    private readonly Mock<IMercadoPagoGateway> _mercadoPagoGateway = new();
    private readonly Mock<IOrcamentoGateway> _orcamentoGateway = new();
    private readonly Mock<IPagamentoGateway> _pagamentoGateway = new();
    private readonly Mock<IRegistrarWebhookPagamentoOutputPort> _outputPort = new();

    public RegistrarWebhookPagamentoUseCaseTests()
    {
        _signatureValidator
            .Setup(v => v.EhValida(It.IsAny<WebhookSignatureContext>()))
            .Returns(true);
    }

    private RegistrarWebhookPagamentoUseCase CriarUseCase() => new(
        _signatureValidator.Object,
        _mercadoPagoGateway.Object,
        _orcamentoGateway.Object,
        _pagamentoGateway.Object,
        _outputPort.Object);

    private static (Orcamento orcamento, Pagamento pagamento) CriarOrcamentoComPagamentoPendente(Guid idOrdemServico)
    {
        var orcamento = new Orcamento();
        orcamento.Gerar(idOrdemServico, [new OrcamentoItem(Guid.NewGuid(), "Item", 200m, TipoItemOrcamento.Servico)]);

        var pagamento = new Pagamento();
        pagamento.Criar(orcamento.Id, "preference-123", 200m);

        return (orcamento, pagamento);
    }

    [Theory]
    [InlineData(null, "payment")]
    [InlineData("", "payment")]
    [InlineData("data-id", null)]
    [InlineData("data-id", "")]
    [InlineData("data-id", "merchant_order")]
    public async Task Execute_ComPayloadInvalido_DeveRetornarPayloadInvalidoENaoConsultarNada(string? dataId, string? tipo)
    {
        await CriarUseCase().Execute(new RegistrarWebhookPagamentoInput(tipo, dataId, "x", "y"));

        _outputPort.Verify(o => o.PayloadInvalido(It.IsAny<string>()), Times.Once);
        _mercadoPagoGateway.Verify(g => g.BuscarPagamentoPorId(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Execute_ComAssinaturaInvalida_DeveRetornarAssinaturaInvalidaENaoConsultarMercadoPago()
    {
        _signatureValidator.Setup(v => v.EhValida(It.IsAny<WebhookSignatureContext>())).Returns(false);

        await CriarUseCase().Execute(new RegistrarWebhookPagamentoInput("payment", "data-id", "x", "y"));

        _outputPort.Verify(o => o.AssinaturaInvalida(), Times.Once);
        _mercadoPagoGateway.Verify(g => g.BuscarPagamentoPorId(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Execute_QuandoMercadoPagoNaoEncontraOPagamento_DeveRetornarNaoEncontrado()
    {
        _mercadoPagoGateway.Setup(g => g.BuscarPagamentoPorId("data-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync((PagamentoConsultadoOutput?)null);

        await CriarUseCase().Execute(new RegistrarWebhookPagamentoInput("payment", "data-id", "x", "y"));

        _outputPort.Verify(o => o.NaoEncontrado(), Times.Once);
        _orcamentoGateway.Verify(g => g.BuscarPorIdOrdemServico(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Execute_QuandoOrcamentoNaoExiste_DeveRetornarNaoEncontrado()
    {
        var idOrdemServico = Guid.NewGuid();

        _mercadoPagoGateway.Setup(g => g.BuscarPagamentoPorId("data-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagamentoConsultadoOutput("mp-1", "approved", idOrdemServico, 200m));

        _orcamentoGateway.Setup(g => g.BuscarPorIdOrdemServico(idOrdemServico, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Orcamento?)null);

        await CriarUseCase().Execute(new RegistrarWebhookPagamentoInput("payment", "data-id", "x", "y"));

        _outputPort.Verify(o => o.NaoEncontrado(), Times.Once);
        _pagamentoGateway.Verify(g => g.BuscarPorIdOrcamento(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Execute_QuandoPagamentoDoOrcamentoNaoExiste_DeveRetornarNaoEncontrado()
    {
        var idOrdemServico = Guid.NewGuid();
        var orcamento = new Orcamento();
        orcamento.Gerar(idOrdemServico, [new OrcamentoItem(Guid.NewGuid(), "Item", 200m, TipoItemOrcamento.Servico)]);

        _mercadoPagoGateway.Setup(g => g.BuscarPagamentoPorId("data-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagamentoConsultadoOutput("mp-1", "approved", idOrdemServico, 200m));
        _orcamentoGateway.Setup(g => g.BuscarPorIdOrdemServico(idOrdemServico, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orcamento);
        _pagamentoGateway.Setup(g => g.BuscarPorIdOrcamento(orcamento.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Pagamento?)null);

        await CriarUseCase().Execute(new RegistrarWebhookPagamentoInput("payment", "data-id", "x", "y"));

        _outputPort.Verify(o => o.NaoEncontrado(), Times.Once);
    }

    [Fact]
    public async Task Execute_ComStatusApproved_DeveAprovarPagamentoEOrcamentoEAtualizarAmbos()
    {
        var idOrdemServico = Guid.NewGuid();
        var (orcamento, pagamento) = CriarOrcamentoComPagamentoPendente(idOrdemServico);

        _mercadoPagoGateway.Setup(g => g.BuscarPagamentoPorId("data-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagamentoConsultadoOutput("mp-1", "approved", idOrdemServico, 200m));
        _orcamentoGateway.Setup(g => g.BuscarPorIdOrdemServico(idOrdemServico, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orcamento);
        _pagamentoGateway.Setup(g => g.BuscarPorIdOrcamento(orcamento.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagamento);

        await CriarUseCase().Execute(new RegistrarWebhookPagamentoInput("payment", "data-id", "x", "y"));

        pagamento.Status.Should().Be(StatusPagamento.Aprovado);
        orcamento.Status.Should().Be(StatusOrcamento.Aprovado);

        _pagamentoGateway.Verify(g => g.Atualizar(pagamento, It.IsAny<CancellationToken>()), Times.Once);
        _orcamentoGateway.Verify(g => g.Atualizar(orcamento, It.IsAny<CancellationToken>()), Times.Once);
        _outputPort.Verify(o => o.Processado(), Times.Once);
    }

    [Theory]
    [InlineData("rejected")]
    [InlineData("cancelled")]
    public async Task Execute_ComStatusRejectedOuCancelled_DeveRecusarPagamentoEReprovarOrcamentoEAtualizarAmbos(string status)
    {
        var idOrdemServico = Guid.NewGuid();
        var (orcamento, pagamento) = CriarOrcamentoComPagamentoPendente(idOrdemServico);

        _mercadoPagoGateway.Setup(g => g.BuscarPagamentoPorId("data-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagamentoConsultadoOutput("mp-1", status, idOrdemServico, 200m));
        _orcamentoGateway.Setup(g => g.BuscarPorIdOrdemServico(idOrdemServico, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orcamento);
        _pagamentoGateway.Setup(g => g.BuscarPorIdOrcamento(orcamento.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagamento);

        await CriarUseCase().Execute(new RegistrarWebhookPagamentoInput("payment", "data-id", "x", "y"));

        pagamento.Status.Should().Be(StatusPagamento.Recusado);
        orcamento.Status.Should().Be(StatusOrcamento.Reprovado);

        _pagamentoGateway.Verify(g => g.Atualizar(pagamento, It.IsAny<CancellationToken>()), Times.Once);
        _orcamentoGateway.Verify(g => g.Atualizar(orcamento, It.IsAny<CancellationToken>()), Times.Once);
        _outputPort.Verify(o => o.Processado(), Times.Once);
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("in_process")]
    [InlineData("authorized")]
    public async Task Execute_ComStatusNaoFinal_NaoDeveAlterarNemAtualizarNada(string status)
    {
        var idOrdemServico = Guid.NewGuid();
        var (orcamento, pagamento) = CriarOrcamentoComPagamentoPendente(idOrdemServico);

        _mercadoPagoGateway.Setup(g => g.BuscarPagamentoPorId("data-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagamentoConsultadoOutput("mp-1", status, idOrdemServico, 200m));
        _orcamentoGateway.Setup(g => g.BuscarPorIdOrdemServico(idOrdemServico, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orcamento);
        _pagamentoGateway.Setup(g => g.BuscarPorIdOrcamento(orcamento.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagamento);

        await CriarUseCase().Execute(new RegistrarWebhookPagamentoInput("payment", "data-id", "x", "y"));

        pagamento.Status.Should().Be(StatusPagamento.Pendente);
        orcamento.Status.Should().Be(StatusOrcamento.Pendente);

        _pagamentoGateway.Verify(g => g.Atualizar(It.IsAny<Pagamento>(), It.IsAny<CancellationToken>()), Times.Never);
        _orcamentoGateway.Verify(g => g.Atualizar(It.IsAny<Orcamento>(), It.IsAny<CancellationToken>()), Times.Never);
        _outputPort.Verify(o => o.Processado(), Times.Once);
    }

    [Fact]
    public async Task Execute_ComStatusApprovedMasPagamentoJaAprovado_NaoDeveAprovarNovamente()
    {
        // Reentrega da notificação (idempotência): o pagamento já não está mais Pendente,
        // então não deve tentar aprovar de novo (o que lançaria DomainException).
        var idOrdemServico = Guid.NewGuid();
        var (orcamento, pagamento) = CriarOrcamentoComPagamentoPendente(idOrdemServico);
        pagamento.Aprovar("mp-1");
        orcamento.Aprovar();

        _mercadoPagoGateway.Setup(g => g.BuscarPagamentoPorId("data-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagamentoConsultadoOutput("mp-1", "approved", idOrdemServico, 200m));
        _orcamentoGateway.Setup(g => g.BuscarPorIdOrdemServico(idOrdemServico, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orcamento);
        _pagamentoGateway.Setup(g => g.BuscarPorIdOrcamento(orcamento.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagamento);

        await CriarUseCase().Execute(new RegistrarWebhookPagamentoInput("payment", "data-id", "x", "y"));

        _pagamentoGateway.Verify(g => g.Atualizar(It.IsAny<Pagamento>(), It.IsAny<CancellationToken>()), Times.Never);
        _orcamentoGateway.Verify(g => g.Atualizar(It.IsAny<Orcamento>(), It.IsAny<CancellationToken>()), Times.Never);
        _outputPort.Verify(o => o.Processado(), Times.Once);
    }
}
