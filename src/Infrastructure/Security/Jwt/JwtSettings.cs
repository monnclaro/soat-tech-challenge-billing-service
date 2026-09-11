namespace Infrastructure.Security.Jwt;

// Este serviço nunca emite tokens (isso é responsabilidade exclusiva do OS Service via
// AuthenticationController) — apenas valida o JWT Bearer recebido, usando o mesmo segredo
// simétrico compartilhado (ADR 0005 do monolito de origem). Por isso só o binding de
// configuração é reaproveitado aqui, sem ITokenProvider/JwtTokenProvider (que fazem a
// emissão) nem BCryptPasswordHasher (login não existe neste serviço).
public class JwtSettings
{
    public string Secret { get; set; } = null!;
    public int ExpirationHours { get; set; } = 2;
}
