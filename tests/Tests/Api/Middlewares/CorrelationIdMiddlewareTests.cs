using Api.Middlewares;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace Tests.Api.Middlewares;

public class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ComHeaderDeCorrelationIdNaRequisicao_DeveReutilizarOMesmoIdNaResposta()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "correlation-existente";

        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString().Should().Be("correlation-existente");
    }

    [Fact]
    public async Task InvokeAsync_SemHeaderDeCorrelationIdNaRequisicao_DeveGerarUmNovoId()
    {
        var context = new DefaultHttpContext();

        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        var gerado = context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        gerado.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(gerado, out _).Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_ComHeaderEmBranco_DeveGerarUmNovoId()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "   ";

        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        var gerado = context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        Guid.TryParse(gerado, out _).Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_DeveChamarOProximoMiddlewareDaPipeline()
    {
        var context = new DefaultHttpContext();
        var chamouProximo = false;

        var middleware = new CorrelationIdMiddleware(_ =>
        {
            chamouProximo = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        chamouProximo.Should().BeTrue();
    }
}
