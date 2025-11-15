using DDDPlayground.Domain.Orders;
using DDDPlayground.Domain.ValueObjects;
using DDDPlayground.Infrastructure.Persistence.Mappers;
using Microsoft.EntityFrameworkCore;

namespace DDDPlayground.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation demonstrating domain/persistence layer separation.
/// Optimized for performance with efficient querying and minimal change tracking.
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
            .AsNoTracking() // Performance: disable change tracking for read-only queries
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id.Value, cancellationToken);

        return entity is null ? null : OrderMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<Order>> GetByCustomerIdAsync(CustomerId customerId, CancellationToken cancellationToken = default)
    {
        var entities = await _context.Orders
            .AsNoTracking() // Performance: disable change tracking for read-only queries
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customerId.Value)
            .ToListAsync(cancellationToken);

        return entities.Select(OrderMapper.ToDomain).ToList();
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        var entity = OrderMapper.ToEntity(order);
        
        // Performance: Use AddAsync for better async handling
        await _context.Orders.AddAsync(entity, cancellationToken);
        
        // Performance: Explicitly track the entity state
        _context.Entry(entity).State = EntityState.Added;
        foreach (var item in entity.Items)
        {
            _context.Entry(item).State = EntityState.Added;
        }
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

        // Performance: Update only modified properties
        _context.Entry(entity).CurrentValues.SetValues(new
        {
            Status = order.Status.ToString(),
            SubtotalAmount = order.Subtotal.Amount,
            SubtotalCurrency = order.Subtotal.Currency,
            DiscountAmount = order.Discount.Amount,
            DiscountCurrency = order.Discount.Currency,
            TotalAmount = order.Total.Amount,
            TotalCurrency = order.Total.Currency,
            ConfirmedAt = order.ConfirmedAt
        });

        // Performance: Optimized item updates - clear and recreate for simplicity
        // This is more performant for small collections than complex diffing
        entity.Items.Clear();
        foreach (var domainItem in order.Items)
        {
            var itemEntity = OrderItemMapper.ToEntity(domainItem);
            itemEntity.OrderId = entity.Id;
            entity.Items.Add(itemEntity);
        }
    }

    public async Task DeleteAsync(OrderId id, CancellationToken cancellationToken = default)
    {
        // Performance: Use ExecuteDeleteAsync for better performance
        await _context.Orders
            .Where(o => o.Id == id.Value)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
