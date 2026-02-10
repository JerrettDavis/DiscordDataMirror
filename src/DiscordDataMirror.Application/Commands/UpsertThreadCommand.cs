using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.Repositories;
using DiscordDataMirror.Domain.ValueObjects;
using MediatR;
using Thread = DiscordDataMirror.Domain.Entities.Thread;

namespace DiscordDataMirror.Application.Commands;

public record UpsertThreadCommand(
    string Id,
    string ParentChannelId,
    string? OwnerId,
    int MessageCount,
    int MemberCount,
    bool IsArchived,
    bool IsLocked,
    DateTime? ArchiveTimestamp,
    int? AutoArchiveDuration
) : IRequest<Thread>;

public class UpsertThreadCommandHandler : IRequestHandler<UpsertThreadCommand, Thread>
{
    private readonly IThreadRepository _threadRepository;
    private readonly IUnitOfWork _unitOfWork;
    
    public UpsertThreadCommandHandler(IThreadRepository threadRepository, IUnitOfWork unitOfWork)
    {
        _threadRepository = threadRepository;
        _unitOfWork = unitOfWork;
    }
    
    public async Task<Thread> Handle(UpsertThreadCommand request, CancellationToken cancellationToken)
    {
        var threadId = new Snowflake(request.Id);
        var thread = await _threadRepository.GetByIdAsync(threadId, cancellationToken);
        
        if (thread is null)
        {
            thread = new Thread(threadId, new Snowflake(request.ParentChannelId));
            await _threadRepository.AddAsync(thread, cancellationToken);
        }
        
        thread.Update(
            string.IsNullOrWhiteSpace(request.OwnerId) ? null : new Snowflake(request.OwnerId),
            request.MessageCount,
            request.MemberCount,
            request.IsArchived,
            request.IsLocked,
            request.ArchiveTimestamp,
            request.AutoArchiveDuration);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return thread;
    }
}
