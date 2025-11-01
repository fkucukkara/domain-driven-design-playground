using DDDPlayground.Domain.Events;
using MediatR;

namespace DDDPlayground.Application.Orders.Notifications;

/// <summary>
/// MediatR notification wrapper for OrderConfirmedEvent.
/// Bridges pure domain events to MediatR infrastructure.
/// 
/// Key Pattern: Domain Event Bridge
/// - Allows domain events to remain pure (no infrastructure dependencies)
/// - Provides MediatR integration in the Application layer
/// </summary>
public sealed record OrderConfirmedNotification : INotification
{
    public OrderConfirmedEvent DomainEvent { get; init; }

    public OrderConfirmedNotification(OrderConfirmedEvent domainEvent)
    {
        DomainEvent = domainEvent;
    }
}