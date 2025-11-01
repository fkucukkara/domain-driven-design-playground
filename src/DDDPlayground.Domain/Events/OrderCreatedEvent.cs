using DDDPlayground.Domain.Common;
using DDDPlayground.Domain.ValueObjects;

namespace DDDPlayground.Domain.Events;

/// <summary>
/// Domain Event raised when a new order is created.
/// 
/// Key DDD Pattern: Domain Events
/// - Represents something significant that happened in the domain
/// - Pure domain event (no infrastructure dependencies)
/// - Named in past tense (OrderCreated, not OrderCreating)
/// </summary>
public sealed record OrderCreatedEvent : IDomainEvent
{
    public OrderId OrderId { get; init; }
    public CustomerId CustomerId { get; init; }
    public int ItemCount { get; init; }
    public Money Total { get; init; }
    public DateTime OccurredAt { get; init; }

    public OrderCreatedEvent(OrderId orderId, CustomerId customerId, int itemCount, Money total)
    {
        OrderId = orderId;
        CustomerId = customerId;
        ItemCount = itemCount;
        Total = total;
        OccurredAt = DateTime.UtcNow;
    }
}
