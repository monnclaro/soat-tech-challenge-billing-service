using Application.Common.Interfaces;
using Application.Orcamentos.Gateways;
using Application.Orcamentos.UseCases;
using Application.Orcamentos.UseCases.GerarOrcamento;
using Domain.Orcamentos;
using Domain.Orcamentos.Enums;
using Domain.Orcamentos.Gateways;
using Domain.Orcamentos.Itens;
using Domain.Pagamentos;
using Domain.Pagamentos.Gateways;
using FluentAssertions;
using Moq;

namespace Tests.Application.Orcamentos.UseCases;

public class GerarOrcamentoUseCaseTests
{
    private readonly Mock<IOrcamentoGateway> _orcamentoGateway = new();
    private readonly Mock<IPagamentoGateway> _pagamentoGateway = new();
    private readonly Mock<IMercadoPagoGateway> _mercadoPagoGateway = new();
    private readonly Mock<ISagaEventPublisher> _sagaEventPublisher = new();
    private readonly Mock<IGerarOrcamentoOutputPort> _outputPort = new();

    private GerarOrcamentoUseCase CriarUseCase() => new(
        _orcamentoGateway.Object,
        _pagamentoGateway.Object,
        _mercadoPagoGateway.Object,
        _sagaEventPublisher.Object,
        _outputPort.Object);

    private static List<ItemOrcamentoInput> ItensValidos() =>
    [
        new ItemOrcamentoInput(Guid.NewGuid(), "Troca de óleo", 150m, TipoItemOrcamento.Servico),
        new ItemOrcamentoInput(Guid.NewGuid(), "Filtro de óleo", 50m, TipoItemOrcamento.Produto)
    ];

    [Fact]
    public async Task Execute_QuandoOrcamentoNaoExiste_DeveCriarOrcamentoEPagamentoEPublicarEvento()
    {
        var idOrdemServico = Guid.NewGuid();
        var input = new GerarOrcamentoInput(idOrdemServico, ItensValidos(), 200m);

        _orcamentoGateway.Setup(g => g.BuscarPorIdOrdemServico(idOrdemServico, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Orcamento?)null);

        _mercadoPagoGateway.Setup(g => g.CriarPreferencia(It.IsAny<CriarPreferenciaInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PreferenciaCriadaOutput("preference-123", "https://mercadopago.com/init"));

        await CriarUseCase().Execute(input);

        _orcamentoGateway.Verify(g => g.Salvar(It.Is<Orcamento>(o => o.IdOrdemServico == idOrdemServico && o.ValorTotal == 200m), It.IsAny<CancellationToken>()), Times.Once);

        _mercadoPagoGateway.Verify(g => g.CriarPreferencia(
            It.Is<CriarPreferenciaInput>(p => p.IdOrdemServico == idOrdemServico && p.Itens.Count == 2),
            It.IsAny<CancellationToken>()), Times.Once);

        _pagamentoGateway.Verify(g => g.Salvar(It.Is<Pagamento>(p => p.PreferenceId == "preference-123" && p.Valor == 200m), It.IsAny<CancellationToken>()), Times.Once);

        _sagaEventPublisher.Verify(p => p.PublicarOrcamentoGerado(
            idOrdemServico, It.IsAny<Guid>(), 200m, "https://mercadopago.com/init", It.IsAny<CancellationToken>()), Times.Once);

        _outputPort.Verify(o => o.Ok(It.Is<OrcamentoOutput>(output =>
            output.IdOrdemServico == idOrdemServico &&
            output.ValorTotal == 200m &&
            output.LinkPagamento == "https://mercadopago.com/init")), Times.Once);

        _outputPort.Verify(o => o.ValorDivergente(It.IsAny<string>()), Times.Never);
        _outputPort.Verify(o => o.OrcamentoJaExiste(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Execute_QuandoValorTotalDivergeDaSomaDosItens_DeveRetornarValorDivergenteENaoSalvarNadaNemChamarMercadoPago()
    {
        var idOrdemServico = Guid.NewGuid();
        var input = new GerarOrcamentoInput(idOrdemServico, ItensValidos(), 999m);

        _orcamentoGateway.Setup(g => g.BuscarPorIdOrdemServico(idOrdemServico, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Orcamento?)null);

        await CriarUseCase().Execute(input);

        _outputPort.Verify(o => o.ValorDivergente(It.IsAny<string>()), Times.Once);
        _orcamentoGateway.Verify(g => g.Salvar(It.IsAny<Orcamento>(), It.IsAny<CancellationToken>()), Times.Never);
        _mercadoPagoGateway.Verify(g => g.CriarPreferencia(It.IsAny<CriarPreferenciaInput>(), It.IsAny<CancellationToken>()), Times.Never);
        _pagamentoGateway.Verify(g => g.Salvar(It.IsAny<Pagamento>(), It.IsAny<CancellationToken>()), Times.Never);
        _sagaEventPublisher.Verify(p => p.PublicarOrcamentoGerado(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Execute_QuandoOrcamentoEPagamentoJaExistem_DeveCurtoCircuitarComoDuplicado()
    {
        var idOrdemServico = Guid.NewGuid();
        var input = new GerarOrcamentoInput(idOrdemServico, ItensValidos(), 200m);

        var orcamentoExistente = new Orcamento();
        orcamentoExistente.Gerar(idOrdemServico, [new OrcamentoItem(Guid.NewGuid(), "Item", 200m, TipoItemOrcamento.Servico)]);

        var pagamentoExistente = new Pagamento();
        pagamentoExistente.Criar(orcamentoExistente.Id, "preference-existente", 200m);

        _orcamentoGateway.Setup(g => g.BuscarPorIdOrdemServico(idOrdemServico, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orcamentoExistente);
        _pagamentoGateway.Setup(g => g.BuscarPorIdOrcamento(orcamentoExistente.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagamentoExistente);

        await CriarUseCase().Execute(input);

        _outputPort.Verify(o => o.OrcamentoJaExiste(It.IsAny<string>()), Times.Once);
        _outputPort.Verify(o => o.Ok(It.IsAny<OrcamentoOutput>()), Times.Never);
        _outputPort.Verify(o => o.ValorDivergente(It.IsAny<string>()), Times.Never);

        _orcamentoGateway.Verify(g => g.Salvar(It.IsAny<Orcamento>(), It.IsAny<CancellationToken>()), Times.Never);
        _mercadoPagoGateway.Verify(g => g.CriarPreferencia(It.IsAny<CriarPreferenciaInput>(), It.IsAny<CancellationToken>()), Times.Never);
        _pagamentoGateway.Verify(g => g.Salvar(It.IsAny<Pagamento>(), It.IsAny<CancellationToken>()), Times.Never);
        _sagaEventPublisher.Verify(p => p.PublicarOrcamentoGerado(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Execute_QuandoOrcamentoExisteMasPagamentoNao_DeveRetomarCriandoOPagamentoSemRecriarOOrcamento()
    {
        // Este é o cenário de retomada: uma tentativa anterior salvou o Orcamento mas falhou
        // antes de concluir a integração com o Mercado Pago / criar o Pagamento (ex.: a
        // própria chamada ao Mercado Pago falhou). Reprocessar a mensagem não deve tratar
        // isso como duplicado (não existe Pagamento ainda) nem tentar recriar o Orcamento —
        // deve reaproveitar o Orcamento já persistido e seguir o fluxo até criar o Pagamento.
        var idOrdemServico = Guid.NewGuid();
        var input = new GerarOrcamentoInput(idOrdemServico, ItensValidos(), 200m);

        var orcamentoExistente = new Orcamento();
        orcamentoExistente.Gerar(idOrdemServico, [new OrcamentoItem(Guid.NewGuid(), "Item existente", 200m, TipoItemOrcamento.Servico)]);

        _orcamentoGateway.Setup(g => g.BuscarPorIdOrdemServico(idOrdemServico, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orcamentoExistente);
        _pagamentoGateway.Setup(g => g.BuscarPorIdOrcamento(orcamentoExistente.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Pagamento?)null);

        _mercadoPagoGateway.Setup(g => g.CriarPreferencia(It.IsAny<CriarPreferenciaInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PreferenciaCriadaOutput("preference-retomada", "https://mercadopago.com/retomada"));

        await CriarUseCase().Execute(input);

        // Não recria o Orcamento — reaproveita o existente.
        _orcamentoGateway.Verify(g => g.Salvar(It.IsAny<Orcamento>(), It.IsAny<CancellationToken>()), Times.Never);

        _mercadoPagoGateway.Verify(g => g.CriarPreferencia(
            It.Is<CriarPreferenciaInput>(p => p.IdOrdemServico == idOrdemServico),
            It.IsAny<CancellationToken>()), Times.Once);

        _pagamentoGateway.Verify(g => g.Salvar(
            It.Is<Pagamento>(p => p.IdOrcamento == orcamentoExistente.Id && p.PreferenceId == "preference-retomada"),
            It.IsAny<CancellationToken>()), Times.Once);

        _sagaEventPublisher.Verify(p => p.PublicarOrcamentoGerado(
            idOrdemServico, orcamentoExistente.Id, orcamentoExistente.ValorTotal, "https://mercadopago.com/retomada", It.IsAny<CancellationToken>()), Times.Once);

        _outputPort.Verify(o => o.Ok(It.Is<OrcamentoOutput>(output => output.Id == orcamentoExistente.Id)), Times.Once);
        _outputPort.Verify(o => o.OrcamentoJaExiste(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Execute_QuandoMercadoPagoFalha_DevePublicarOrcamentoFalhouENaoSalvarPagamento()
    {
        // Caminho de compensação: se a chamada ao Mercado Pago falhar (rede, API fora do
        // ar), a saga precisa ser avisada (OrcamentoFalhou) para o OS Service cancelar a OS
        // — em vez de deixar a mensagem cair silenciosamente na fila de erro do RabbitMQ.
        var idOrdemServico = Guid.NewGuid();
        var input = new GerarOrcamentoInput(idOrdemServico, ItensValidos(), 200m);

        _orcamentoGateway.Setup(g => g.BuscarPorIdOrdemServico(idOrdemServico, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Orcamento?)null);

        _mercadoPagoGateway.Setup(g => g.CriarPreferencia(It.IsAny<CriarPreferenciaInput>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Mercado Pago indisponível"));

        await CriarUseCase().Execute(input);

        _sagaEventPublisher.Verify(p => p.PublicarOrcamentoFalhou(idOrdemServico, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _outputPort.Verify(o => o.Falha(It.IsAny<string>()), Times.Once);
        _outputPort.Verify(o => o.Ok(It.IsAny<OrcamentoOutput>()), Times.Never);

        // O Orcamento já persistido não é desfeito — fica disponível para retomada manual.
        _pagamentoGateway.Verify(g => g.Salvar(It.IsAny<Pagamento>(), It.IsAny<CancellationToken>()), Times.Never);
        _sagaEventPublisher.Verify(p => p.PublicarOrcamentoGerado(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
