using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DiscordDataMirror.Infrastructure.Persistence;

public class DiscordMirrorDbContext : DbContext, IUnitOfWork
{
    public DbSet<Guild> Guilds => Set<Guild>();
    public DbSet<Channel> Channels => Set<Channel>();
    public DbSet<User> Users => Set<User>();
    public DbSet<GuildMember> GuildMembers => Set<GuildMember>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<Embed> Embeds => Set<Embed>();
    public DbSet<Reaction> Reactions => Set<Reaction>();
    public DbSet<Domain.Entities.Thread> Threads => Set<Domain.Entities.Thread>();
    public DbSet<SyncState> SyncStates => Set<SyncState>();
    public DbSet<UserMap> UserMaps => Set<UserMap>();
    
    public DiscordMirrorDbContext(DbContextOptions<DiscordMirrorDbContext> options) : base(options)
    {
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DiscordMirrorDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
