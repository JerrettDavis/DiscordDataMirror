using DiscordDataMirror.Application;
using DiscordDataMirror.Application.Services;
using DiscordDataMirror.Dashboard.Components;
using DiscordDataMirror.Dashboard.Services;
using DiscordDataMirror.Infrastructure;
using DiscordDataMirror.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MudBlazor.Services;
using System.Net;
using System.Net.Sockets;

namespace DiscordDataMirror.E2E.Tests.Infrastructure;

/// <summary>
/// Creates and manages a test web server for E2E testing with Playwright.
/// Uses SQLite file-based database instead of PostgreSQL.
/// </summary>
public class TestWebApplicationFactory : IAsyncDisposable
{
    private WebApplication? _app;
    private readonly int _port;
    private readonly string _dbPath;

    public string BaseUrl => $"http://localhost:{_port}";

    public TestWebApplicationFactory()
    {
        // Get a random available port
        using var socket = new TcpListener(IPAddress.Loopback, 0);
        socket.Start();
        _port = ((IPEndPoint)socket.LocalEndpoint).Port;
        socket.Stop();
        
        // Create a unique temp database file for this test run
        _dbPath = Path.Combine(Path.GetTempPath(), $"discord_mirror_test_{Guid.NewGuid():N}.db");
    }

    /// <summary>
    /// Starts the test web server.
    /// </summary>
    public async Task StartAsync()
    {
        // Find the Dashboard project path
        var dashboardAssembly = typeof(DiscordDataMirror.Dashboard.Program).Assembly;
        var dashboardPath = Path.GetDirectoryName(dashboardAssembly.Location)!;
        
        // Navigate up to find the project root (from bin/Debug/net10.0)
        var projectRoot = Path.GetFullPath(Path.Combine(dashboardPath, "..", "..", "..", "..", "..", "src", "DiscordDataMirror.Dashboard"));
        
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing",
            ContentRootPath = projectRoot,
            WebRootPath = Path.Combine(projectRoot, "wwwroot")
        });

        // Configure Kestrel to listen on our port
        builder.WebHost.UseUrls(BaseUrl);

        // Use connection string for file-based SQLite (handles concurrency better)
        var connectionString = $"Data Source={_dbPath};Cache=Shared";

        // SQLite DbContext (replaces Aspire's AddNpgsqlDbContext)
        builder.Services.AddDbContext<DiscordMirrorDbContext>(options =>
        {
            options.UseSqlite(connectionString);
        });

        // DbContext factory for Blazor components
        builder.Services.AddDbContextFactory<DiscordMirrorDbContext>(options =>
        {
            options.UseSqlite(connectionString);
        }, ServiceLifetime.Scoped);

        // Add application and infrastructure services
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure();

        // Add SignalR services
        builder.Services.AddSignalR();
        builder.Services.AddScoped<SyncHubConnection>();
        builder.Services.AddSingleton<ISyncEventPublisher, SignalRSyncEventPublisher>();

        // Add MudBlazor services
        builder.Services.AddMudServices();

        // Add Blazor services
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        // Health check endpoints (simplified, no OpenTelemetry)
        builder.Services.AddHealthChecks();

        _app = builder.Build();

        // Create database and seed test data
        using (var scope = _app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<DiscordMirrorDbContext>();
            context.Database.EnsureCreated();
            await TestDataSeeder.SeedAsync(context);
        }

        // Configure the HTTP request pipeline
        _app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        _app.UseAntiforgery();

        // Use static files instead of MapStaticAssets for testing
        // (MapStaticAssets requires manifest files not available in test context)
        _app.UseStaticFiles();
        
        _app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        // Map SignalR hub
        _app.MapHub<DiscordDataMirror.Dashboard.Hubs.SyncHub>("/hubs/sync");

        // Map health endpoints
        _app.MapHealthChecks("/health");

        // Start the server
        await _app.StartAsync();
    }

    public async Task StopAsync()
    {
        if (_app != null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
            _app = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        
        // Clean up the temp database file
        try
        {
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
