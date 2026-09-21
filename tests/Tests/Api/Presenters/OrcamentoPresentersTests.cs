using Api.Presenters.Orcamentos;
using Application.Orcamentos.UseCases;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Tests.Api.Presenters;

public class GerarOrcamentoPresenterTests
{
    [Fact]
    public void ValorDivergente_DeveRetornarBadRequest()
    {
        var presenter = new GerarOrcamentoPresenter();

        presenter.ValorDivergente("valor divergente");

        presenter.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void OrcamentoJaExiste_DeveRetornarConflict()
    {
        var presenter = new GerarOrcamentoPresenter();

        presenter.OrcamentoJaExiste("já existe");

        presenter.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public void Falha_DeveRetornarBadGateway()
    {
        var presenter = new GerarOrcamentoPresenter();

        presenter.Falha("Mercado Pago indisponível");

        presenter.Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status502BadGateway);
    }

    [Fact]
    public void Ok_DeveRetornarCreatedAtAction()
    {
        var presenter = new GerarOrcamentoPresenter();
        var output = new OrcamentoOutput(Guid.NewGuid(), Guid.NewGuid(), [], 100m, "Pendente", DateTime.UtcNow);

        presenter.Ok(output);

        presenter.Result.Should().BeOfType<CreatedAtActionResult>();
        ((CreatedAtActionResult)presenter.Result!).Value.Should().Be(output);
    }
}

public class BuscarOrcamentoPresenterTests
{
    [Fact]
    public void NaoEncontrado_DeveRetornarNotFound()
    {
        var presenter = new BuscarOrcamentoPresenter();

        presenter.NaoEncontrado();

        presenter.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public void Ok_DeveRetornarOkComOOutput()
    {
        var presenter = new BuscarOrcamentoPresenter();
        var output = new OrcamentoOutput(Guid.NewGuid(), Guid.NewGuid(), [], 100m, "Pendente", DateTime.UtcNow);

        presenter.Ok(output);

        presenter.Result.Should().BeOfType<OkObjectResult>();
        ((OkObjectResult)presenter.Result!).Value.Should().Be(output);
    }
}
