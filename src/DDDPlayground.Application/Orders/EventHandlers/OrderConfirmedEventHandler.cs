using DDDPlayground.Application.Orders.Notifications;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DDDPlayground.Application.Orders.EventHandlers;

/// <summary>
/// Event handler for OrderConfirmedNotification.
/// Demonstrates how domain events enable decoupled side effects.
/// 
/// Key Pattern: Domain Event Handler
/// - Handles cross-cutting concerns (logging, notifications, etc.)
/// - Keeps aggregates focused on core business logic
/// - Allows adding new side effects without modifying aggregates
/// </summary>
public sealed class OrderConfirmedEventHandler : INotificationHandler<OrderConfirmedNotification>
{
    private readonly ILogger<OrderConfirmedEventHandler> _logger;

    public OrderConfirmedEventHandler(ILogger<OrderConfirmedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(OrderConfirmedNotification notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        // Log the order confirmation (could also send notifications, update read models, etc.)
        _logger.LogInformation(
            "Order {OrderId} confirmed for customer {CustomerId} with total {Total} {Currency} at {ConfirmedAt}",
            domainEvent.OrderId.Value,
            domainEvent.CustomerId.Value,
            domainEvent.Total.Amount,
            domainEvent.Total.Currency,
            domainEvent.ConfirmedAt);

        // TODO: In a real application, this could also:
        // - Send confirmation email to customer
        // - Update inventory levels
        // - Trigger shipping workflow
        // - Update reporting/analytics systems
        // - Send push notifications

        return Task.CompletedTask;
    }
}