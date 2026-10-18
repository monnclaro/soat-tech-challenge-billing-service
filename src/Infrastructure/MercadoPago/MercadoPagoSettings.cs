namespace Infrastructure.MercadoPago;

// Bind da seção "MercadoPago" do appsettings/ambiente. AccessToken e WebhookSecret devem
// ser credenciais de sandbox/teste em desenvolvimento (README explica como obtê-las) — em
// produção viriam de um secret gerenciado (SSM), nunca de appsettings.json versionado.
public class MercadoPagoSettings
{
    public string AccessToken { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string? NotificationUrl { get; set; }

    // Liga MercadoPagoGatewayFake no lugar do gateway real (SDK oficial, bate na API de
    // verdade) — usado só pelo teste de integração do OS Service (Testcontainers), que
    // sobe este serviço real mas não tem credencial de sandbox pra chamar o Mercado Pago
    // de verdade de forma repetível em CI. Nunca true fora desse cenário.
    public bool UseFake { get; set; }
}
