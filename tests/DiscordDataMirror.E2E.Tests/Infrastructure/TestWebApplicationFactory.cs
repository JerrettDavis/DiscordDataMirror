using DiscordDataMirror.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace DiscordDataMirror.E2E.Tests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory that uses SQLite in-memory for E2E testing.
/// This avoids the need for Docker/PostgreSQL and full Aspire orchestration.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<DiscordDataMirror.Dashboard.Program>
{
    private SqliteConnection? _connection;

    public TestWebApplicationFactory()
    {
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Keep the connection open for the lifetime of the factory
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            // Remove ALL existing DbContext-related service registrations
            var typesToRemove = new[]
            {
                typeof(DbContextOptions<DiscordMirrorDbContext>),
                typeof(DbContextOptions),
                typeof(DiscordMirrorDbContext),
                typeof(IDbContextFactory<DiscordMirrorDbContext>)
            };

            foreach (var type in typesToRemove)
            {
                var descriptors = services.Where(d => d.ServiceType == type).ToList();
                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }
            }

            // Also remove any service that has DbContext in its implementation
            var dbContextDescriptors = services
                .Where(d => d.ImplementationType?.Name.Contains("DbContext") == true ||
                           d.ServiceType.Name.Contains("DbContext"))
                .ToList();
            foreach (var descriptor in dbContextDescriptors)
            {
                services.Remove(descriptor);
            }

            // Register SQLite DbContext
            services.AddDbContext<DiscordMirrorDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            // Register factory for Blazor components - use same connection
            services.AddSingleton<IDbContextFactory<DiscordMirrorDbContext>>(sp =>
            {
                var options = new DbContextOptionsBuilder<DiscordMirrorDbContext>()
                    .UseSqlite(_connection!)
                    .Options;
                return new TestDbContextFactory(options);
            });
        });

        // Configure the service provider after services are configured
        builder.ConfigureServices(services =>
        {
            // Build service provider to create database and seed data
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DiscordMirrorDbContext>();

            // Create database schema
            context.Database.EnsureCreated();

            // Seed test data
            TestDataSeeder.SeedAsync(context).GetAwaiter().GetResult();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection?.Dispose();
        }
    }
}

/// <summary>
/// Simple DbContext factory for testing that creates contexts with the same connection.
/// </summary>
internal class TestDbContextFactory : IDbContextFactory<DiscordMirrorDbContext>
{
    private readonly DbContextOptions<DiscordMirrorDbContext> _options;

    public TestDbContextFactory(DbContextOptions<DiscordMirrorDbContext> options)
    {
        _options = options;
    }

    public DiscordMirrorDbContext CreateDbContext()
    {
        return new DiscordMirrorDbContext(_options);
    }
}
