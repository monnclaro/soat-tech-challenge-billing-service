using Domain.Common.Events;
using Infrastructure.DomainEvents;

namespace Infrastructure.Database.Helpers;

public class NoopDomainEventsDispatcher : IDomainEventsDispatcher
{
    public Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
