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

    public WebhooksController(PagamentoController controller, RegistrarWebhookPagamentoPresenter presenter)
    {
        _controller = controller;
        _presenter = presenter;
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
        var dataId = body?.Data?.Id ?? dataIdQuery;

        await _controller.RegistrarWebhook(new RegistrarWebhookPagamentoInput(tipo, dataId, xSignature, xRequestId), ct);
        return _presenter.Result!;
    }
}
