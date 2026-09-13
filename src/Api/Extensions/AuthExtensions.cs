using System.Diagnostics.CodeAnalysis;
using System.Text;
using Infrastructure.Security.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Api.Extensions;

// Este serviço é um resource server puro: nunca emite JWT (isso é papel exclusivo do OS
// Service), apenas valida o Bearer recebido usando o mesmo segredo simétrico compartilhado
// (ADR 0005 do monolito de origem) — por isso este extension method é copiado do OS
// Service, mas o serviço não traz ITokenProvider/JwtTokenProvider (emissão) nem
// BCryptPasswordHasher/Usuario/Login (não existe autenticação local aqui).
// Composition root (registro de DI), sem regra de negócio.
[ExcludeFromCodeCoverage]
public static class AuthExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration
            .GetSection("JwtSettings")
            .Get<JwtSettings>()!;

        var key = Encoding.UTF8.GetBytes(jwtSettings.Secret);

        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

        services
            .AddAuthorization()
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };
            });

        return services;
    }
}
