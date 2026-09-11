using Application.Pagamentos.UseCases.Interfaces;
using Infrastructure.Database;
using Infrastructure.DomainEvents;
using Infrastructure.MercadoPago;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration) =>
        services
            .AddServices(configuration)
            .AddDatabase(configuration);

    private static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<IDomainEventsDispatcher, DomainEventsDispatcher>();
        services.AddScoped<IWebhookSignatureValidator, WebhookSignatureValidator>();

        services.Configure<MercadoPagoSettings>(configuration.GetSection("MercadoPago"));

        // Cobre tanto os gateways de persistência (OrcamentoGateway/PagamentoGateway) quanto
        // o gateway de integração externa (MercadoPagoGateway) — todos terminam em "Gateway"
        // por convenção, mesmo padrão já usado no OS Service.
        services.Scan(scan => scan
            .FromApplicationDependencies()
            .AddClasses(c => c.Where(t => t.Name.EndsWith("Gateway")))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        return services;
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("Default");

        services.AddDbContext<BillingServiceDbContext>(options =>
            options.UseNpgsql(connectionString,
                npgsqlOptions => npgsqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName)));

        return services;
    }
}
