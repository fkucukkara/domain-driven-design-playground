using DDDPlayground.ApiService.Endpoints;
using DDDPlayground.Application;
using DDDPlayground.Infrastructure;
using DDDPlayground.Infrastructure.Persistence;
using DDDPlayground.Infrastructure.Persistence.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations
builder.AddServiceDefaults();

// Add Aspire PostgreSQL with EF Core
builder.AddNpgsqlDbContext<AppDbContext>("dddplaygrounddb");

// Add Application layer (MediatR, FluentValidation)
builder.Services.AddApplication();

// Add Infrastructure layer (Repositories, Unit of Work)
builder.Services.AddInfrastructure();

// Add services to the container
builder.Services.AddProblemDetails();

// Add OpenAPI/Swagger
builder.Services.AddOpenApi();

var app = builder.Build();

// Apply database migrations at startup in Development environment
// For production, use proper migration deployment strategies (e.g., init containers, deployment scripts)
if (app.Environment.IsDevelopment())
{
    await app.MigrateDatabaseAsync<AppDbContext>();
}

// Configure the HTTP request pipeline
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Map API endpoints
app.MapOrderEndpoints();

app.MapDefaultEndpoints();

app.Run();
