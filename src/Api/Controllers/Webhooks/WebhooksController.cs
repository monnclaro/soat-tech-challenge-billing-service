using Api.Controllers.Webhooks.Requests;
using Api.Presenters.Pagamentos;
using Application.Pagamentos.Controllers;
using Application.Pagamentos.UseCases.RegistrarWebhookPagamento;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.Webhooks;

// Sem [Authorize]: quem chama este endpoint é o Mercado Pago, não um cliente autenticado
// com o JWT compartilhado do back-office — a autenticidade é garantida pela validação de
// assinatura HMAC (x-signature/x-request-id), não por Bearer token. Substitui o antigo
// OrcamentoWebhookController mockado do monolito de origem.
[ApiController]
[Route("api/v1/webhooks/mercadopago")]
[AllowAnonymous]
[Produces("application/json")]
public class WebhooksController : ControllerBase
{
    private readonly PagamentoController _controller;
    private readonly RegistrarWebhookPagamentoPresenter _presenter;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(PagamentoController controller, RegistrarWebhookPagamentoPresenter presenter, ILogger<WebhooksController> logger)
    {
        _controller = controller;
        _presenter = presenter;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Receber(
        [FromBody] MercadoPagoWebhookRequest? body,
        [FromQuery(Name = "data.id")] string? dataIdQuery,
        [FromQuery] string? type,
        [FromHeader(Name = "x-signature")] string? xSignature,
        [FromHeader(Name = "x-request-id")] string? xRequestId,
        CancellationToken ct)
    {
        var tipo = body?.Type ?? body?.Action ?? type;
        // Prioriza a query string, não o corpo: os SDKs oficiais do Mercado Pago montam o
        // manifesto da assinatura (ver WebhookSignatureValidator) a partir do "data.id" da
        // query string (ex.: PHP lê "$_GET['data_id']"), não do corpo JSON — mesmo quando o
        // corpo também traz esse campo. Usar o valor errado aqui faz a assinatura calculada
        // nunca bater, mesmo com o secret certo.
        var dataId = dataIdQuery ?? body?.Data?.Id;

        // TODO(diagnostico-temporario): remover depois de confirmar a causa do 400 em
        // produção — sem isso não há visibilidade do que o Mercado Pago realmente envia
        // (content-type, se o body bindou, etc.) sem acesso direto ao log do pod.
        _logger.LogInformation(
            "Webhook Mercado Pago recebido: contentType={ContentType} bodyNulo={BodyNulo} " +
            "bodyType={BodyType} bodyAction={BodyAction} queryType={QueryType} tipoResolvido={Tipo} " +
            "dataIdBody={DataIdBody} dataIdQuery={DataIdQuery} dataIdResolvido={DataId} " +
            "temXSignature={TemXSignature} temXRequestId={TemXRequestId}",
            Request.ContentType, body is null, body?.Type, body?.Action, type, tipo,
            body?.Data?.Id, dataIdQuery, dataId,
            !string.IsNullOrWhiteSpace(xSignature), !string.IsNullOrWhiteSpace(xRequestId));

        await _controller.RegistrarWebhook(new RegistrarWebhookPagamentoInput(tipo, dataId, xSignature, xRequestId), ct);
        return _presenter.Result!;
    }
}
