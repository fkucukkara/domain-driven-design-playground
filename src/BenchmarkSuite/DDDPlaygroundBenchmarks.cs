using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using DDDPlayground.Application.Common;
using DDDPlayground.Application.Orders;
using DDDPlayground.Application.Orders.ConfirmOrder;
using DDDPlayground.Application.Orders.CreateOrder;
using DDDPlayground.Application.Orders.GetOrder;
using DDDPlayground.Domain.Common;
using DDDPlayground.Domain.Orders;
using DDDPlayground.Domain.ValueObjects;
using DDDPlayground.Infrastructure.Persistence;
using DDDPlayground.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DDDPlayground.Benchmarks;

/// <summary>
/// Benchmarks for DDD Playground core operations.
/// Tests the performance of order creation, confirmation, and retrieval.
/// </summary>
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.CpuSampling)]
[SimpleJob]
public class DDDPlaygroundBenchmarks
{
    private ServiceProvider _serviceProvider = null!;
    private CreateOrderHandler _createHandler = null!;
    private ConfirmOrderHandler _confirmHandler = null!;
    private GetOrderHandler _getHandler = null!;
    private AppDbContext _dbContext = null!;
    private List<CreateOrderItemDto> _orderItems = null!;
    private Guid _existingOrderId;
    private readonly Guid _customerId = Guid.NewGuid();

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        // Setup in-memory database and services
        var services = new ServiceCollection();
        
        // Add in-memory database
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase($"BenchmarkDb_{Guid.NewGuid()}"));

        // Add repositories and unit of work
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<AppDbContext>());

        // Add domain event publisher (no-op for benchmarks)
        services.AddScoped<IDomainEventPublisher, NoOpDomainEventPublisher>();

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<AppDbContext>();

        // Create handlers directly without MediatR to avoid licensing issues
        var orderRepository = _serviceProvider.GetRequiredService<IOrderRepository>();
        var unitOfWork = _serviceProvider.GetRequiredService<IUnitOfWork>();
        var eventPublisher = _serviceProvider.GetRequiredService<IDomainEventPublisher>();

        _createHandler = new CreateOrderHandler(orderRepository, unitOfWork, eventPublisher);
        _confirmHandler = new ConfirmOrderHandler(orderRepository, unitOfWork, eventPublisher);
        _getHandler = new GetOrderHandler(orderRepository);

        // Setup test data
        _orderItems = new List<CreateOrderItemDto>
        {
            new() { ProductId = Guid.NewGuid(), Quantity = 2, UnitPrice = 29.99m, Currency = "USD" },
            new() { ProductId = Guid.NewGuid(), Quantity = 1, UnitPrice = 49.99m, Currency = "USD" },
            new() { ProductId = Guid.NewGuid(), Quantity = 3, UnitPrice = 15.99m, Currency = "USD" }
        };

        // Create an existing order for retrieval and confirmation benchmarks
        var createCommand = new CreateOrderCommand
        {
            CustomerId = _customerId,
            Items = _orderItems
        };
        var createdOrder = await _createHandler.Handle(createCommand, CancellationToken.None);
        _existingOrderId = createdOrder.Id;
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _dbContext?.Dispose();
        _serviceProvider?.Dispose();
    }

    /// <summary>
    /// Benchmark for creating a new order with multiple items.
    /// Tests: Value object creation, domain aggregate creation, persistence, domain events.
    /// </summary>
    [Benchmark]
    public async Task<OrderResponse> CreateOrderAsync()
    {
        var command = new CreateOrderCommand
        {
            CustomerId = Guid.NewGuid(),
            Items = _orderItems
        };

        return await _createHandler.Handle(command, CancellationToken.None);
    }

    /// <summary>
    /// Benchmark for confirming an existing order.
    /// Tests: Order retrieval, state transition, business rule validation, persistence.
    /// </summary>
    [Benchmark]
    public async Task<OrderResponse?> ConfirmOrderAsync()
    {
        var command = new ConfirmOrderCommand(_existingOrderId);
        return await _confirmHandler.Handle(command, CancellationToken.None);
    }

    /// <summary>
    /// Benchmark for retrieving an order by ID.
    /// Tests: Repository query, entity mapping, DTO conversion.
    /// </summary>
    [Benchmark]
    public async Task<OrderResponse?> GetOrderAsync()
    {
        var query = new GetOrderQuery(_existingOrderId);
        return await _getHandler.Handle(query, CancellationToken.None);
    }

    /// <summary>
    /// Benchmark for creating orders in batch.
    /// Tests: Bulk operations performance.
    /// </summary>
    [Benchmark]
    [Arguments(10)]
    [Arguments(50)]
    [Arguments(100)]
    public async Task CreateOrdersBatchAsync(int batchSize)
    {
        var tasks = new List<Task<OrderResponse>>(batchSize);
        
        for (int i = 0; i < batchSize; i++)
        {
            var command = new CreateOrderCommand
            {
                CustomerId = Guid.NewGuid(),
                Items = _orderItems
            };
            tasks.Add(_createHandler.Handle(command, CancellationToken.None));
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Benchmark for domain aggregate creation without persistence.
    /// Tests: Pure domain logic performance.
    /// </summary>
    [Benchmark]
    public Order CreateDomainOrderOnly()
    {
        var customerId = new CustomerId(Guid.NewGuid());
        var orderItems = _orderItems.Select(item =>
            OrderItem.Create(
                new ProductId(item.ProductId),
                item.Quantity,
                new Money(item.UnitPrice, item.Currency)
            )
        ).ToList();

        return Order.Create(customerId, orderItems);
    }

    /// <summary>
    /// Benchmark for value object creation.
    /// Tests: Money value object instantiation and validation.
    /// </summary>
    [Benchmark]
    public Money CreateMoneyValueObject()
    {
        return new Money(99.99m, "USD");
    }

    /// <summary>
    /// Benchmark for DTO mapping.
    /// Tests: Manual mapping performance from domain to DTO.
    /// </summary>
    [Benchmark]
    public OrderResponse MapOrderToResponse()
    {
        var order = CreateDomainOrderOnly();
        return OrderResponse.FromDomain(order);
    }
}

/// <summary>
/// No-op domain event publisher for benchmarking.
/// Avoids overhead of actual event publishing during performance tests.
/// </summary>
public class NoOpDomainEventPublisher : IDomainEventPublisher
{
    public Task PublishDomainEventsAsync(AggregateRoot aggregate, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task PublishDomainEventsAsync(IEnumerable<AggregateRoot> aggregates, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}