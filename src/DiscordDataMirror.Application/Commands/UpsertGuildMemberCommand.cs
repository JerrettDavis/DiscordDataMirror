using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.Repositories;
using DiscordDataMirror.Domain.ValueObjects;
using MediatR;

namespace DiscordDataMirror.Application.Commands;

public record UpsertGuildMemberCommand(
    string UserId,
    string GuildId,
    string? Nickname,
    DateTime? JoinedAt,
    bool IsPending,
    List<string> RoleIds,
    string? RawJson = null
) : IRequest<GuildMember>;

public class UpsertGuildMemberCommandHandler : IRequestHandler<UpsertGuildMemberCommand, GuildMember>
{
    private readonly IGuildMemberRepository _guildMemberRepository;
    private readonly IUnitOfWork _unitOfWork;
    
    public UpsertGuildMemberCommandHandler(IGuildMemberRepository guildMemberRepository, IUnitOfWork unitOfWork)
    {
        _guildMemberRepository = guildMemberRepository;
        _unitOfWork = unitOfWork;
    }
    
    public async Task<GuildMember> Handle(UpsertGuildMemberCommand request, CancellationToken cancellationToken)
    {
        var userId = new Snowflake(request.UserId);
        var guildId = new Snowflake(request.GuildId);
        var member = await _guildMemberRepository.GetByUserAndGuildAsync(userId, guildId, cancellationToken);
        
        if (member is null)
        {
            member = new GuildMember(userId, guildId);
            await _guildMemberRepository.AddAsync(member, cancellationToken);
        }
        
        member.Update(
            request.Nickname,
            request.JoinedAt,
            request.IsPending,
            request.RoleIds,
            request.RawJson);
        member.MarkSynced();
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return member;
    }
}
