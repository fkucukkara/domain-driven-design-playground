namespace DDDPlayground.Domain.ValueObjects;

/// <summary>
/// Value Object representing a monetary amount with currency.
/// Immutable and ensures value equality, not reference equality.
/// Optimized for reduced memory allocations with cached common instances.
/// </summary>
public sealed record Money
{
    // Performance: Cache common zero values to reduce allocations
    private static readonly Dictionary<string, Money> ZeroCache = new()
    {
        ["USD"] = new Money(0, "USD", skipValidation: true),
        ["EUR"] = new Money(0, "EUR", skipValidation: true),
        ["GBP"] = new Money(0, "GBP", skipValidation: true)
    };

    public decimal Amount { get; init; }
    public string Currency { get; init; }

    public Money(decimal amount, string currency) : this(amount, currency, skipValidation: false)
    {
    }

    // Performance: Internal constructor to skip validation for cached instances
    private Money(decimal amount, string currency, bool skipValidation)
    {
        if (!skipValidation)
        {
            if (amount < 0)
            {
                throw new ArgumentException("Amount cannot be negative", nameof(amount));
            }

            if (string.IsNullOrWhiteSpace(currency))
            {
                throw new ArgumentException("Currency cannot be empty", nameof(currency));
            }
        }

        Amount = amount;
        Currency = skipValidation ? currency : currency.ToUpperInvariant();
    }

    // Performance: Use cached instances for common zero values
    public static Money Zero(string currency = "USD") => 
        ZeroCache.TryGetValue(currency.ToUpperInvariant(), out var cached) 
            ? cached 
            : new Money(0, currency);

    public static Money operator +(Money left, Money right)
    {
        if (left.Currency != right.Currency)
        {
            throw new InvalidOperationException($"Cannot add amounts with different currencies: {left.Currency} and {right.Currency}");
        }

        // Performance: Return zero if result is zero
        var result = left.Amount + right.Amount;
        return result == 0 ? Zero(left.Currency) : new Money(result, left.Currency, skipValidation: true);
    }

    public static Money operator -(Money left, Money right)
    {
        if (left.Currency != right.Currency)
        {
            throw new InvalidOperationException($"Cannot subtract amounts with different currencies: {left.Currency} and {right.Currency}");
        }

        // Performance: Return zero if result is zero
        var result = left.Amount - right.Amount;
        return result == 0 ? Zero(left.Currency) : new Money(result, left.Currency, skipValidation: true);
    }

    public static Money operator *(Money money, decimal multiplier)
    {
        var result = money.Amount * multiplier;
        return result == 0 ? Zero(money.Currency) : new Money(result, money.Currency, skipValidation: true);
    }

    public override string ToString() => $"{Amount:N2} {Currency}";
}
