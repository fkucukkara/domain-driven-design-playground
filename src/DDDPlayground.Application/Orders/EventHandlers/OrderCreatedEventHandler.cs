using DDDPlayground.Application.Orders.Notifications;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DDDPlayground.Application.Orders.EventHandlers;

/// <summary>
/// Event handler for OrderCreatedNotification.
/// Demonstrates how domain events enable decoupled side effects.
/// 
/// Key Pattern: Domain Event Handler
/// - Handles cross-cutting concerns (logging, notifications, etc.)
/// - Keeps aggregates focused on core business logic
/// - Allows adding new side effects without modifying aggregates
/// </summary>
public sealed class OrderCreatedEventHandler : INotificationHandler<OrderCreatedNotification>
{
    private readonly ILogger<OrderCreatedEventHandler> _logger;

    public OrderCreatedEventHandler(ILogger<OrderCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(OrderCreatedNotification notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        // Log the order creation
        _logger.LogInformation(
            "Order {OrderId} created for customer {CustomerId} with {ItemCount} items and total {Total} {Currency}",
            domainEvent.OrderId.Value,
            domainEvent.CustomerId.Value,
            domainEvent.ItemCount,
            domainEvent.Total.Amount,
            domainEvent.Total.Currency);

        // TODO: In a real application, this could also:
        // - Send welcome/confirmation email
        // - Update analytics/reporting systems
        // - Trigger inventory reservation
        // - Send notifications to mobile apps
        // - Create audit log entries

        return Task.CompletedTask;
    }
}
