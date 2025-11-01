using DDDPlayground.Domain.Orders;
using DDDPlayground.Domain.ValueObjects;
using DDDPlayground.Infrastructure.Persistence.Mappers;
using Microsoft.EntityFrameworkCore;

namespace DDDPlayground.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation demonstrating domain/persistence layer separation.
/// 
/// Key Patterns:
/// - Implements domain interface (IOrderRepository in Domain layer)
/// - Works with domain models (Order) in its public API
/// - Uses persistence entities (OrderEntity) internally
/// - Performs explicit mapping between layers
/// - Encapsulates all EF Core concerns from domain
/// </summary>
public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public OrderRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetByIdAsync(OrderId id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id.Value, cancellationToken);

        return entity is null ? null : OrderMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<Order>> GetByCustomerIdAsync(CustomerId customerId, CancellationToken cancellationToken = default)
    {
        var entities = await _context.Orders
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customerId.Value)
            .ToListAsync(cancellationToken);

        return entities.Select(OrderMapper.ToDomain).ToList();
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        var entity = OrderMapper.ToEntity(order);
        await _context.Orders.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Orders
         .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == order.Id.Value, cancellationToken);

        if (entity is null)
        {
            return;
        }

        // Update scalar properties
        entity.Status = order.Status.ToString();
        entity.SubtotalAmount = order.Subtotal.Amount;
        entity.SubtotalCurrency = order.Subtotal.Currency;
        entity.DiscountAmount = order.Discount.Amount;
        entity.DiscountCurrency = order.Discount.Currency;
        entity.TotalAmount = order.Total.Amount;
        entity.TotalCurrency = order.Total.Currency;
        entity.ConfirmedAt = order.ConfirmedAt;

        // For items, instead of clearing and recreating, we'll:
        // 1. Remove items that no longer exist in domain
        // 2. Update existing items that match by ProductId
        // 3. Add new items

        var existingItems = entity.Items.ToList();
        var domainItems = order.Items.ToList();

        // Remove items that no longer exist in domain
        var itemsToRemove = existingItems
            .Where(existing => !domainItems.Any(domain => domain.ProductId.Value == existing.ProductId))
            .ToList();

        foreach (var itemToRemove in itemsToRemove)
        {
            entity.Items.Remove(itemToRemove);
        }

        // Update existing items and add new ones
        foreach (var domainItem in domainItems)
        {
            var existingItem = existingItems
                .FirstOrDefault(e => e.ProductId == domainItem.ProductId.Value);

            if (existingItem is not null)
            {
                // Update existing item
                existingItem.Quantity = domainItem.Quantity;
                existingItem.UnitPriceAmount = domainItem.UnitPrice.Amount;
                existingItem.UnitPriceCurrency = domainItem.UnitPrice.Currency;
            }
            else
            {
                // Add new item
                var newItemEntity = OrderItemMapper.ToEntity(domainItem);
                newItemEntity.OrderId = entity.Id;
                entity.Items.Add(newItemEntity);
            }
        }
    }

    public async Task DeleteAsync(OrderId id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Orders
            .Include(o => o.Items) // Include items for proper cascade deletion
            .FirstOrDefaultAsync(o => o.Id == id.Value, cancellationToken);

        if (entity is not null)
        {
            _context.Orders.Remove(entity);
        }
    }
}
