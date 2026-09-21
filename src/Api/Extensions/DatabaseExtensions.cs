using System.Diagnostics.CodeAnalysis;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Api.Extensions;

// Bootstrap de infraestrutura (aplica migrations no start-up), sem regra de negócio.
[ExcludeFromCodeCoverage]
public static class DatabaseExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<BillingServiceDbContext>();
        await db.Database.MigrateAsync();
    }
}
