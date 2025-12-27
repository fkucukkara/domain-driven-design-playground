using DDDPlayground.Application.Common;
using DDDPlayground.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace DDDPlayground.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core DbContext for DDD Playground.
/// Contains only persistence entities (OrderEntity), not domain models (Order).
/// Implements IUnitOfWork for transaction boundaries.
/// Optimized for performance with change tracking and batching improvements.
/// </summary>
public class AppDbContext : DbContext, IUnitOfWork
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<OrderEntity> Orders => Set<OrderEntity>();
    public DbSet<OrderItemEntity> OrderItems => Set<OrderItemEntity>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Performance optimizations
            optionsBuilder.EnableSensitiveDataLogging(false)
                         .EnableDetailedErrors(false);
        }

        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from the assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    /// <summary>
    /// IUnitOfWork implementation - commits all changes in a single transaction.
    /// Optimized with change detection disabled for performance.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Optimize change tracking for better performance
        ChangeTracker.AutoDetectChangesEnabled = false;

        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            ChangeTracker.AutoDetectChangesEnabled = true;
        }
    }
}
