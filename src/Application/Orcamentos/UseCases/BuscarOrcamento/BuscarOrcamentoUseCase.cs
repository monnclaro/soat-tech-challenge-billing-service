using Application.Common.Interfaces;
using Domain.Orcamentos.Gateways;

namespace Application.Orcamentos.UseCases.BuscarOrcamento;

public class BuscarOrcamentoUseCase : IUseCase
{
    private readonly IOrcamentoGateway _gateway;
    private readonly IBuscarOrcamentoOutputPort _outputPort;

    public BuscarOrcamentoUseCase(IOrcamentoGateway gateway, IBuscarOrcamentoOutputPort outputPort)
    {
        _gateway = gateway;
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

        _outputPort.Ok(new OrcamentoOutput(
            orcamento.Id,
            orcamento.IdOrdemServico,
            orcamento.Itens.Select(i => new ItemOrcamentoOutput(i.IdItemOrigem, i.NomeItem, i.Valor, i.Tipo.ToString())).ToList(),
            orcamento.ValorTotal,
            orcamento.Status.ToString(),
            orcamento.DataCriacao));
    }
}
