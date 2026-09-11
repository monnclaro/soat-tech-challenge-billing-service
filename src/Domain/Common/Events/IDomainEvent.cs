namespace Domain.Common.Events;

public interface IDomainEvent
{
    DateTime OcurredAt { get; }
};
