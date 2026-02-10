using DiscordDataMirror.Application.Services;
using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.Repositories;
using DiscordDataMirror.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DiscordDataMirror.Infrastructure.Services;

/// <summary>
/// Implementation of guild sync service using repositories.
/// </summary>
public class GuildSyncService : IGuildSyncService
{
    private readonly IGuildRepository _guildRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GuildSyncService> _logger;

    public GuildSyncService(
        IGuildRepository guildRepository,
        IUnitOfWork unitOfWork,
        ILogger<GuildSyncService> logger)
    {
        _guildRepository = guildRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Guild> SyncGuildAsync(
        Snowflake guildId,
        string name,
        string? iconUrl,
        string? description,
        Snowflake ownerId,
        DateTime createdAt,
        string? rawJson = null,
        CancellationToken ct = default)
    {
        var existingGuild = await _guildRepository.GetByIdAsync(guildId, ct);

        if (existingGuild is null)
        {
            _logger.LogInformation("Creating new guild: {GuildName} ({GuildId})", name, guildId);
            
            var guild = new Guild(guildId, name, ownerId, createdAt);
            guild.Update(name, iconUrl, description, ownerId, rawJson);
            guild.MarkSynced();
            
            await _guildRepository.AddAsync(guild, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            
            return guild;
        }
        else
        {
            _logger.LogDebug("Updating existing guild: {GuildName} ({GuildId})", name, guildId);
            
            existingGuild.Update(name, iconUrl, description, ownerId, rawJson);
            existingGuild.MarkSynced();
            
            await _guildRepository.UpdateAsync(existingGuild, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            
            return existingGuild;
        }
    }

    public async Task FullSyncGuildAsync(Snowflake guildId, CancellationToken ct = default)
    {
        // This will be called by the orchestrator
        // The actual work happens in the individual sync services
        var guild = await _guildRepository.GetByIdAsync(guildId, ct);
        if (guild is not null)
        {
            guild.MarkSynced();
            await _guildRepository.UpdateAsync(guild, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }

    public async Task<IReadOnlyList<Guild>> GetGuildsNeedingSyncAsync(TimeSpan syncInterval, CancellationToken ct = default)
    {
        return await _guildRepository.GetNeedingSyncAsync(syncInterval, ct);
    }
}
