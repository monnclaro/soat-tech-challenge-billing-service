using System.Diagnostics.CodeAnalysis;
using Application.Common.Interfaces;
using Application.Pagamentos.UseCases.Interfaces;
using Infrastructure.Database;
using Infrastructure.DomainEvents;
using Infrastructure.MercadoPago;
using Infrastructure.Messaging;
using Infrastructure.Messaging.Consumers;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

// Composition root (registro de DI) — sem regra de negócio, coberto indiretamente pelos
// testes de cada serviço registrado aqui.
[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration) =>
        services
            .AddServices(configuration)
            .AddDatabase(configuration)
            .AddMessaging(configuration);

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

    private static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqSettings>(configuration.GetSection("RabbitMq"));
        services.AddScoped<ISagaEventPublisher, MassTransitSagaEventPublisher>();

        var rabbitMq = configuration.GetSection("RabbitMq").Get<RabbitMqSettings>() ?? new RabbitMqSettings();

        services.AddMassTransit(x =>
        {
            x.AddConsumer<GerarOrcamentoConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMq.Host, rabbitMq.VirtualHost, h =>
                {
                    h.Username(rabbitMq.Username);
                    h.Password(rabbitMq.Password);
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
