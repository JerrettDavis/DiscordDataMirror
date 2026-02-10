using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.Repositories;
using DiscordDataMirror.Domain.ValueObjects;
using MediatR;

namespace DiscordDataMirror.Application.Commands;

public record UpsertChannelCommand(
    string Id,
    string GuildId,
    string Name,
    ChannelType Type,
    string? Topic,
    int Position,
    bool IsNsfw,
    string? ParentId,
    DateTime CreatedAt,
    string? RawJson = null
) : IRequest<Channel>;

public class UpsertChannelCommandHandler : IRequestHandler<UpsertChannelCommand, Channel>
{
    private readonly IChannelRepository _channelRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpsertChannelCommandHandler(IChannelRepository channelRepository, IUnitOfWork unitOfWork)
    {
        _channelRepository = channelRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Channel> Handle(UpsertChannelCommand request, CancellationToken cancellationToken)
    {
        var channelId = new Snowflake(request.Id);
        var channel = await _channelRepository.GetByIdAsync(channelId, cancellationToken);

        if (channel is null)
        {
            channel = new Channel(channelId, new Snowflake(request.GuildId), request.Name, request.Type, request.CreatedAt);
            await _channelRepository.AddAsync(channel, cancellationToken);
        }

        channel.Update(
            request.Name,
            request.Type,
            request.Topic,
            request.Position,
            request.IsNsfw,
            string.IsNullOrWhiteSpace(request.ParentId) ? null : new Snowflake(request.ParentId),
            request.RawJson);
        channel.MarkSynced();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return channel;
    }
}
