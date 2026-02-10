using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.Repositories;
using DiscordDataMirror.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace DiscordDataMirror.Infrastructure.Persistence.Repositories;

public class AttachmentRepository : GenericRepository<Attachment, Snowflake>, IAttachmentRepository
{
    public AttachmentRepository(DiscordMirrorDbContext context) : base(context)
    {
    }
    
    public async Task<IReadOnlyList<Attachment>> GetByMessageIdAsync(Snowflake messageId, CancellationToken ct = default)
        => await DbSet
            .Where(a => a.MessageId == messageId)
            .ToListAsync(ct);
    
    public async Task<IReadOnlyList<Attachment>> GetUncachedAsync(int take = 100, CancellationToken ct = default)
        => await DbSet
            .Where(a => !a.IsCached && a.DownloadStatus == AttachmentDownloadStatus.Pending)
            .OrderBy(a => a.Id)
            .Take(take)
            .ToListAsync(ct);
    
    public async Task<IReadOnlyList<Attachment>> GetByStatusAsync(AttachmentDownloadStatus status, int take = 100, CancellationToken ct = default)
        => await DbSet
            .Where(a => a.DownloadStatus == status)
            .OrderBy(a => a.Id)
            .Take(take)
            .ToListAsync(ct);
    
    public async Task<IReadOnlyList<Attachment>> GetQueuedAsync(int take = 100, CancellationToken ct = default)
        => await DbSet
            .Where(a => a.DownloadStatus == AttachmentDownloadStatus.Queued)
            .OrderBy(a => a.Id)
            .Take(take)
            .ToListAsync(ct);
    
    public async Task<IReadOnlyList<Attachment>> GetFailedAsync(int maxAttempts = 3, int take = 100, CancellationToken ct = default)
        => await DbSet
            .Where(a => a.DownloadStatus == AttachmentDownloadStatus.Failed && a.DownloadAttempts < maxAttempts)
            .OrderBy(a => a.DownloadAttempts)
            .ThenBy(a => a.Id)
            .Take(take)
            .ToListAsync(ct);
    
    public async Task<Attachment?> GetByContentHashAsync(string contentHash, CancellationToken ct = default)
        => await DbSet
            .Where(a => a.ContentHash == contentHash && a.IsCached)
            .FirstOrDefaultAsync(ct);
    
    public async Task<IReadOnlyList<string>> GetAllLocalPathsAsync(CancellationToken ct = default)
        => await DbSet
            .Where(a => a.LocalPath != null)
            .Select(a => a.LocalPath!)
            .ToListAsync(ct);
    
    public async Task<AttachmentStorageStatistics> GetStorageStatisticsAsync(CancellationToken ct = default)
    {
        var stats = await DbSet
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalCount = g.LongCount(),
                CachedCount = g.LongCount(a => a.IsCached),
                TotalSizeBytes = g.Sum(a => a.Size),
                CachedSizeBytes = g.Where(a => a.IsCached).Sum(a => a.Size),
                PendingCount = g.LongCount(a => a.DownloadStatus == AttachmentDownloadStatus.Pending),
                QueuedCount = g.LongCount(a => a.DownloadStatus == AttachmentDownloadStatus.Queued),
                FailedCount = g.LongCount(a => a.DownloadStatus == AttachmentDownloadStatus.Failed),
                SkippedCount = g.LongCount(a => a.DownloadStatus == AttachmentDownloadStatus.Skipped)
            })
            .FirstOrDefaultAsync(ct);
        
        var uniqueHashCount = await DbSet
            .Where(a => a.ContentHash != null)
            .Select(a => a.ContentHash)
            .Distinct()
            .LongCountAsync(ct);
        
        return new AttachmentStorageStatistics(
            stats?.TotalCount ?? 0,
            stats?.CachedCount ?? 0,
            stats?.TotalSizeBytes ?? 0,
            stats?.CachedSizeBytes ?? 0,
            stats?.PendingCount ?? 0,
            stats?.QueuedCount ?? 0,
            stats?.FailedCount ?? 0,
            stats?.SkippedCount ?? 0,
            uniqueHashCount);
    }
}
