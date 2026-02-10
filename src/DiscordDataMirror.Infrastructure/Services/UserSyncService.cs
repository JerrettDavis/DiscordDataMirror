using DiscordDataMirror.Application.Services;
using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.Repositories;
using DiscordDataMirror.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DiscordDataMirror.Infrastructure.Services;

/// <summary>
/// Implementation of user sync service using repositories.
/// </summary>
public class UserSyncService : IUserSyncService
{
    private readonly IUserRepository _userRepository;
    private readonly IGuildMemberRepository _guildMemberRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserSyncService> _logger;

    public UserSyncService(
        IUserRepository userRepository,
        IGuildMemberRepository guildMemberRepository,
        IUnitOfWork unitOfWork,
        ILogger<UserSyncService> logger)
    {
        _userRepository = userRepository;
        _guildMemberRepository = guildMemberRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<User> SyncUserAsync(
        Snowflake userId,
        string username,
        string? discriminator,
        string? globalName,
        string? avatarUrl,
        bool isBot,
        DateTime createdAt,
        string? rawJson = null,
        CancellationToken ct = default)
    {
        var existingUser = await _userRepository.GetByIdAsync(userId, ct);

        if (existingUser is null)
        {
            _logger.LogDebug("Creating new user: {Username} ({UserId})", username, userId);
            
            var user = new User(userId, username, createdAt);
            user.Update(username, discriminator, globalName, avatarUrl, isBot, rawJson);
            user.MarkSeen();
            
            await _userRepository.AddAsync(user, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            
            return user;
        }
        else
        {
            _logger.LogDebug("Updating existing user: {Username} ({UserId})", username, userId);
            
            existingUser.Update(username, discriminator, globalName, avatarUrl, isBot, rawJson);
            existingUser.MarkSeen();
            
            await _userRepository.UpdateAsync(existingUser, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            
            return existingUser;
        }
    }

    public async Task<GuildMember> SyncGuildMemberAsync(
        Snowflake userId,
        Snowflake guildId,
        string? nickname,
        DateTime? joinedAt,
        bool isPending,
        IEnumerable<string> roleIds,
        string? rawJson = null,
        CancellationToken ct = default)
    {
        var existingMember = await _guildMemberRepository.GetByUserAndGuildAsync(userId, guildId, ct);

        if (existingMember is null)
        {
            _logger.LogDebug("Creating new guild member: User {UserId} in Guild {GuildId}", userId, guildId);
            
            var member = new GuildMember(userId, guildId);
            member.Update(nickname, joinedAt, isPending, roleIds.ToList(), rawJson);
            member.MarkSynced();
            
            await _guildMemberRepository.AddAsync(member, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            
            return member;
        }
        else
        {
            _logger.LogDebug("Updating existing guild member: User {UserId} in Guild {GuildId}", userId, guildId);
            
            existingMember.Update(nickname, joinedAt, isPending, roleIds.ToList(), rawJson);
            existingMember.MarkSynced();
            
            await _guildMemberRepository.UpdateAsync(existingMember, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            
            return existingMember;
        }
    }

    public async Task RemoveGuildMemberAsync(Snowflake userId, Snowflake guildId, CancellationToken ct = default)
    {
        var member = await _guildMemberRepository.GetByUserAndGuildAsync(userId, guildId, ct);
        if (member is not null)
        {
            _logger.LogInformation("Removing guild member: User {UserId} from Guild {GuildId}", userId, guildId);
            await _guildMemberRepository.DeleteAsync(member, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }

    public async Task<IReadOnlyList<GuildMember>> GetGuildMembersAsync(Snowflake guildId, CancellationToken ct = default)
    {
        return await _guildMemberRepository.GetByGuildIdAsync(guildId, ct);
    }

    public async Task SyncGuildMemberBatchAsync(Snowflake guildId, IEnumerable<GuildMemberData> members, CancellationToken ct = default)
    {
        _logger.LogInformation("Batch syncing members for guild {GuildId}", guildId);
        
        foreach (var memberData in members)
        {
            ct.ThrowIfCancellationRequested();
            
            // First ensure user exists
            await SyncUserAsync(
                memberData.UserId,
                memberData.Username,
                memberData.Discriminator,
                memberData.GlobalName,
                memberData.AvatarUrl,
                memberData.IsBot,
                memberData.UserCreatedAt,
                null,
                ct);
            
            // Then sync guild membership
            await SyncGuildMemberAsync(
                memberData.UserId,
                guildId,
                memberData.Nickname,
                memberData.JoinedAt,
                memberData.IsPending,
                memberData.RoleIds,
                memberData.RawJson,
                ct);
        }
        
        _logger.LogInformation("Batch sync complete for guild {GuildId}", guildId);
    }
}
