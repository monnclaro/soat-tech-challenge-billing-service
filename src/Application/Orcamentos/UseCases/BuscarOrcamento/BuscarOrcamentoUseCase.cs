using Application.Common.Interfaces;
using Domain.Orcamentos.Gateways;
using Domain.Pagamentos.Gateways;

namespace Application.Orcamentos.UseCases.BuscarOrcamento;

public class BuscarOrcamentoUseCase : IUseCase
{
    private readonly IOrcamentoGateway _gateway;
    private readonly IPagamentoGateway _pagamentoGateway;
    private readonly IBuscarOrcamentoOutputPort _outputPort;

    public BuscarOrcamentoUseCase(IOrcamentoGateway gateway, IPagamentoGateway pagamentoGateway, IBuscarOrcamentoOutputPort outputPort)
    {
        _gateway = gateway;
        _pagamentoGateway = pagamentoGateway;
        _outputPort = outputPort;
    }

    public async Task Execute(BuscarOrcamentoInput input, CancellationToken ct = default)
    {
        var orcamento = await _gateway.BuscarPorIdOrdemServico(input.IdOrdemServico, ct);

        if (orcamento is null)
        {
            _outputPort.NaoEncontrado();
            return;
        }

        // O link de pagamento só existe depois que a preferência é criada no Mercado Pago
        // (ver GerarOrcamentoUseCase) — se a chamada falhou, o Orcamento existe mas o
        // Pagamento ainda não, e o link fica null aqui (consistente com o estado real).
        var pagamento = await _pagamentoGateway.BuscarPorIdOrcamento(orcamento.Id, ct);

        _outputPort.Ok(new OrcamentoOutput(
            orcamento.Id,
            orcamento.IdOrdemServico,
            orcamento.Itens.Select(i => new ItemOrcamentoOutput(i.IdItemOrigem, i.NomeItem, i.Valor, i.Tipo.ToString())).ToList(),
            orcamento.ValorTotal,
            orcamento.Status.ToString(),
            orcamento.DataCriacao,
            pagamento?.LinkPagamento));
    }
}
