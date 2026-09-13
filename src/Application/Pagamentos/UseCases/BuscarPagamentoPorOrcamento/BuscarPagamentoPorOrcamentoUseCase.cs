using Application.Common.Interfaces;
using Domain.Pagamentos.Gateways;

namespace Application.Pagamentos.UseCases.BuscarPagamentoPorOrcamento;

public class BuscarPagamentoPorOrcamentoUseCase : IUseCase
{
    private readonly IPagamentoGateway _gateway;
    private readonly IBuscarPagamentoPorOrcamentoOutputPort _outputPort;

    public BuscarPagamentoPorOrcamentoUseCase(IPagamentoGateway gateway, IBuscarPagamentoPorOrcamentoOutputPort outputPort)
    {
        _gateway = gateway;
        _outputPort = outputPort;
    }

    public async Task Execute(BuscarPagamentoPorOrcamentoInput input, CancellationToken ct = default)
    {
        var pagamento = await _gateway.BuscarPorIdOrcamento(input.IdOrcamento, ct);

        if (pagamento is null)
        {
            _outputPort.NaoEncontrado();
            return;
        }

        _outputPort.Ok(new PagamentoOutput(
            pagamento.Id,
            pagamento.IdOrcamento,
            pagamento.PreferenceId,
            pagamento.PaymentId,
            pagamento.Status.ToString(),
            pagamento.Valor,
            pagamento.DataCriacao,
            pagamento.DataAtualizacao));
    }
}
