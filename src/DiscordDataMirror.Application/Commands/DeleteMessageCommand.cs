using DiscordDataMirror.Domain.Repositories;
using DiscordDataMirror.Domain.ValueObjects;
using MediatR;

namespace DiscordDataMirror.Application.Commands;

public record DeleteMessageCommand(string MessageId) : IRequest<bool>;

public class DeleteMessageCommandHandler : IRequestHandler<DeleteMessageCommand, bool>
{
    private readonly IMessageRepository _messageRepository;
    private readonly IUnitOfWork _unitOfWork;
    
    public DeleteMessageCommandHandler(IMessageRepository messageRepository, IUnitOfWork unitOfWork)
    {
        _messageRepository = messageRepository;
        _unitOfWork = unitOfWork;
    }
    
    public async Task<bool> Handle(DeleteMessageCommand request, CancellationToken cancellationToken)
    {
        var messageId = new Snowflake(request.MessageId);
        var message = await _messageRepository.GetByIdAsync(messageId, cancellationToken);
        
        if (message is null)
            return false;
        
        await _messageRepository.DeleteAsync(message, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return true;
    }
}
