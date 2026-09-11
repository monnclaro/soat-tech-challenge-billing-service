using Application.Pagamentos.UseCases.BuscarPagamento;
using Application.Pagamentos.UseCases.BuscarPagamentoPorOrcamento;
using Application.Pagamentos.UseCases.RegistrarWebhookPagamento;
using SharedKernel.Interfaces;

namespace Application.Pagamentos.Controllers;

public class PagamentoController : IScoped
{
    private readonly BuscarPagamentoUseCase _buscar;
    private readonly BuscarPagamentoPorOrcamentoUseCase _buscarPorOrcamento;
    private readonly RegistrarWebhookPagamentoUseCase _registrarWebhook;

    public PagamentoController(
        BuscarPagamentoUseCase buscar,
        BuscarPagamentoPorOrcamentoUseCase buscarPorOrcamento,
        RegistrarWebhookPagamentoUseCase registrarWebhook)
    {
        _buscar = buscar;
        _buscarPorOrcamento = buscarPorOrcamento;
        _registrarWebhook = registrarWebhook;
    }

    public async Task Buscar(BuscarPagamentoInput input, CancellationToken ct = default)
        => await _buscar.Execute(input, ct);

    public async Task BuscarPorOrcamento(BuscarPagamentoPorOrcamentoInput input, CancellationToken ct = default)
        => await _buscarPorOrcamento.Execute(input, ct);

    public async Task RegistrarWebhook(RegistrarWebhookPagamentoInput input, CancellationToken ct = default)
        => await _registrarWebhook.Execute(input, ct);
}
