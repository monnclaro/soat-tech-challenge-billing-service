using Application.Common.Interfaces;
using Application.Orcamentos.Gateways;
using Domain.Orcamentos;
using Domain.Orcamentos.Gateways;
using Domain.Orcamentos.Itens;
using Domain.Pagamentos;
using Domain.Pagamentos.Gateways;

namespace Application.Orcamentos.UseCases.GerarOrcamento;

public class GerarOrcamentoUseCase : IUseCase
{
    private readonly IOrcamentoGateway _orcamentoGateway;
    private readonly IPagamentoGateway _pagamentoGateway;
    private readonly IMercadoPagoGateway _mercadoPagoGateway;
    private readonly IGerarOrcamentoOutputPort _outputPort;

    public GerarOrcamentoUseCase(
        IOrcamentoGateway orcamentoGateway,
        IPagamentoGateway pagamentoGateway,
        IMercadoPagoGateway mercadoPagoGateway,
        IGerarOrcamentoOutputPort outputPort)
    {
        _orcamentoGateway = orcamentoGateway;
        _pagamentoGateway = pagamentoGateway;
        _mercadoPagoGateway = mercadoPagoGateway;
        _outputPort = outputPort;
    }

    public async Task Execute(GerarOrcamentoInput input, CancellationToken ct = default)
    {
        var existente = await _orcamentoGateway.BuscarPorIdOrdemServico(input.IdOrdemServico, ct);
        if (existente is not null)
        {
            _outputPort.OrcamentoJaExiste($"Já existe um orçamento para a ordem de serviço '{input.IdOrdemServico}'.");
            return;
        }

        var itens = input.Itens
            .Select(i => new OrcamentoItem(i.IdItemOrigem, i.NomeItem, i.Valor, i.Tipo))
            .ToList();

        var orcamento = new Orcamento();
        orcamento.Gerar(input.IdOrdemServico, itens);

        if (orcamento.ValorTotal != input.ValorTotal)
        {
            _outputPort.ValorDivergente(
                $"O valor total informado ({input.ValorTotal:0.00}) não confere com a soma dos itens ({orcamento.ValorTotal:0.00}).");
            return;
        }

        await _orcamentoGateway.Salvar(orcamento, ct);

        var preferencia = await _mercadoPagoGateway.CriarPreferencia(
            new CriarPreferenciaInput(
                input.IdOrdemServico,
                itens.Select(i => new ItemPreferenciaInput(i.NomeItem, i.Valor)).ToList()),
            ct);

        var pagamento = new Pagamento();
        pagamento.Criar(orcamento.Id, preferencia.PreferenceId, orcamento.ValorTotal);

        await _pagamentoGateway.Salvar(pagamento, ct);

        _outputPort.Ok(new OrcamentoOutput(
            orcamento.Id,
            orcamento.IdOrdemServico,
            orcamento.Itens.Select(i => new ItemOrcamentoOutput(i.IdItemOrigem, i.NomeItem, i.Valor, i.Tipo.ToString())).ToList(),
            orcamento.ValorTotal,
            orcamento.Status.ToString(),
            orcamento.DataCriacao,
            preferencia.InitPoint));
    }
}
