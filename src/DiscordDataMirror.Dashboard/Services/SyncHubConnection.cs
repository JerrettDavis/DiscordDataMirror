using DiscordDataMirror.Application.Events;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace DiscordDataMirror.Dashboard.Services;

/// <summary>
/// Manages the SignalR hub connection for Blazor components.
/// Provides connection state management and event subscriptions.
/// </summary>
public sealed class SyncHubConnection : IAsyncDisposable
{
    private readonly NavigationManager _navigation;
    private readonly ILogger<SyncHubConnection> _logger;
    private HubConnection? _hubConnection;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    public event Func<GuildSyncedEvent, Task>? OnGuildSynced;
    public event Func<ChannelSyncedEvent, Task>? OnChannelSynced;
    public event Func<MessageReceivedEvent, Task>? OnMessageReceived;
    public event Func<MessageUpdatedEvent, Task>? OnMessageUpdated;
    public event Func<MessageDeletedEvent, Task>? OnMessageDeleted;
    public event Func<SyncProgressEvent, Task>? OnSyncProgress;
    public event Func<SyncErrorEvent, Task>? OnSyncError;
    public event Func<MemberUpdatedEvent, Task>? OnMemberUpdated;
    public event Func<AttachmentDownloadedEvent, Task>? OnAttachmentDownloaded;
    public event Func<HubConnectionState, Task>? OnConnectionStateChanged;

    public HubConnectionState State => _hubConnection?.State ?? HubConnectionState.Disconnected;
    public bool IsConnected => State == HubConnectionState.Connected;

    public SyncHubConnection(NavigationManager navigation, ILogger<SyncHubConnection> logger)
    {
        _navigation = navigation;
        _logger = logger;
    }

    /// <summary>
    /// Starts the SignalR connection if not already connected.
    /// </summary>
    public async Task StartAsync()
    {
        await _connectionLock.WaitAsync();
        try
        {
            if (_hubConnection is { State: HubConnectionState.Connected })
                return;

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_navigation.ToAbsoluteUri("/hubs/sync"))
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
                .Build();

            RegisterHandlers();
            RegisterConnectionEvents();

            _logger.LogInformation("Starting SignalR connection...");
            await _hubConnection.StartAsync();
            _logger.LogInformation("SignalR connection established");

            await NotifyConnectionStateChanged();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start SignalR connection");
            throw;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private void RegisterHandlers()
    {
        if (_hubConnection == null) return;

        _hubConnection.On<GuildSyncedEvent>("GuildSynced", async evt =>
        {
            if (OnGuildSynced != null) await OnGuildSynced.Invoke(evt);
        });

        _hubConnection.On<ChannelSyncedEvent>("ChannelSynced", async evt =>
        {
            if (OnChannelSynced != null) await OnChannelSynced.Invoke(evt);
        });

        _hubConnection.On<MessageReceivedEvent>("MessageReceived", async evt =>
        {
            if (OnMessageReceived != null) await OnMessageReceived.Invoke(evt);
        });

        _hubConnection.On<MessageUpdatedEvent>("MessageUpdated", async evt =>
        {
            if (OnMessageUpdated != null) await OnMessageUpdated.Invoke(evt);
        });

        _hubConnection.On<MessageDeletedEvent>("MessageDeleted", async evt =>
        {
            if (OnMessageDeleted != null) await OnMessageDeleted.Invoke(evt);
        });

        _hubConnection.On<SyncProgressEvent>("SyncProgress", async evt =>
        {
            if (OnSyncProgress != null) await OnSyncProgress.Invoke(evt);
        });

        _hubConnection.On<SyncErrorEvent>("SyncError", async evt =>
        {
            if (OnSyncError != null) await OnSyncError.Invoke(evt);
        });

        _hubConnection.On<MemberUpdatedEvent>("MemberUpdated", async evt =>
        {
            if (OnMemberUpdated != null) await OnMemberUpdated.Invoke(evt);
        });

        _hubConnection.On<AttachmentDownloadedEvent>("AttachmentDownloaded", async evt =>
        {
            if (OnAttachmentDownloaded != null) await OnAttachmentDownloaded.Invoke(evt);
        });
    }

    private void RegisterConnectionEvents()
    {
        if (_hubConnection == null) return;

        _hubConnection.Reconnecting += async _ =>
        {
            _logger.LogWarning("SignalR connection lost. Reconnecting...");
            await NotifyConnectionStateChanged();
        };

        _hubConnection.Reconnected += async _ =>
        {
            _logger.LogInformation("SignalR connection reestablished");
            await NotifyConnectionStateChanged();
        };

        _hubConnection.Closed += async _ =>
        {
            _logger.LogWarning("SignalR connection closed");
            await NotifyConnectionStateChanged();
        };
    }

    private async Task NotifyConnectionStateChanged()
    {
        if (OnConnectionStateChanged != null)
            await OnConnectionStateChanged.Invoke(State);
    }

    /// <summary>
    /// Subscribe to receive updates for a specific guild.
    /// </summary>
    public async Task SubscribeToGuildAsync(string guildId)
    {
        if (_hubConnection is { State: HubConnectionState.Connected })
        {
            await _hubConnection.InvokeAsync("SubscribeToGuild", guildId);
            _logger.LogDebug("Subscribed to guild {GuildId}", guildId);
        }
    }

    /// <summary>
    /// Unsubscribe from a specific guild's updates.
    /// </summary>
    public async Task UnsubscribeFromGuildAsync(string guildId)
    {
        if (_hubConnection is { State: HubConnectionState.Connected })
        {
            await _hubConnection.InvokeAsync("UnsubscribeFromGuild", guildId);
            _logger.LogDebug("Unsubscribed from guild {GuildId}", guildId);
        }
    }

    /// <summary>
    /// Subscribe to receive updates for a specific channel.
    /// </summary>
    public async Task SubscribeToChannelAsync(string channelId)
    {
        if (_hubConnection is { State: HubConnectionState.Connected })
        {
            await _hubConnection.InvokeAsync("SubscribeToChannel", channelId);
            _logger.LogDebug("Subscribed to channel {ChannelId}", channelId);
        }
    }

    /// <summary>
    /// Unsubscribe from a specific channel's updates.
    /// </summary>
    public async Task UnsubscribeFromChannelAsync(string channelId)
    {
        if (_hubConnection is { State: HubConnectionState.Connected })
        {
            await _hubConnection.InvokeAsync("UnsubscribeFromChannel", channelId);
            _logger.LogDebug("Unsubscribed from channel {ChannelId}", channelId);
        }
    }

    /// <summary>
    /// Subscribe to sync status updates.
    /// </summary>
    public async Task SubscribeToSyncStatusAsync()
    {
        if (_hubConnection is { State: HubConnectionState.Connected })
        {
            await _hubConnection.InvokeAsync("SubscribeToSyncStatus");
            _logger.LogDebug("Subscribed to sync status");
        }
    }

    /// <summary>
    /// Unsubscribe from sync status updates.
    /// </summary>
    public async Task UnsubscribeFromSyncStatusAsync()
    {
        if (_hubConnection is { State: HubConnectionState.Connected })
        {
            await _hubConnection.InvokeAsync("UnsubscribeFromSyncStatus");
            _logger.LogDebug("Unsubscribed from sync status");
        }
    }

    /// <summary>
    /// Request a guild sync from the Dashboard to the Bot.
    /// </summary>
    public async Task RequestGuildSyncAsync(string guildId, bool backfillMessages = true, int? messageLimit = null)
    {
        if (_hubConnection is { State: HubConnectionState.Connected })
        {
            await _hubConnection.InvokeAsync("RequestGuildSync", guildId, backfillMessages, messageLimit);
            _logger.LogInformation("Requested sync for guild {GuildId}", guildId);
        }
        else
        {
            throw new InvalidOperationException("Not connected to SignalR hub");
        }
    }

    /// <summary>
    /// Request a channel sync from the Dashboard to the Bot.
    /// </summary>
    public async Task RequestChannelSyncAsync(string guildId, string channelId, bool backfillMessages = true, int? messageLimit = null)
    {
        if (_hubConnection is { State: HubConnectionState.Connected })
        {
            await _hubConnection.InvokeAsync("RequestChannelSync", guildId, channelId, backfillMessages, messageLimit);
            _logger.LogInformation("Requested sync for channel {ChannelId} in guild {GuildId}", channelId, guildId);
        }
        else
        {
            throw new InvalidOperationException("Not connected to SignalR hub");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
        _connectionLock.Dispose();
    }
}
