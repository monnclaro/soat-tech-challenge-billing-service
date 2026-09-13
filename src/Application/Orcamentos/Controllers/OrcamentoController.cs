using Application.Orcamentos.UseCases.BuscarOrcamento;
using Application.Orcamentos.UseCases.GerarOrcamento;
using SharedKernel.Interfaces;

namespace Application.Orcamentos.Controllers;

public class OrcamentoController : IScoped
{
    private readonly GerarOrcamentoUseCase _gerar;
    private readonly BuscarOrcamentoUseCase _buscar;

    public OrcamentoController(GerarOrcamentoUseCase gerar, BuscarOrcamentoUseCase buscar)
    {
        _gerar = gerar;
        _buscar = buscar;
    }

    public async Task Gerar(GerarOrcamentoInput input, CancellationToken ct = default)
        => await _gerar.Execute(input, ct);

    public async Task Buscar(BuscarOrcamentoInput input, CancellationToken ct = default)
        => await _buscar.Execute(input, ct);
}
