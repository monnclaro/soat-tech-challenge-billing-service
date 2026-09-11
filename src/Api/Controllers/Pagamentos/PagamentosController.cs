using Api.Presenters.Pagamentos;
using Application.Pagamentos.Controllers;
using Application.Pagamentos.UseCases;
using Application.Pagamentos.UseCases.BuscarPagamento;
using Application.Pagamentos.UseCases.BuscarPagamentoPorOrcamento;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.Pagamentos;

[ApiController]
[Route("api/v1/pagamentos")]
[Authorize]
[Produces("application/json")]
public class PagamentosController : ControllerBase
{
    private readonly PagamentoController _controller;
    private readonly BuscarPagamentoPresenter _buscarPresenter;
    private readonly BuscarPagamentoPorOrcamentoPresenter _buscarPorOrcamentoPresenter;

    public PagamentosController(
        PagamentoController controller,
        BuscarPagamentoPresenter buscarPresenter,
        BuscarPagamentoPorOrcamentoPresenter buscarPorOrcamentoPresenter)
    {
        _controller = controller;
        _buscarPresenter = buscarPresenter;
        _buscarPorOrcamentoPresenter = buscarPorOrcamentoPresenter;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PagamentoOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Buscar([FromRoute] Guid id, CancellationToken ct)
    {
        await _controller.Buscar(new BuscarPagamentoInput(id), ct);
        return _buscarPresenter.Result!;
    }

    [HttpGet("por-orcamento/{idOrcamento:guid}")]
    [ProducesResponseType(typeof(PagamentoOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BuscarPorOrcamento([FromRoute] Guid idOrcamento, CancellationToken ct)
    {
        await _controller.BuscarPorOrcamento(new BuscarPagamentoPorOrcamentoInput(idOrcamento), ct);
        return _buscarPorOrcamentoPresenter.Result!;
    }
}
