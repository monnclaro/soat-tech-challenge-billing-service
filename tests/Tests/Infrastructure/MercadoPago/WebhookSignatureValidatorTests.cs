using System.Security.Cryptography;
using System.Text;
using Application.Pagamentos.UseCases.Interfaces;
using FluentAssertions;
using Infrastructure.MercadoPago;
using Microsoft.Extensions.Options;

namespace Tests.Infrastructure.MercadoPago;

public class WebhookSignatureValidatorTests
{
    private static WebhookSignatureValidator CriarValidator(string webhookSecret) =>
        new(Options.Create(new MercadoPagoSettings { WebhookSecret = webhookSecret }));

    private static string AssinarManifesto(string secret, string dataId, string xRequestId, string ts)
    {
        var manifesto = $"id:{dataId.ToLowerInvariant()};request-id:{xRequestId};ts:{ts};";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(manifesto));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    [Fact]
    public void EhValida_SemWebhookSecretConfigurado_DeveAceitarSemValidar()
    {
        var validator = CriarValidator(webhookSecret: "");

        var resultado = validator.EhValida(new WebhookSignatureContext("qualquer", "qualquer", "123"));

        resultado.Should().BeTrue();
    }

    [Fact]
    public void EhValida_ComAssinaturaCorreta_DeveRetornarTrue()
    {
        const string secret = "meu-segredo-de-teste";
        const string dataId = "123456789";
        const string xRequestId = "req-abc";
        const string ts = "1700000000";

        var assinatura = AssinarManifesto(secret, dataId, xRequestId, ts);
        var xSignature = $"ts={ts},v1={assinatura}";

        var validator = CriarValidator(secret);

        var resultado = validator.EhValida(new WebhookSignatureContext(xSignature, xRequestId, dataId));

        resultado.Should().BeTrue();
    }

    [Fact]
    public void EhValida_ComAssinaturaIncorreta_DeveRetornarFalse()
    {
        const string secret = "meu-segredo-de-teste";
        const string dataId = "123456789";
        const string xRequestId = "req-abc";
        const string ts = "1700000000";

        var xSignature = $"ts={ts},v1=0000000000000000000000000000000000000000000000000000000000000000";

        var validator = CriarValidator(secret);

        var resultado = validator.EhValida(new WebhookSignatureContext(xSignature, xRequestId, dataId));

        resultado.Should().BeFalse();
    }

    [Theory]
    [InlineData(null, "req-abc")]
    [InlineData("", "req-abc")]
    [InlineData("ts=123,v1=abc", null)]
    [InlineData("ts=123,v1=abc", "")]
    public void EhValida_SemXSignatureOuXRequestId_DeveRetornarFalse(string? xSignature, string? xRequestId)
    {
        var validator = CriarValidator("meu-segredo-de-teste");

        var resultado = validator.EhValida(new WebhookSignatureContext(xSignature, xRequestId, "123456789"));

        resultado.Should().BeFalse();
    }

    [Fact]
    public void EhValida_ComXSignatureSemOsCamposTsOuV1_DeveRetornarFalse()
    {
        var validator = CriarValidator("meu-segredo-de-teste");

        var resultado = validator.EhValida(new WebhookSignatureContext("formato-invalido-sem-chave-valor", "req-abc", "123456789"));

        resultado.Should().BeFalse();
    }
}
