using Domain.Common;
using Domain.Common.Events;
using Domain.Orcamentos;
using Domain.Orcamentos.Itens;
using Domain.Pagamentos;
using Infrastructure.DomainEvents;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Database;

public class BillingServiceDbContext : DbContext
{
    private readonly IDomainEventsDispatcher _dispatcher;

    public BillingServiceDbContext(
        DbContextOptions<BillingServiceDbContext> options,
        IDomainEventsDispatcher dispatcher) : base(options)
    {
        _dispatcher = dispatcher;
    }

    public DbSet<Orcamento> Orcamento { get; set; }
    public DbSet<OrcamentoItem> OrcamentoItem { get; set; }
    public DbSet<Pagamento> Pagamento { get; set; }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        int result = await base.SaveChangesAsync(cancellationToken);

        await PublishDomainEventsAsync();

        return result;
    }

    private async Task PublishDomainEventsAsync()
    {
        var domainEvents = ChangeTracker
            .Entries<Entity>()
            .Select(entry => entry.Entity)
            .SelectMany(entity =>
            {
                List<IDomainEvent> domainEvents = entity.DomainEvents;

                entity.ClearDomainEvents();

                return domainEvents;
            })
            .ToList();

        await _dispatcher.DispatchAsync(domainEvents);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BillingServiceDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
