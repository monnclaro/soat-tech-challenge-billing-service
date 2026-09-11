using Infrastructure.Database.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Database;

/// <summary>
/// Usado só em design-time (ex.: "dotnet ef migrations add") — a connection string real
/// vem de appsettings/ambiente via <see cref="DependencyInjection.AddDatabase"/> em runtime.
/// </summary>
public class BillingServiceDbContextFactory : IDesignTimeDbContextFactory<BillingServiceDbContext>
{
    public BillingServiceDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BillingServiceDbContext>()
            .UseNpgsql("Host=localhost;Database=soat_billing;Username=postgres;Password=postgres")
            .Options;

        return new BillingServiceDbContext(options, new NoopDomainEventsDispatcher());
    }
}
