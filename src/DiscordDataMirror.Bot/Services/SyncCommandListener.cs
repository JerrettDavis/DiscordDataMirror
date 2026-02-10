using Discord.WebSocket;
using DiscordDataMirror.Application.Services;
using DiscordDataMirror.Infrastructure.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscordDataMirror.Bot.Services;

/// <summary>
/// Listens for sync commands from the Dashboard via SignalR.
/// </summary>
public class SyncCommandListener : BackgroundService
{
    private readonly ILogger<SyncCommandListener> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly DiscordClientService _discordClient;
    private readonly string _dashboardUrl;
    private HubConnection? _hubConnection;

    public SyncCommandListener(
        ILogger<SyncCommandListener> logger,
        IServiceProvider serviceProvider,
        DiscordClientService discordClient,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _discordClient = discordClient;
        
        // Get dashboard URL from service discovery or configuration
        var dashboardEndpoint = configuration["services:dashboard:https:0"] 
            ?? configuration["services:dashboard:http:0"]
            ?? "https://localhost:7152";
        _dashboardUrl = dashboardEndpoint;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait a bit for services to start
        await Task.Delay(5000, stoppingToken);
        
        try
        {
            await ConnectToHubAsync(stoppingToken);
            
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_hubConnection?.State != HubConnectionState.Connected)
                {
                    _logger.LogWarning("SignalR connection lost, attempting reconnect...");
                    await ConnectToHubAsync(stoppingToken);
                }
                
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in SyncCommandListener");
        }
    }

    private async Task ConnectToHubAsync(CancellationToken stoppingToken)
    {
        try
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{_dashboardUrl}/hubs/sync", options =>
                {
                    // Accept self-signed certs in development
                    options.HttpMessageHandlerFactory = handler =>
                    {
                        if (handler is HttpClientHandler clientHandler)
                        {
                            clientHandler.ServerCertificateCustomValidationCallback = 
                                (message, cert, chain, errors) => true;
                        }
                        return handler;
                    };
                })
                .WithAutomaticReconnect()
                .Build();

            // Register handlers for sync commands
            _hubConnection.On<SyncGuildCommand>("TriggerGuildSync", async command =>
            {
                await HandleGuildSyncCommand(command);
            });

            _hubConnection.On<SyncChannelCommand>("TriggerChannelSync", async command =>
            {
                await HandleChannelSyncCommand(command);
            });

            await _hubConnection.StartAsync(stoppingToken);
            
            // Subscribe to bot commands group
            await _hubConnection.InvokeAsync("SubscribeToCommands", stoppingToken);
            
            _logger.LogInformation("Connected to Dashboard SignalR hub at {Url}", _dashboardUrl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to connect to Dashboard SignalR hub at {Url}", _dashboardUrl);
        }
    }

    private async Task HandleGuildSyncCommand(SyncGuildCommand command)
    {
        _logger.LogInformation("Received sync command for guild {GuildId}", command.GuildId);
        
        try
        {
            if (!ulong.TryParse(command.GuildId, out var guildId))
            {
                _logger.LogError("Invalid guild ID: {GuildId}", command.GuildId);
                return;
            }

            var guild = _discordClient.Client.GetGuild(guildId);
            if (guild == null)
            {
                _logger.LogError("Guild not found: {GuildId}", command.GuildId);
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<HistoricalSyncOrchestrator>();
            
            await orchestrator.SyncGuildAsync(guild);
            
            _logger.LogInformation("Completed sync for guild {GuildName} ({GuildId})", 
                guild.Name, command.GuildId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling sync command for guild {GuildId}", command.GuildId);
        }
    }

    private async Task HandleChannelSyncCommand(SyncChannelCommand command)
    {
        _logger.LogInformation("Received sync command for channel {ChannelId} in guild {GuildId}", 
            command.ChannelId, command.GuildId);
        
        // For now, channel sync triggers a full guild sync
        // TODO: Add individual channel sync support
        await HandleGuildSyncCommand(new SyncGuildCommand(command.GuildId, command.BackfillMessages, command.MessageLimit));
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }
        await base.StopAsync(cancellationToken);
    }
}

// Mirror the command records from the Hub
public record SyncGuildCommand(
    string GuildId,
    bool BackfillMessages = true,
    int? MessageLimit = null);

public record SyncChannelCommand(
    string GuildId,
    string ChannelId,
    bool BackfillMessages = true,
    int? MessageLimit = null);
