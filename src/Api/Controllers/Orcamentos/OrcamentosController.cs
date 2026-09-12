using Api.Common.Exceptions;
using Api.Controllers.Orcamentos.Requests;
using Api.Presenters.Orcamentos;
using Application.Orcamentos.Controllers;
using Application.Orcamentos.UseCases;
using Application.Orcamentos.UseCases.BuscarOrcamento;
using Application.Orcamentos.UseCases.GerarOrcamento;
using Domain.Orcamentos.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.Orcamentos;

[ApiController]
[Route("api/v1/orcamentos")]
[Authorize]
[Produces("application/json")]
public class OrcamentosController : ControllerBase
{
    private readonly OrcamentoController _controller;
    private readonly GerarOrcamentoPresenter _gerarPresenter;
    private readonly BuscarOrcamentoPresenter _buscarPresenter;

    public OrcamentosController(
        OrcamentoController controller,
        GerarOrcamentoPresenter gerarPresenter,
        BuscarOrcamentoPresenter buscarPresenter)
    {
        _controller = controller;
        _gerarPresenter = gerarPresenter;
        _buscarPresenter = buscarPresenter;
    }

    [HttpGet("{idOrdemServico:guid}")]
    [ProducesResponseType(typeof(OrcamentoOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Buscar([FromRoute] Guid idOrdemServico, CancellationToken ct)
    {
        await _controller.Buscar(new BuscarOrcamentoInput(idOrdemServico), ct);
        return _buscarPresenter.Result!;
    }

    // Endpoint interno/manual: dispara a geração do orçamento a partir dos itens do
    // diagnóstico. Mantido para depuração/teste manual — em produção, o mesmo fluxo
    // é disparado pelo consumer RabbitMQ/MassTransit do comando "GerarOrcamento".
    [HttpPost]
    [ProducesResponseType(typeof(OrcamentoOutput), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Gerar([FromBody] GerarOrcamentoRequest request, CancellationToken ct)
    {
        var itens = request.Itens
            .Select(i =>
            {
                if (!Enum.TryParse<TipoItemOrcamento>(i.Tipo, ignoreCase: true, out var tipo))
                {
                    throw new Api.Common.Exceptions.ConflictException($"Tipo de item inválido: '{i.Tipo}'. Use 'Servico' ou 'Produto'.");
                }

                return new ItemOrcamentoInput(i.IdItemOrigem, i.Nome, i.Valor, tipo);
            })
            .ToList();

        await _controller.Gerar(new GerarOrcamentoInput(request.IdOrdemServico, itens, request.ValorTotal), ct);
        return _gerarPresenter.Result!;
    }
}
