using Api.Extensions.Markers;
using Application.Orcamentos.UseCases;
using Application.Orcamentos.UseCases.GerarOrcamento;
using Microsoft.AspNetCore.Mvc;

namespace Api.Presenters.Orcamentos;

public class GerarOrcamentoPresenter : IGerarOrcamentoOutputPort, IPresenter
{
    public IActionResult? Result { get; private set; }

    public void ValorDivergente(string mensagem) =>
        Result = new BadRequestObjectResult(new { erro = mensagem });

    public void OrcamentoJaExiste(string mensagem) =>
        Result = new ConflictObjectResult(new { erro = mensagem });

    // 502: a falha é numa dependência externa (Mercado Pago), não num erro do cliente.
    public void Falha(string mensagem) =>
        Result = new ObjectResult(new { erro = mensagem }) { StatusCode = StatusCodes.Status502BadGateway };

    public void Ok(OrcamentoOutput output) =>
        Result = new CreatedAtActionResult("Buscar", "Orcamentos", new { idOrdemServico = output.IdOrdemServico }, output);
}
