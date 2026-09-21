using Api.Presenters.Pagamentos;
using Application.Pagamentos.UseCases;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace Tests.Api.Presenters;

public class BuscarPagamentoPresenterTests
{
    [Fact]
    public void NaoEncontrado_DeveRetornarNotFound()
    {
        var presenter = new BuscarPagamentoPresenter();

        presenter.NaoEncontrado();

        presenter.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public void Ok_DeveRetornarOkComOOutput()
    {
        var presenter = new BuscarPagamentoPresenter();
        var output = new PagamentoOutput(Guid.NewGuid(), Guid.NewGuid(), "pref", null, "Pendente", 100m, DateTime.UtcNow, null);

        presenter.Ok(output);

        presenter.Result.Should().BeOfType<OkObjectResult>();
        ((OkObjectResult)presenter.Result!).Value.Should().Be(output);
    }
}

public class BuscarPagamentoPorOrcamentoPresenterTests
{
    [Fact]
    public void NaoEncontrado_DeveRetornarNotFound()
    {
        var presenter = new BuscarPagamentoPorOrcamentoPresenter();

        presenter.NaoEncontrado();

        presenter.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public void Ok_DeveRetornarOkComOOutput()
    {
        var presenter = new BuscarPagamentoPorOrcamentoPresenter();
        var output = new PagamentoOutput(Guid.NewGuid(), Guid.NewGuid(), "pref", null, "Pendente", 100m, DateTime.UtcNow, null);

        presenter.Ok(output);

        presenter.Result.Should().BeOfType<OkObjectResult>();
        ((OkObjectResult)presenter.Result!).Value.Should().Be(output);
    }
}

public class RegistrarWebhookPagamentoPresenterTests
{
    [Fact]
    public void PayloadInvalido_DeveRetornarBadRequest()
    {
        var presenter = new RegistrarWebhookPagamentoPresenter();

        presenter.PayloadInvalido("payload inválido");

        presenter.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void AssinaturaInvalida_DeveRetornarUnauthorized()
    {
        var presenter = new RegistrarWebhookPagamentoPresenter();

        presenter.AssinaturaInvalida();

        presenter.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public void NaoEncontrado_DeveRetornarOk()
    {
        var presenter = new RegistrarWebhookPagamentoPresenter();

        presenter.NaoEncontrado();

        presenter.Result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public void Processado_DeveRetornarOk()
    {
        var presenter = new RegistrarWebhookPagamentoPresenter();

        presenter.Processado();

        presenter.Result.Should().BeOfType<OkResult>();
    }
}
