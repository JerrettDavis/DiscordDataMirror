using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.Repositories;
using DiscordDataMirror.Domain.ValueObjects;
using MediatR;

namespace DiscordDataMirror.Application.Commands;

public record AddReactionCommand(
    string MessageId,
    string EmoteKey,
    string UserId
) : IRequest<Reaction>;

public record RemoveReactionCommand(
    string MessageId,
    string EmoteKey,
    string UserId
) : IRequest<Reaction?>;

public class AddReactionCommandHandler : IRequestHandler<AddReactionCommand, Reaction>
{
    private readonly IReactionRepository _reactionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddReactionCommandHandler(IReactionRepository reactionRepository, IUnitOfWork unitOfWork)
    {
        _reactionRepository = reactionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Reaction> Handle(AddReactionCommand request, CancellationToken cancellationToken)
    {
        var messageId = new Snowflake(request.MessageId);
        var reaction = await _reactionRepository.GetByMessageAndEmoteAsync(messageId, request.EmoteKey, cancellationToken);

        if (reaction is null)
        {
            reaction = new Reaction(messageId, request.EmoteKey, 0);
            await _reactionRepository.AddAsync(reaction, cancellationToken);
        }

        reaction.AddUser(request.UserId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return reaction;
    }
}

public class RemoveReactionCommandHandler : IRequestHandler<RemoveReactionCommand, Reaction?>
{
    private readonly IReactionRepository _reactionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveReactionCommandHandler(IReactionRepository reactionRepository, IUnitOfWork unitOfWork)
    {
        _reactionRepository = reactionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Reaction?> Handle(RemoveReactionCommand request, CancellationToken cancellationToken)
    {
        var messageId = new Snowflake(request.MessageId);
        var reaction = await _reactionRepository.GetByMessageAndEmoteAsync(messageId, request.EmoteKey, cancellationToken);

        if (reaction is null)
            return null;

        reaction.RemoveUser(request.UserId);

        // Delete reaction if no users left
        if (reaction.Count == 0)
        {
            await _reactionRepository.DeleteAsync(reaction, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return null;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return reaction;
    }
}
