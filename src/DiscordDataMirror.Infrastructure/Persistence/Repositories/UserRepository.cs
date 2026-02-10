using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.Repositories;
using DiscordDataMirror.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace DiscordDataMirror.Infrastructure.Persistence.Repositories;

public class UserRepository : GenericRepository<User, Snowflake>, IUserRepository
{
    public UserRepository(DiscordMirrorDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
        => await DbSet
            .FirstOrDefaultAsync(u => u.Username == username, ct);

    public async Task<IReadOnlyList<User>> SearchByUsernameAsync(string searchTerm, int take = 20, CancellationToken ct = default)
        => await DbSet
            .Where(u => EF.Functions.ILike(u.Username, $"%{searchTerm}%") ||
                        (u.GlobalName != null && EF.Functions.ILike(u.GlobalName, $"%{searchTerm}%")))
            .Take(take)
            .ToListAsync(ct);
}
