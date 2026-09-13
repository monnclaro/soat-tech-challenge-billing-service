using Application.Common.Interfaces;
using Application.Orcamentos.Gateways;
using Domain.Orcamentos;
using Domain.Orcamentos.Enums;
using Domain.Orcamentos.Gateways;
using Domain.Orcamentos.Itens;
using Domain.Pagamentos;
using Domain.Pagamentos.Gateways;
using FluentAssertions;
using Infrastructure.Messaging.Consumers;
using MassTransit;
using Moq;
using Soat.Contracts.Saga;

namespace Tests.Infrastructure.Messaging.Consumers;

public class GerarOrcamentoConsumerTests
{
    private readonly Mock<IOrcamentoGateway> _orcamentoGateway = new();
    private readonly Mock<IPagamentoGateway> _pagamentoGateway = new();
    private readonly Mock<IMercadoPagoGateway> _mercadoPagoGateway = new();
    private readonly Mock<ISagaEventPublisher> _sagaEventPublisher = new();

    private GerarOrcamentoConsumer CriarConsumer() => new(
        _orcamentoGateway.Object,
        _pagamentoGateway.Object,
        _mercadoPagoGateway.Object,
        _sagaEventPublisher.Object);

    private static Mock<ConsumeContext<GerarOrcamento>> CriarContexto(GerarOrcamento mensagem)
    {
        var contexto = new Mock<ConsumeContext<GerarOrcamento>>();
        contexto.SetupGet(c => c.Message).Returns(mensagem);
        contexto.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);
        return contexto;
    }

    [Fact]
    public async Task Consume_ComMensagemValida_DeveExecutarOUseCaseConvertendoServicosEProdutosEmItens()
    {
        var idOrdemServico = Guid.NewGuid();
        var idServico = Guid.NewGuid();
        var idProduto = Guid.NewGuid();

        var mensagem = new GerarOrcamento(
            idOrdemServico,
            [new ItemServicoDiagnosticado(idServico, "Alinhamento", 100m)],
            [new ItemProdutoDiagnosticado(idProduto, "Pastilha de freio", 50m, 2m)],
            200m);

        _orcamentoGateway.Setup(g => g.BuscarPorIdOrdemServico(idOrdemServico, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Orcamento?)null);
        _mercadoPagoGateway.Setup(g => g.CriarPreferencia(It.IsAny<CriarPreferenciaInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PreferenciaCriadaOutput("preference-1", "https://mercadopago.com/init"));

        var contexto = CriarContexto(mensagem);

        await CriarConsumer().Consume(contexto.Object);

        _orcamentoGateway.Verify(g => g.Salvar(
            It.Is<Orcamento>(o => o.IdOrdemServico == idOrdemServico && o.ValorTotal == 200m && o.Itens.Count == 2),
            It.IsAny<CancellationToken>()), Times.Once);

        _mercadoPagoGateway.Verify(g => g.CriarPreferencia(
            It.Is<CriarPreferenciaInput>(p => p.Itens.Count == 2 && p.Itens.Any(i => i.Valor == 100m) && p.Itens.Any(i => i.Valor == 100m)),
            It.IsAny<CancellationToken>()), Times.Once);

        _pagamentoGateway.Verify(g => g.Salvar(It.IsAny<Pagamento>(), It.IsAny<CancellationToken>()), Times.Once);
        _sagaEventPublisher.Verify(p => p.PublicarOrcamentoGerado(idOrdemServico, It.IsAny<Guid>(), 200m, "https://mercadopago.com/init", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Consume_QuandoValorTotalDiverge_DeveLancarInvalidOperationException()
    {
        var idOrdemServico = Guid.NewGuid();

        var mensagem = new GerarOrcamento(
            idOrdemServico,
            [new ItemServicoDiagnosticado(Guid.NewGuid(), "Alinhamento", 100m)],
            [],
            999m);

        _orcamentoGateway.Setup(g => g.BuscarPorIdOrdemServico(idOrdemServico, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Orcamento?)null);

        var contexto = CriarContexto(mensagem);

        var acao = async () => await CriarConsumer().Consume(contexto.Object);

        await acao.Should().ThrowAsync<InvalidOperationException>();
        _mercadoPagoGateway.Verify(g => g.CriarPreferencia(It.IsAny<CriarPreferenciaInput>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Consume_QuandoOrcamentoEPagamentoJaExistem_NaoDeveLancarExceptionNemChamarMercadoPago()
    {
        // Reentrega da mensagem já totalmente processada — idempotente, não é uma falha.
        var idOrdemServico = Guid.NewGuid();
        var mensagem = new GerarOrcamento(
            idOrdemServico,
            [new ItemServicoDiagnosticado(Guid.NewGuid(), "Alinhamento", 100m)],
            [],
            100m);

        var orcamentoExistente = new Orcamento();
        orcamentoExistente.Gerar(idOrdemServico, [new OrcamentoItem(Guid.NewGuid(), "Item", 100m, TipoItemOrcamento.Servico)]);

        var pagamentoExistente = new Pagamento();
        pagamentoExistente.Criar(orcamentoExistente.Id, "preference-existente", 100m);

        _orcamentoGateway.Setup(g => g.BuscarPorIdOrdemServico(idOrdemServico, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orcamentoExistente);
        _pagamentoGateway.Setup(g => g.BuscarPorIdOrcamento(orcamentoExistente.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagamentoExistente);

        var contexto = CriarContexto(mensagem);

        var acao = async () => await CriarConsumer().Consume(contexto.Object);

        await acao.Should().NotThrowAsync();
        _mercadoPagoGateway.Verify(g => g.CriarPreferencia(It.IsAny<CriarPreferenciaInput>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
