using Application.Common.Interfaces;
using Domain.Pagamentos.Gateways;

namespace Application.Pagamentos.UseCases.BuscarPagamento;

public class BuscarPagamentoUseCase : IUseCase
{
    private readonly IPagamentoGateway _gateway;
    private readonly IBuscarPagamentoOutputPort _outputPort;

    public BuscarPagamentoUseCase(IPagamentoGateway gateway, IBuscarPagamentoOutputPort outputPort)
    {
        _gateway = gateway;
        _outputPort = outputPort;
    }

    public async Task Execute(BuscarPagamentoInput input, CancellationToken ct = default)
    {
        var pagamento = await _gateway.BuscarPorId(input.Id, ct);

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
