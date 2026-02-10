using DiscordDataMirror.Domain.Common;
using DiscordDataMirror.Domain.ValueObjects;

namespace DiscordDataMirror.Domain.Entities;

/// <summary>
/// Represents a Discord server (guild).
/// </summary>
public class Guild : Entity<Snowflake>
{
    public string Name { get; private set; } = string.Empty;
    public string? IconUrl { get; private set; }
    public string? Description { get; private set; }
    public Snowflake OwnerId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastSyncedAt { get; private set; }
    public string? RawJson { get; private set; }
    
    // Navigation properties
    private readonly List<Channel> _channels = [];
    public IReadOnlyCollection<Channel> Channels => _channels.AsReadOnly();
    
    private readonly List<Role> _roles = [];
    public IReadOnlyCollection<Role> Roles => _roles.AsReadOnly();
    
    private readonly List<GuildMember> _members = [];
    public IReadOnlyCollection<GuildMember> Members => _members.AsReadOnly();
    
    private Guild() { } // EF Core
    
    public Guild(Snowflake id, string name, Snowflake ownerId, DateTime createdAt)
    {
        Id = id;
        Name = name;
        OwnerId = ownerId;
        CreatedAt = createdAt;
    }
    
    public void Update(string name, string? iconUrl, string? description, Snowflake ownerId, string? rawJson = null)
    {
        Name = name;
        IconUrl = iconUrl;
        Description = description;
        OwnerId = ownerId;
        RawJson = rawJson;
    }
    
    public void MarkSynced() => LastSyncedAt = DateTime.UtcNow;
    
    public void AddChannel(Channel channel)
    {
        if (!_channels.Any(c => c.Id == channel.Id))
            _channels.Add(channel);
    }
    
    public void AddRole(Role role)
    {
        if (!_roles.Any(r => r.Id == role.Id))
            _roles.Add(role);
    }
    
    public void AddMember(GuildMember member)
    {
        if (!_members.Any(m => m.UserId == member.UserId))
            _members.Add(member);
    }
}
