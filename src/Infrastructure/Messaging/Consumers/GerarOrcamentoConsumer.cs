using Application.Common.Interfaces;
using Application.Orcamentos.Gateways;
using Application.Orcamentos.UseCases;
using Application.Orcamentos.UseCases.GerarOrcamento;
using Domain.Orcamentos.Enums;
using Domain.Orcamentos.Gateways;
using Domain.Pagamentos.Gateways;
using MassTransit;
using Soat.Contracts.Saga;

namespace Infrastructure.Messaging.Consumers;

// Consome o comando publicado pelo OS Service ao concluir o registro do diagnóstico,
// reaproveitando o mesmo GerarOrcamentoUseCase usado pelo endpoint interno equivalente
// (única fonte de verdade da regra de negócio, independente do meio de entrada).
public class GerarOrcamentoConsumer : IConsumer<GerarOrcamento>
{
    private readonly IOrcamentoGateway _orcamentoGateway;
    private readonly IPagamentoGateway _pagamentoGateway;
    private readonly IMercadoPagoGateway _mercadoPagoGateway;
    private readonly ISagaEventPublisher _sagaEventPublisher;

    public GerarOrcamentoConsumer(
        IOrcamentoGateway orcamentoGateway,
        IPagamentoGateway pagamentoGateway,
        IMercadoPagoGateway mercadoPagoGateway,
        ISagaEventPublisher sagaEventPublisher)
    {
        _orcamentoGateway = orcamentoGateway;
        _pagamentoGateway = pagamentoGateway;
        _mercadoPagoGateway = mercadoPagoGateway;
        _sagaEventPublisher = sagaEventPublisher;
    }

    public async Task Consume(ConsumeContext<GerarOrcamento> context)
    {
        var msg = context.Message;
        var outputPort = new CapturingOutputPort();
        var useCase = new GerarOrcamentoUseCase(_orcamentoGateway, _pagamentoGateway, _mercadoPagoGateway, _sagaEventPublisher, outputPort);

        var itens = msg.Servicos
            .Select(s => new ItemOrcamentoInput(s.IdServico, s.NomeServico, s.Valor, TipoItemOrcamento.Servico))
            .Concat(msg.Produtos.Select(p => new ItemOrcamentoInput(p.IdProduto, p.NomeProduto, p.ValorUnitario * p.Quantidade, TipoItemOrcamento.Produto)))
            .ToList();

        await useCase.Execute(new GerarOrcamentoInput(msg.IdOrdemServico, itens, msg.ValorTotal), context.CancellationToken);

        // OrcamentoJaExiste é idempotente por natureza (reentrega da mensagem) — não é falha.
        if (outputPort.ValorDivergenteMensagem is { } mensagem)
        {
            throw new InvalidOperationException(mensagem);
        }
    }

    private sealed class CapturingOutputPort : IGerarOrcamentoOutputPort
    {
        public string? ValorDivergenteMensagem { get; private set; }
        public void ValorDivergente(string mensagem) => ValorDivergenteMensagem = mensagem;
        public void OrcamentoJaExiste(string mensagem) { }

        // Falha ao chamar o Mercado Pago já foi tratada dentro do use case (publica
        // OrcamentoFalhou e compensa a saga) — não é um erro de mensageria, a mensagem
        // deve ser considerada processada com sucesso, sem retry/fila de erro.
        public void Falha(string mensagem) { }

        public void Ok(OrcamentoOutput output) { }
    }
}
