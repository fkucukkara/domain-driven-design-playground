namespace DDDPlayground.Domain.Common;

/// <summary>
/// Marker interface for domain events.
/// Domain events represent something significant that happened in the domain.
/// 
/// Key DDD Pattern: Domain Events
/// - Used for in-process communication between aggregates
/// - Allow loose coupling between different parts of the domain
/// - Named in past tense (OrderConfirmed, PaymentProcessed, etc.)
/// - Immutable (record types preferred)
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// When the event occurred.
    /// </summary>
    DateTime OccurredAt { get; }
}