using Discord;
using Discord.WebSocket;
using DiscordDataMirror.Application.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscordDataMirror.Bot.Services;

/// <summary>
/// Manages the Discord socket client lifecycle.
/// </summary>
public class DiscordClientService : IAsyncDisposable
{
    private readonly DiscordSocketClient _client;
    private readonly DiscordOptions _options;
    private readonly ILogger<DiscordClientService> _logger;
    private bool _isConnected;

    public DiscordSocketClient Client => _client;
    public bool IsConnected => _isConnected;

    public DiscordClientService(
        IOptions<DiscordOptions> options,
        ILogger<DiscordClientService> logger)
    {
        _options = options.Value;
        _logger = logger;

        var config = new DiscordSocketConfig
        {
            GatewayIntents =
                GatewayIntents.Guilds |
                GatewayIntents.GuildMembers |
                GatewayIntents.GuildMessages |
                GatewayIntents.GuildMessageReactions |
                GatewayIntents.MessageContent |
                GatewayIntents.DirectMessages,
            MessageCacheSize = 100,
            LogLevel = LogSeverity.Info,
            AlwaysDownloadUsers = true
        };

        _client = new DiscordSocketClient(config);

        // Wire up internal logging
        _client.Log += OnLogAsync;
        _client.Connected += OnConnectedAsync;
        _client.Disconnected += OnDisconnectedAsync;
        _client.Ready += OnReadyAsync;
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_options.Token))
        {
            _logger.LogError("Discord token not configured. Set Discord:Token in user-secrets or environment.");
            return;
        }

        _logger.LogInformation("Starting Discord client...");

        await _client.LoginAsync(TokenType.Bot, _options.Token);
        await _client.StartAsync();
    }

    public async Task StopAsync()
    {
        _logger.LogInformation("Stopping Discord client...");
        await _client.StopAsync();
        await _client.LogoutAsync();
    }

    private Task OnLogAsync(LogMessage log)
    {
        var logLevel = log.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Debug,
            LogSeverity.Debug => LogLevel.Trace,
            _ => LogLevel.Information
        };

        if (log.Exception is not null)
            _logger.Log(logLevel, log.Exception, "[Discord] {Source}: {Message}", log.Source, log.Message);
        else
            _logger.Log(logLevel, "[Discord] {Source}: {Message}", log.Source, log.Message);

        return Task.CompletedTask;
    }

    private Task OnConnectedAsync()
    {
        _isConnected = true;
        _logger.LogInformation("Discord client connected");
        return Task.CompletedTask;
    }

    private Task OnDisconnectedAsync(Exception ex)
    {
        _isConnected = false;
        _logger.LogWarning(ex, "Discord client disconnected");
        return Task.CompletedTask;
    }

    private Task OnReadyAsync()
    {
        _logger.LogInformation("Discord client ready. Connected to {GuildCount} guilds", _client.Guilds.Count);
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
    }
}
