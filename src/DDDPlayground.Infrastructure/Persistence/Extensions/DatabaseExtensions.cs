using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DDDPlayground.Infrastructure.Persistence.Extensions;

/// <summary>
/// Extension methods for database initialization and migration.
/// Designed for containerized development environments.
/// </summary>
/// <remarks>
/// For production environments, consider using proper migration deployment strategies:
/// - Kubernetes init containers
/// - Database migration jobs in CI/CD pipelines
/// - Manual migration scripts executed during deployment
/// - Tools like Flyway or Liquibase for enterprise scenarios
/// </remarks>
public static class DatabaseExtensions
{
    /// <summary>
    /// Applies pending database migrations at application startup.
    /// Recommended for Development environment only. For Production, use dedicated migration strategies.
    /// Includes retry logic to handle cases where the database container might not be ready yet.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type to migrate.</typeparam>
    /// <param name="app">The application host.</param>
    /// <param name="maxRetries">Maximum number of retry attempts. Default is 10.</param>
    /// <param name="delaySeconds">Delay between retries in seconds. Default is 3.</param>
    /// <returns>The application host for method chaining.</returns>
    public static async Task<IHost> MigrateDatabaseAsync<TContext>(
        this IHost app, 
        int maxRetries = 10,
        int delaySeconds = 3) where TContext : DbContext
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<TContext>>();

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                logger.LogInformation(
                    "Starting database migration for {Context} (Attempt {Attempt}/{MaxRetries})...", 
                    typeof(TContext).Name, 
                    attempt, 
                    maxRetries);

                var context = services.GetRequiredService<TContext>();
                
                // Check if database can be connected
                if (await context.Database.CanConnectAsync())
                {
                    // Apply any pending migrations
                    await context.Database.MigrateAsync();
                    
                    logger.LogInformation(
                        "Database migration completed successfully for {Context}", 
                        typeof(TContext).Name);
                    
                    return app;
                }
                
                logger.LogWarning(
                    "Cannot connect to database yet. Retrying in {Delay} seconds...", 
                    delaySeconds);
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                logger.LogWarning(
                    ex, 
                    "Attempt {Attempt} failed while migrating {Context}. Retrying in {Delay} seconds...",
                    attempt,
                    typeof(TContext).Name,
                    delaySeconds);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex, 
                    "Failed to migrate database for {Context} after {MaxRetries} attempts",
                    typeof(TContext).Name,
                    maxRetries);
                throw;
            }

            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
        }

        return app;
    }
}
