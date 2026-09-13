using Domain.Common.Events;

namespace Application.Common.Interfaces;

public interface IDomainEventHandler<in T> where T : IDomainEvent
{
    Task Handle(T domainEvent, CancellationToken cancellationToken);
}
