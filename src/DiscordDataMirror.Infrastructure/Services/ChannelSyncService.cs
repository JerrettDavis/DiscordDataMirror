using DiscordDataMirror.Application.Services;
using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.Repositories;
using DiscordDataMirror.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DiscordDataMirror.Infrastructure.Services;

/// <summary>
/// Implementation of channel sync service using repositories.
/// </summary>
public class ChannelSyncService : IChannelSyncService
{
    private readonly IChannelRepository _channelRepository;
    private readonly IThreadRepository _threadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ChannelSyncService> _logger;

    public ChannelSyncService(
        IChannelRepository channelRepository,
        IThreadRepository threadRepository,
        IUnitOfWork unitOfWork,
        ILogger<ChannelSyncService> logger)
    {
        _channelRepository = channelRepository;
        _threadRepository = threadRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Channel> SyncChannelAsync(
        Snowflake channelId,
        Snowflake guildId,
        string name,
        ChannelType type,
        string? topic,
        int position,
        bool isNsfw,
        Snowflake? parentId,
        DateTime createdAt,
        string? rawJson = null,
        CancellationToken ct = default)
    {
        var existingChannel = await _channelRepository.GetByIdAsync(channelId, ct);

        if (existingChannel is null)
        {
            _logger.LogDebug("Creating new channel: {ChannelName} ({ChannelId})", name, channelId);

            var channel = new Channel(channelId, guildId, name, type, createdAt);
            channel.Update(name, type, topic, position, isNsfw, parentId, rawJson);
            channel.MarkSynced();

            await _channelRepository.AddAsync(channel, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return channel;
        }
        else
        {
            _logger.LogDebug("Updating existing channel: {ChannelName} ({ChannelId})", name, channelId);

            existingChannel.Update(name, type, topic, position, isNsfw, parentId, rawJson);
            existingChannel.MarkSynced();

            await _channelRepository.UpdateAsync(existingChannel, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return existingChannel;
        }
    }

    public async Task<Domain.Entities.Thread> SyncThreadAsync(
        Snowflake threadId,
        Snowflake parentChannelId,
        Snowflake? ownerId,
        int messageCount,
        int memberCount,
        bool isArchived,
        bool isLocked,
        DateTime? archiveTimestamp,
        int? autoArchiveDuration,
        CancellationToken ct = default)
    {
        var existingThread = await _threadRepository.GetByIdAsync(threadId, ct);

        if (existingThread is null)
        {
            _logger.LogDebug("Creating new thread: {ThreadId}", threadId);

            var thread = new Domain.Entities.Thread(threadId, parentChannelId);
            thread.Update(ownerId, messageCount, memberCount, isArchived, isLocked, archiveTimestamp, autoArchiveDuration);

            await _threadRepository.AddAsync(thread, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return thread;
        }
        else
        {
            _logger.LogDebug("Updating existing thread: {ThreadId}", threadId);

            existingThread.Update(
                ownerId,
                messageCount,
                memberCount,
                isArchived,
                isLocked,
                archiveTimestamp,
                autoArchiveDuration);

            await _threadRepository.UpdateAsync(existingThread, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return existingThread;
        }
    }

    public async Task DeleteChannelAsync(Snowflake channelId, CancellationToken ct = default)
    {
        var channel = await _channelRepository.GetByIdAsync(channelId, ct);
        if (channel is not null)
        {
            _logger.LogInformation("Deleting channel: {ChannelId}", channelId);
            await _channelRepository.DeleteAsync(channel, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }

    public async Task<IReadOnlyList<Channel>> GetGuildChannelsAsync(Snowflake guildId, CancellationToken ct = default)
    {
        return await _channelRepository.GetByGuildIdAsync(guildId, ct);
    }
}
