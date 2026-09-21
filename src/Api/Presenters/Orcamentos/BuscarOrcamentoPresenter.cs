using Api.Extensions.Markers;
using Application.Orcamentos.UseCases;
using Application.Orcamentos.UseCases.BuscarOrcamento;
using Microsoft.AspNetCore.Mvc;

namespace Api.Presenters.Orcamentos;

public class BuscarOrcamentoPresenter : IBuscarOrcamentoOutputPort, IPresenter
{
    public IActionResult? Result { get; private set; }

    public void NaoEncontrado() => Result = new NotFoundResult();

    public void Ok(OrcamentoOutput output) => Result = new OkObjectResult(output);
}
