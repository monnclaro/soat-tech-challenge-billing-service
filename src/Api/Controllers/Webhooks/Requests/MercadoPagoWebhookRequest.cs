using System.Text.Json.Serialization;

namespace Api.Controllers.Webhooks.Requests;

// Formato "IPN v2" do Mercado Pago (JSON no corpo do POST). O mesmo webhook também pode
// receber os dados via query string (?data.id=...&type=...) em notificações legadas — ver
// tratamento em WebhooksController.
public record MercadoPagoWebhookRequest(
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("action")] string? Action,
    [property: JsonPropertyName("data")] MercadoPagoWebhookDataRequest? Data);

public record MercadoPagoWebhookDataRequest(
    [property: JsonPropertyName("id")] string? Id);
