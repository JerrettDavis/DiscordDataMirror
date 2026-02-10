using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.Repositories;
using DiscordDataMirror.Domain.ValueObjects;
using MediatR;

namespace DiscordDataMirror.Application.Commands;

public record UpsertRoleCommand(
    string Id,
    string GuildId,
    string Name,
    int Color,
    int Position,
    string? Permissions,
    bool IsHoisted,
    bool IsMentionable,
    bool IsManaged,
    string? RawJson = null
) : IRequest<Role>;

public class UpsertRoleCommandHandler : IRequestHandler<UpsertRoleCommand, Role>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;
    
    public UpsertRoleCommandHandler(IRoleRepository roleRepository, IUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
    }
    
    public async Task<Role> Handle(UpsertRoleCommand request, CancellationToken cancellationToken)
    {
        var roleId = new Snowflake(request.Id);
        var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
        
        if (role is null)
        {
            role = new Role(roleId, new Snowflake(request.GuildId), request.Name);
            await _roleRepository.AddAsync(role, cancellationToken);
        }
        
        role.Update(
            request.Name,
            request.Color,
            request.Position,
            request.Permissions,
            request.IsHoisted,
            request.IsMentionable,
            request.IsManaged,
            request.RawJson);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return role;
    }
}
