namespace Infrastructure.MercadoPago;

// Bind da seção "MercadoPago" do appsettings/ambiente. AccessToken e WebhookSecret devem
// ser credenciais de sandbox/teste em desenvolvimento (README explica como obtê-las) — em
// produção viriam de um secret gerenciado (SSM), nunca de appsettings.json versionado.
public class MercadoPagoSettings
{
    public string AccessToken { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string? NotificationUrl { get; set; }
}
