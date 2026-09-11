using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Api.Extensions;

public static class DatabaseExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<BillingServiceDbContext>();
        await db.Database.MigrateAsync();
    }
}
