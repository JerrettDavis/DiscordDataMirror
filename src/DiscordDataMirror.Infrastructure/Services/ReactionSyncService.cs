using DiscordDataMirror.Application.Services;
using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.Repositories;
using DiscordDataMirror.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DiscordDataMirror.Infrastructure.Services;

/// <summary>
/// Implementation of reaction sync service using repositories.
/// </summary>
public class ReactionSyncService : IReactionSyncService
{
    private readonly IReactionRepository _reactionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReactionSyncService> _logger;

    public ReactionSyncService(
        IReactionRepository reactionRepository,
        IUnitOfWork unitOfWork,
        ILogger<ReactionSyncService> logger)
    {
        _reactionRepository = reactionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task AddReactionAsync(
        Snowflake messageId,
        string emoteKey,
        Snowflake userId,
        CancellationToken ct = default)
    {
        var existingReaction = await _reactionRepository.GetByMessageAndEmoteAsync(messageId, emoteKey, ct);

        if (existingReaction is null)
        {
            _logger.LogDebug("Creating new reaction: {EmoteKey} on {MessageId}", emoteKey, messageId);
            
            var reaction = new Reaction(messageId, emoteKey, 1);
            reaction.AddUser(userId.ToString());
            
            await _reactionRepository.AddAsync(reaction, ct);
        }
        else
        {
            _logger.LogDebug("Adding user to reaction: {EmoteKey} on {MessageId}", emoteKey, messageId);
            
            existingReaction.AddUser(userId.ToString());
            await _reactionRepository.UpdateAsync(existingReaction, ct);
        }
        
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task RemoveReactionAsync(
        Snowflake messageId,
        string emoteKey,
        Snowflake userId,
        CancellationToken ct = default)
    {
        var reaction = await _reactionRepository.GetByMessageAndEmoteAsync(messageId, emoteKey, ct);

        if (reaction is not null)
        {
            _logger.LogDebug("Removing user from reaction: {EmoteKey} on {MessageId}", emoteKey, messageId);
            
            reaction.RemoveUser(userId.ToString());
            
            if (reaction.Count == 0)
            {
                await _reactionRepository.DeleteAsync(reaction, ct);
            }
            else
            {
                await _reactionRepository.UpdateAsync(reaction, ct);
            }
            
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }

    public async Task SyncReactionsAsync(
        Snowflake messageId,
        IEnumerable<ReactionData> reactions,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Syncing reactions for message {MessageId}", messageId);
        
        // Get existing reactions for this message
        var existingReactions = await _reactionRepository.GetByMessageIdAsync(messageId, ct);
        var existingDict = existingReactions.ToDictionary(r => r.EmoteKey);
        var incomingKeys = new HashSet<string>();
        
        foreach (var reactionData in reactions)
        {
            ct.ThrowIfCancellationRequested();
            incomingKeys.Add(reactionData.EmoteKey);
            
            if (existingDict.TryGetValue(reactionData.EmoteKey, out var existing))
            {
                // Update existing
                existing.Update(reactionData.Count, reactionData.UserIds.ToList());
                await _reactionRepository.UpdateAsync(existing, ct);
            }
            else
            {
                // Create new
                var reaction = new Reaction(messageId, reactionData.EmoteKey, reactionData.Count);
                reaction.Update(reactionData.Count, reactionData.UserIds.ToList());
                await _reactionRepository.AddAsync(reaction, ct);
            }
        }
        
        // Remove reactions that no longer exist
        foreach (var existing in existingReactions)
        {
            if (!incomingKeys.Contains(existing.EmoteKey))
            {
                await _reactionRepository.DeleteAsync(existing, ct);
            }
        }
        
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
