using System.Diagnostics.CodeAnalysis;
using Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Interfaces;

namespace Application;

// Composition root (registro de DI) — sem regra de negócio, coberto indiretamente pelos
// testes de cada use case/handler registrado aqui.
[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.Scan(scan => scan
            .FromAssemblyOf<IUseCase>()
            .AddClasses(c => c.AssignableTo<IUseCase>())
            .AsSelf()
            .WithScopedLifetime());

        services.Scan(scan => scan
            .FromAssemblyOf<IUseCase>()
            .AddClasses(c => c.AssignableTo<IScoped>())
            .AsSelf()
            .WithScopedLifetime());

        services.Scan(scan => scan
            .FromAssemblyOf<IUseCase>()
            // publicOnly: false — handlers de domain event costumam ser internal ao assembly
            // Application; o Scrutor só escaneia classes públicas por padrão.
            .AddClasses(c => c.AssignableTo(typeof(IDomainEventHandler<>)), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        return services;
    }
}
