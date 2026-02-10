using DiscordDataMirror.Domain.Common;
using DiscordDataMirror.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DiscordDataMirror.Infrastructure.Persistence.Repositories;

public class GenericRepository<TEntity, TId> : IRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : notnull
{
    protected readonly DiscordMirrorDbContext Context;
    protected readonly DbSet<TEntity> DbSet;

    public GenericRepository(DiscordMirrorDbContext context)
    {
        Context = context;
        DbSet = context.Set<TEntity>();
    }

    public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken ct = default)
        => await DbSet.FindAsync([id], ct);

    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken ct = default)
        => await DbSet.ToListAsync(ct);

    public virtual async Task AddAsync(TEntity entity, CancellationToken ct = default)
        => await DbSet.AddAsync(entity, ct);

    public virtual Task UpdateAsync(TEntity entity, CancellationToken ct = default)
    {
        DbSet.Update(entity);
        return Task.CompletedTask;
    }

    public virtual Task DeleteAsync(TEntity entity, CancellationToken ct = default)
    {
        DbSet.Remove(entity);
        return Task.CompletedTask;
    }

    public virtual async Task<bool> ExistsAsync(TId id, CancellationToken ct = default)
        => await DbSet.AnyAsync(e => e.Id.Equals(id), ct);
}
