using DDDPlayground.Application.Orders.Notifications;
using DDDPlayground.Domain.Common;
using DDDPlayground.Domain.Events;
using MediatR;

namespace DDDPlayground.Application.Common;

/// <summary>
/// Service for publishing domain events.
/// Ensures events are published after successful persistence.
/// 
/// Key Pattern: Domain Event Publishing
/// - Events are published after successful database transactions
/// - Ensures consistency between state changes and event publishing
/// - Prevents events from being published if persistence fails
/// </summary>
public interface IDomainEventPublisher
{
    /// <summary>
    /// Publish all domain events from the given aggregate roots.
    /// Supports multiple aggregates modified in a single use case.
    /// </summary>
    Task PublishDomainEventsAsync(IEnumerable<AggregateRoot> aggregateRoots, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publish all domain events from a single aggregate root.
    /// Convenience method for common single-aggregate scenarios.
    /// </summary>
    Task PublishDomainEventsAsync(AggregateRoot aggregateRoot, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of domain event publisher using MediatR.
/// </summary>
public sealed class DomainEventPublisher : IDomainEventPublisher
{
    private readonly IPublisher _publisher;

    public DomainEventPublisher(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task PublishDomainEventsAsync(IEnumerable<AggregateRoot> aggregateRoots, CancellationToken cancellationToken = default)
    {
        // Collect all domain events from all aggregates
        var domainEvents = aggregateRoots
            .SelectMany(aggregate => aggregate.DomainEvents)
            .ToList();

        // Clear events from aggregates (they will be published)
        foreach (var aggregate in aggregateRoots)
        {
            aggregate.ClearDomainEvents();
        }

        // Publish each event by wrapping it in the appropriate MediatR notification
        foreach (var domainEvent in domainEvents)
        {
            var notification = CreateNotification(domainEvent);
            if (notification != null)
            {
                await _publisher.Publish(notification, cancellationToken);
            }
        }
    }

    public async Task PublishDomainEventsAsync(AggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
    {
        // Delegate to the collection overload - no code duplication!
        await PublishDomainEventsAsync(new[] { aggregateRoot }, cancellationToken);
    }

    /// <summary>
    /// Create MediatR notification wrapper for domain events.
    /// This method maps pure domain events to MediatR notifications.
    /// </summary>
    private INotification? CreateNotification(IDomainEvent domainEvent)
    {
        return domainEvent switch
        {
            OrderCreatedEvent orderCreated => new OrderCreatedNotification(orderCreated),
            OrderConfirmedEvent orderConfirmed => new OrderConfirmedNotification(orderConfirmed),
            // Add more domain event mappings here as needed
            _ => null // Unknown event type - could log warning
        };
    }
}