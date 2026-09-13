namespace Application.Pagamentos.UseCases.RegistrarWebhookPagamento;

// Payload realista de uma notificação IPN/webhook do Mercado Pago (Checkout Pro):
// https://www.mercadopago.com.br/developers/pt/docs/checkout-api/webhooks
// O corpo traz "type"/"action" + "data.id" (id do pagamento); a autenticidade é garantida
// pelos cabeçalhos x-signature/x-request-id (ver IWebhookSignatureValidator).
public record RegistrarWebhookPagamentoInput(
    string? Tipo,
    string? DataId,
    string? XSignature,
    string? XRequestId);
