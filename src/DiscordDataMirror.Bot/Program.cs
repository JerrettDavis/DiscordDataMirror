using DiscordDataMirror.Application;
using DiscordDataMirror.Application.Configuration;
using DiscordDataMirror.Application.Services;
using DiscordDataMirror.Bot.Services;
using DiscordDataMirror.Infrastructure;
using DiscordDataMirror.Infrastructure.Persistence;
using DiscordDataMirror.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

// Add Aspire service defaults
builder.AddServiceDefaults();

// Add PostgreSQL DbContext via Aspire
builder.AddNpgsqlDbContext<DiscordMirrorDbContext>("discorddatamirror");

// Configuration options
builder.Services.Configure<DiscordOptions>(builder.Configuration.GetSection(DiscordOptions.SectionName));
builder.Services.Configure<SyncOptions>(builder.Configuration.GetSection(SyncOptions.SectionName));
builder.Services.Configure<AttachmentOptions>(builder.Configuration.GetSection(AttachmentOptions.SectionName));

// Add application and infrastructure services
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

// Add HttpClient for publishing events to Dashboard
builder.Services.AddHttpClient<ISyncEventPublisher, HttpSyncEventPublisher>(client =>
{
    // Use service discovery to find the dashboard service
    client.BaseAddress = new Uri("https+http://dashboard");
});

// Add Discord services
builder.Services.AddSingleton<DiscordClientService>();
builder.Services.AddSingleton<DiscordEventHandler>();

// Expose the DiscordSocketClient as a singleton so the orchestrator can use it
builder.Services.AddSingleton(sp => sp.GetRequiredService<DiscordClientService>().Client);

// Register the historical sync orchestrator (requires DiscordSocketClient, so only in Bot)
builder.Services.AddScoped<HistoricalSyncOrchestrator>();

// Add the bot worker service
builder.Services.AddHostedService<DiscordBotWorker>();

// Add attachment download worker
builder.Services.AddHostedService<AttachmentDownloadWorker>();

// Add SignalR command listener for Dashboard → Bot sync commands
builder.Services.AddHostedService<SyncCommandListener>();

var host = builder.Build();

// Apply pending migrations on startup
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DiscordMirrorDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        logger.LogInformation("Applying database migrations...");
        await db.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to apply database migrations");
        throw;
    }
}

host.Run();

/// <summary>
/// Discord bot worker service that manages the Discord client lifecycle.
/// </summary>
public class DiscordBotWorker : BackgroundService
{
    private readonly ILogger<DiscordBotWorker> _logger;
    private readonly DiscordClientService _clientService;
    private readonly DiscordEventHandler _eventHandler;
    private readonly IServiceProvider _serviceProvider;
    private readonly DiscordOptions _discordOptions;
    private bool _historicalSyncComplete;
    
    public DiscordBotWorker(
        ILogger<DiscordBotWorker> logger,
        DiscordClientService clientService,
        DiscordEventHandler eventHandler,
        IServiceProvider serviceProvider,
        IOptions<DiscordOptions> discordOptions)
    {
        _logger = logger;
        _clientService = clientService;
        _eventHandler = eventHandler;
        _serviceProvider = serviceProvider;
        _discordOptions = discordOptions.Value;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Discord bot worker starting...");
        
        try
        {
            // Register event handlers before connecting
            _eventHandler.RegisterEventHandlers();
            
            // Register Ready event to trigger historical sync
            _clientService.Client.Ready += OnClientReadyAsync;
            
            // Start the Discord client
            await _clientService.StartAsync(stoppingToken);
            
            _logger.LogInformation("Discord bot started successfully");
            
            // Keep the service running
            while (!stoppingToken.IsCancellationRequested)
            {
                // Periodic health check / maintenance
                if (_clientService.IsConnected)
                {
                    _logger.LogDebug("Discord bot health check: Connected");
                }
                else
                {
                    _logger.LogWarning("Discord bot health check: Disconnected - client will auto-reconnect");
                }
                
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when stopping
            _logger.LogInformation("Discord bot worker stopping...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in Discord bot worker");
            throw;
        }
        finally
        {
            await _clientService.StopAsync();
            _logger.LogInformation("Discord bot worker stopped");
        }
    }
    
    private async Task OnClientReadyAsync()
    {
        if (!_discordOptions.SyncOnStartup || _historicalSyncComplete)
            return;
        
        _historicalSyncComplete = true; // Prevent running twice on reconnect
        
        _logger.LogInformation("Discord client ready - starting historical sync...");
        
        // Run historical sync in background to not block the Ready event
        _ = Task.Run(async () =>
        {
            try
            {
                // Wait for guilds to be available (Discord.NET loads them after Ready)
                var client = _clientService.Client;
                var maxWait = TimeSpan.FromSeconds(30);
                var waited = TimeSpan.Zero;
                while (client.Guilds.Count == 0 && waited < maxWait)
                {
                    _logger.LogDebug("Waiting for guilds to load... ({Elapsed}s)", waited.TotalSeconds);
                    await Task.Delay(1000);
                    waited += TimeSpan.FromSeconds(1);
                }
                
                _logger.LogInformation("Starting historical sync with {GuildCount} guilds", client.Guilds.Count);
                
                // Create scope inside the task so it lives for the duration of the sync
                using var scope = _serviceProvider.CreateScope();
                var orchestrator = scope.ServiceProvider.GetRequiredService<HistoricalSyncOrchestrator>();
                await orchestrator.SyncAllGuildsAsync();
                
                _logger.LogInformation("Historical sync completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Historical sync failed");
            }
        });
        
        await Task.CompletedTask;
    }
    
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Discord bot worker received stop signal");
        await base.StopAsync(cancellationToken);
    }
}
