using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.Repositories;
using DiscordDataMirror.Domain.ValueObjects;
using MediatR;

namespace DiscordDataMirror.Application.Commands;

public record UpsertGuildCommand(
    string Id,
    string Name,
    string? IconUrl,
    string? Description,
    string OwnerId,
    DateTime CreatedAt,
    string? RawJson = null
) : IRequest<Guild>;

public class UpsertGuildCommandHandler : IRequestHandler<UpsertGuildCommand, Guild>
{
    private readonly IGuildRepository _guildRepository;
    private readonly IUnitOfWork _unitOfWork;
    
    public UpsertGuildCommandHandler(IGuildRepository guildRepository, IUnitOfWork unitOfWork)
    {
        _guildRepository = guildRepository;
        _unitOfWork = unitOfWork;
    }
    
    public async Task<Guild> Handle(UpsertGuildCommand request, CancellationToken cancellationToken)
    {
        var snowflakeId = new Snowflake(request.Id);
        var guild = await _guildRepository.GetByIdAsync(snowflakeId, cancellationToken);
        
        if (guild is null)
        {
            guild = new Guild(snowflakeId, request.Name, new Snowflake(request.OwnerId), request.CreatedAt);
            await _guildRepository.AddAsync(guild, cancellationToken);
        }
        
        guild.Update(request.Name, request.IconUrl, request.Description, new Snowflake(request.OwnerId), request.RawJson);
        guild.MarkSynced();
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return guild;
    }
}
