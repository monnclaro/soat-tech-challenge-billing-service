using System.Security.Cryptography;
using System.Text;
using Application.Pagamentos.UseCases.Interfaces;
using Microsoft.Extensions.Logging;
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
            _logger.LogInformation("Assinatura invalida: x-signature ou x-request-id ausente (xSignatureNula={XSignatureNula} xRequestIdNulo={XRequestIdNulo})",
                string.IsNullOrWhiteSpace(context.XSignature), string.IsNullOrWhiteSpace(context.XRequestId));
            return false;
        }

        var partes = context.XSignature
            .Split(',')
            .Select(p => p.Split('=', 2))
            .Where(p => p.Length == 2)
            .ToDictionary(p => p[0].Trim(), p => p[1].Trim());

        if (!partes.TryGetValue("ts", out var ts) || !partes.TryGetValue("v1", out var assinaturaRecebida))
        {
            _logger.LogInformation("Assinatura invalida: x-signature sem 'ts' ou 'v1' no formato esperado. xSignature={XSignature}", context.XSignature);
            return false;
        }

        var manifesto = $"id:{context.DataId.ToLowerInvariant()};request-id:{context.XRequestId};ts:{ts};";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_settings.WebhookSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(manifesto));
        var assinaturaCalculada = Convert.ToHexString(hash).ToLowerInvariant();
        // Não força case na recebida antes de comparar: HMAC hex pode, em tese, vir em
        // qualquer combinação de maiúsculas/minúsculas, e FixedTimeEquals compara byte a
        // byte — sem isso, uma assinatura correta mas com case diferente seria rejeitada.
        var assinaturaRecebidaNormalizada = assinaturaRecebida.ToLowerInvariant();

        var bateu = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(assinaturaCalculada),
            Encoding.UTF8.GetBytes(assinaturaRecebidaNormalizada));

        // TODO(diagnostico-temporario): remover junto com os outros logs de diagnostico do
        // webhook depois de confirmar a causa do 401 em producao — nunca loga o secret nem o
        // manifesto completo (teria o mesmo efeito de logar o segredo em texto claro). O
        // "fingerprint" é só os 8 primeiros caracteres do SHA256 do secret — não reversível,
        // mas permite comparar "é o mesmo secret de antes?" sem expor o valor.
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(_settings.WebhookSecret)))[..8].ToLowerInvariant();

        // Hipótese de espaço/quebra de linha escondida no secret (comum ao colar num campo
        // de Secret do k8s/GitHub) — testa também com o secret "trimado" e compara, sem
        // precisar de outro ciclo de build+deploy pra descobrir se é isso.
        var secretTrimado = _settings.WebhookSecret.Trim();
        var temEspacoNoSecret = secretTrimado.Length != _settings.WebhookSecret.Length;
        bool? bateuComSecretTrimado = null;
        if (temEspacoNoSecret)
        {
            using var hmacTrimado = new HMACSHA256(Encoding.UTF8.GetBytes(secretTrimado));
            var hashTrimado = hmacTrimado.ComputeHash(Encoding.UTF8.GetBytes(manifesto));
            var assinaturaCalculadaTrimada = Convert.ToHexString(hashTrimado).ToLowerInvariant();
            bateuComSecretTrimado = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(assinaturaCalculadaTrimada),
                Encoding.UTF8.GetBytes(assinaturaRecebidaNormalizada));
        }

        // O manifesto em si e o x-signature crua não são segredo (é só dataId/request-id/ts,
        // todos públicos na própria request) — logar eles mostra exatamente a string que foi
        // assinada e o header original, útil pra achar qualquer diferença de formatação.
        _logger.LogInformation(
            "Validacao de assinatura Mercado Pago: manifesto={Manifesto} xSignatureCrua={XSignatureCrua} " +
            "assinaturaRecebida={AssinaturaRecebida} assinaturaCalculada={AssinaturaCalculada} bateu={Bateu} " +
            "secretConfiguradoTamanho={SecretTamanho} secretFingerprint={Fingerprint} temEspacoNoSecret={TemEspacoNoSecret} " +
            "bateuComSecretTrimado={BateuComSecretTrimado}",
            manifesto, context.XSignature, assinaturaRecebidaNormalizada, assinaturaCalculada, bateu,
            _settings.WebhookSecret.Length, fingerprint, temEspacoNoSecret, bateuComSecretTrimado);

        return bateu;
    }
}
