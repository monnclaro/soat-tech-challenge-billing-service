using System.Security.Cryptography;
using System.Text;
using Application.Pagamentos.UseCases.Interfaces;
using Microsoft.Extensions.Options;

namespace Infrastructure.MercadoPago;

// Implementação real do algoritmo documentado pelo Mercado Pago para validar a origem de
// um webhook, sem depender do SDK (o SDK não expõe essa validação):
// https://www.mercadopago.com.br/developers/pt/docs/checkout-api/additional-content/security/signature
//
// O cabeçalho "x-signature" chega no formato "ts=<epoch>,v1=<hmac_hex>". O manifesto
// assinado é "id:{data.id};request-id:{x-request-id};ts:{ts};" (data.id em minúsculo,
// conforme a documentação), assinado com HMAC-SHA256 usando o "Secret Key" configurado
// no painel de webhooks do Mercado Pago.
public class WebhookSignatureValidator : IWebhookSignatureValidator
{
    private readonly MercadoPagoSettings _settings;

    public WebhookSignatureValidator(IOptions<MercadoPagoSettings> settings)
    {
        _settings = settings.Value;
    }

    public bool EhValida(WebhookSignatureContext context)
    {
        if (string.IsNullOrWhiteSpace(_settings.WebhookSecret))
        {
            // TODO(fase-4-followup): sem "WebhookSecret" configurado não há como validar a
            // assinatura — cenário esperado em ambiente local/sandbox sem o secret real
            // ainda configurado. Em qualquer ambiente com credenciais reais, WebhookSecret
            // deve estar sempre preenchido; sem ele, a notificação é aceita sem validação
            // (documentado no README como limitação do scaffold).
            return true;
        }

        if (string.IsNullOrWhiteSpace(context.XSignature) || string.IsNullOrWhiteSpace(context.XRequestId))
        {
            return false;
        }

        var partes = context.XSignature
            .Split(',')
            .Select(p => p.Split('=', 2))
            .Where(p => p.Length == 2)
            .ToDictionary(p => p[0].Trim(), p => p[1].Trim());

        if (!partes.TryGetValue("ts", out var ts) || !partes.TryGetValue("v1", out var assinaturaRecebida))
        {
            return false;
        }

        var manifesto = $"id:{context.DataId.ToLowerInvariant()};request-id:{context.XRequestId};ts:{ts};";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_settings.WebhookSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(manifesto));
        var assinaturaCalculada = Convert.ToHexString(hash).ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(assinaturaCalculada),
            Encoding.UTF8.GetBytes(assinaturaRecebida));
    }
}
