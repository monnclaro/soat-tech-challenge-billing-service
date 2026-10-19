using Application.Pagamentos.UseCases.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SdkWebhookSignatureValidator = MercadoPago.Webhook.WebhookSignatureValidator;

namespace Infrastructure.MercadoPago;

// Delega pro validador oficial do SDK (MercadoPago.Webhook.WebhookSignatureValidator) em vez
// de reimplementar o algoritmo HMAC-SHA256 na mão: depois de confirmar manifesto, secret e
// algoritmo todos corretos e a assinatura calculada ainda assim nunca bater com a recebida
// em produção, usar o validador mantido pelo próprio Mercado Pago elimina qualquer
// divergência de formato entre a nossa implementação e o que o backend deles realmente
// espera — é a mesma lógica que PaymentClient/PreferenceClient já usam.
public class WebhookSignatureValidator : IWebhookSignatureValidator
{
    private readonly MercadoPagoSettings _settings;
    private readonly ILogger<WebhookSignatureValidator> _logger;

    public WebhookSignatureValidator(IOptions<MercadoPagoSettings> settings, ILogger<WebhookSignatureValidator> logger)
    {
        _settings = settings.Value;
        _logger = logger;
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
            _logger.LogInformation(
                "Assinatura invalida: x-signature ou x-request-id ausente (xSignatureNula={XSignatureNula} xRequestIdNulo={XRequestIdNulo})",
                string.IsNullOrWhiteSpace(context.XSignature), string.IsNullOrWhiteSpace(context.XRequestId));
            return false;
        }

        try
        {
            SdkWebhookSignatureValidator.Validate(context.XSignature, context.XRequestId, context.DataId, _settings.WebhookSecret);
            return true;
        }
        catch (Exception ex)
        {
            // TODO(diagnostico-temporario): remover depois de confirmar que o validador do
            // SDK resolve o 401 em produção.
            _logger.LogInformation(ex, "Assinatura invalida (validador do SDK rejeitou): dataId={DataId}", context.DataId);
            return false;
        }
    }
}
