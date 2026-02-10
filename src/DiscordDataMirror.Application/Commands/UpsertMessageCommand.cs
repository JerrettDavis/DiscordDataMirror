using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.Repositories;
using DiscordDataMirror.Domain.ValueObjects;
using MediatR;

namespace DiscordDataMirror.Application.Commands;

public record UpsertMessageCommand(
    string Id,
    string ChannelId,
    string AuthorId,
    string? Content,
    string? CleanContent,
    MessageType Type,
    bool IsPinned,
    bool IsTts,
    DateTime Timestamp,
    DateTime? EditedTimestamp,
    string? ReferencedMessageId,
    string? RawJson = null
) : IRequest<Message>;

public class UpsertMessageCommandHandler : IRequestHandler<UpsertMessageCommand, Message>
{
    private readonly IMessageRepository _messageRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpsertMessageCommandHandler(IMessageRepository messageRepository, IUnitOfWork unitOfWork)
    {
        _messageRepository = messageRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Message> Handle(UpsertMessageCommand request, CancellationToken cancellationToken)
    {
        var messageId = new Snowflake(request.Id);
        var message = await _messageRepository.GetByIdAsync(messageId, cancellationToken);

        if (message is null)
        {
            message = new Message(messageId, new Snowflake(request.ChannelId), new Snowflake(request.AuthorId), request.Timestamp);
            await _messageRepository.AddAsync(message, cancellationToken);
        }

        message.Update(
            request.Content,
            request.CleanContent,
            request.Type,
            request.IsPinned,
            request.IsTts,
            request.EditedTimestamp,
            string.IsNullOrWhiteSpace(request.ReferencedMessageId) ? null : new Snowflake(request.ReferencedMessageId),
            request.RawJson);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return message;
    }
}
