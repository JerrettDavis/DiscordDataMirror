using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.Repositories;
using DiscordDataMirror.Domain.ValueObjects;
using MediatR;

namespace DiscordDataMirror.Application.Commands;

public record UpsertUserCommand(
    string Id,
    string Username,
    string? Discriminator,
    string? GlobalName,
    string? AvatarUrl,
    bool IsBot,
    DateTime CreatedAt,
    string? RawJson = null
) : IRequest<User>;

public class UpsertUserCommandHandler : IRequestHandler<UpsertUserCommand, User>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpsertUserCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<User> Handle(UpsertUserCommand request, CancellationToken cancellationToken)
    {
        var userId = new Snowflake(request.Id);
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            user = new User(userId, request.Username, request.CreatedAt);
            await _userRepository.AddAsync(user, cancellationToken);
        }

        user.Update(
            request.Username,
            request.Discriminator,
            request.GlobalName,
            request.AvatarUrl,
            request.IsBot,
            request.RawJson);
        user.MarkSeen();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return user;
    }
}
