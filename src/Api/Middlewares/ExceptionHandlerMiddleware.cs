using System.Text.Json;
using Api.Common.Exceptions;
using Domain.Common.Exceptions;

namespace Api.Middlewares;

public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;

    public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (DomainException ex)
        {
            await WriteResponse(ctx, StatusCodes.Status400BadRequest, ex.Message);
        }
        catch (NotFoundException ex)
        {
            await WriteResponse(ctx, StatusCodes.Status404NotFound, ex.Message);
        }
        catch (ConflictException ex)
        {
            await WriteResponse(ctx, StatusCodes.Status409Conflict, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado: {Message}", ex.Message);
            await WriteResponse(ctx, StatusCodes.Status500InternalServerError, "Ocorreu um erro interno. Tente novamente.");
        }
    }

    private static Task WriteResponse(HttpContext ctx, int status, string mensagem)
    {
        ctx.Response.StatusCode  = status;
        ctx.Response.ContentType = "application/json";

        var body = JsonSerializer.Serialize(new { erro = mensagem });
        return ctx.Response.WriteAsync(body);
    }
}
