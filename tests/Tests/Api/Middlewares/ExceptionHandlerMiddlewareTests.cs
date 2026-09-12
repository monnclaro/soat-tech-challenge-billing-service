using System.Text.Json;
using Api.Common.Exceptions;
using Api.Middlewares;
using Domain.Common.Exceptions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Tests.Api.Middlewares;

public class ExceptionHandlerMiddlewareTests
{
    private static async Task<(int statusCode, string body)> ExecutarComExceptionAsync(Exception? exception)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        RequestDelegate next = _ => exception is null ? Task.CompletedTask : throw exception;

        var middleware = new ExceptionHandlerMiddleware(next, NullLogger<ExceptionHandlerMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();

        return (context.Response.StatusCode, body);
    }

    [Fact]
    public async Task InvokeAsync_QuandoNaoHaExcecao_DevePropagarStatusPadrao()
    {
        var (statusCode, _) = await ExecutarComExceptionAsync(null);

        statusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task InvokeAsync_ComDomainException_DeveRetornar400ComAMensagem()
    {
        var (statusCode, body) = await ExecutarComExceptionAsync(new DomainException("erro de domínio"));

        statusCode.Should().Be(StatusCodes.Status400BadRequest);
        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("erro").GetString().Should().Be("erro de domínio");
    }

    [Fact]
    public async Task InvokeAsync_ComNotFoundException_DeveRetornar404ComAMensagem()
    {
        var (statusCode, body) = await ExecutarComExceptionAsync(new NotFoundException("não encontrado"));

        statusCode.Should().Be(StatusCodes.Status404NotFound);
        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("erro").GetString().Should().Be("não encontrado");
    }

    [Fact]
    public async Task InvokeAsync_ComConflictException_DeveRetornar409ComAMensagem()
    {
        var (statusCode, body) = await ExecutarComExceptionAsync(new ConflictException("conflito"));

        statusCode.Should().Be(StatusCodes.Status409Conflict);
        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("erro").GetString().Should().Be("conflito");
    }

    [Fact]
    public async Task InvokeAsync_ComExcecaoNaoMapeada_DeveRetornar500ComMensagemGenerica()
    {
        var (statusCode, body) = await ExecutarComExceptionAsync(new InvalidOperationException("boom"));

        statusCode.Should().Be(StatusCodes.Status500InternalServerError);
        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("erro").GetString().Should().Be("Ocorreu um erro interno. Tente novamente.");
    }
}
