using Api.Extensions.Markers;
using Application.Pagamentos.UseCases;
using Application.Pagamentos.UseCases.BuscarPagamento;
using Microsoft.AspNetCore.Mvc;

namespace Api.Presenters.Pagamentos;

public class BuscarPagamentoPresenter : IBuscarPagamentoOutputPort, IPresenter
{
    public IActionResult? Result { get; private set; }

    public void NaoEncontrado() => Result = new NotFoundResult();

    public void Ok(PagamentoOutput output) => Result = new OkObjectResult(output);
}
