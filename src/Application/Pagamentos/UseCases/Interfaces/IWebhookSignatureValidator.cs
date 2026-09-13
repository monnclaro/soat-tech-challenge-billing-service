namespace Application.Pagamentos.UseCases.Interfaces;

// Porta para validação da assinatura do webhook do Mercado Pago (cabeçalhos x-signature/
// x-request-id, algoritmo HMAC-SHA256 documentado em
// https://www.mercadopago.com.br/developers/pt/docs/checkout-api/additional-content/security/signature).
// Implementação real em Infrastructure.MercadoPago.WebhookSignatureValidator.
public interface IWebhookSignatureValidator
{
    bool EhValida(WebhookSignatureContext context);
}

public record WebhookSignatureContext(string? XSignature, string? XRequestId, string DataId);
