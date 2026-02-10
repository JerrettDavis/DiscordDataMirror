using DiscordDataMirror.Application.Services;
using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.Repositories;
using DiscordDataMirror.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DiscordDataMirror.Infrastructure.Services;

/// <summary>
/// Implementation of role sync service using repositories.
/// </summary>
public class RoleSyncService : IRoleSyncService
{
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RoleSyncService> _logger;

    public RoleSyncService(
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork,
        ILogger<RoleSyncService> logger)
    {
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Role> SyncRoleAsync(
        Snowflake roleId,
        Snowflake guildId,
        string name,
        int color,
        int position,
        string? permissions,
        bool isHoisted,
        bool isMentionable,
        bool isManaged,
        string? rawJson = null,
        CancellationToken ct = default)
    {
        var existingRole = await _roleRepository.GetByIdAsync(roleId, ct);

        if (existingRole is null)
        {
            _logger.LogDebug("Creating new role: {RoleName} ({RoleId})", name, roleId);
            
            var role = new Role(roleId, guildId, name);
            role.Update(name, color, position, permissions, isHoisted, isMentionable, isManaged, rawJson);
            
            await _roleRepository.AddAsync(role, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            
            return role;
        }
        else
        {
            _logger.LogDebug("Updating existing role: {RoleName} ({RoleId})", name, roleId);
            
            existingRole.Update(name, color, position, permissions, isHoisted, isMentionable, isManaged, rawJson);
            
            await _roleRepository.UpdateAsync(existingRole, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            
            return existingRole;
        }
    }

    public async Task DeleteRoleAsync(Snowflake roleId, CancellationToken ct = default)
    {
        var role = await _roleRepository.GetByIdAsync(roleId, ct);
        if (role is not null)
        {
            _logger.LogInformation("Deleting role: {RoleId}", roleId);
            await _roleRepository.DeleteAsync(role, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }

    public async Task<IReadOnlyList<Role>> GetGuildRolesAsync(Snowflake guildId, CancellationToken ct = default)
    {
        return await _roleRepository.GetByGuildIdAsync(guildId, ct);
    }
}
