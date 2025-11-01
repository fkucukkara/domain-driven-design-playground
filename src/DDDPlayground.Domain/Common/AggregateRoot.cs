namespace DDDPlayground.Domain.Common;

/// <summary>
/// Base class for aggregates that can raise domain events.
/// Provides infrastructure for collecting and dispatching events.
/// 
/// Key DDD Pattern: Domain Events in Aggregates
/// - Events are collected during business operations
/// - Events are dispatched after successful persistence
/// - Ensures consistency between state changes and event publishing
/// </summary>
public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>
    /// Domain events raised by this aggregate.
    /// Read-only collection to prevent external modification.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Add a domain event to be published.
    /// Called by aggregate methods when significant business events occur.
    /// </summary>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clear all domain events.
    /// Called after events have been successfully published.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}