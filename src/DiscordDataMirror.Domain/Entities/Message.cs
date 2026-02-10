using DiscordDataMirror.Domain.Common;
using DiscordDataMirror.Domain.ValueObjects;

namespace DiscordDataMirror.Domain.Entities;

public enum MessageType
{
    Default = 0,
    RecipientAdd = 1,
    RecipientRemove = 2,
    Call = 3,
    ChannelNameChange = 4,
    ChannelIconChange = 5,
    ChannelPinnedMessage = 6,
    GuildMemberJoin = 7,
    UserPremiumGuildSubscription = 8,
    UserPremiumGuildSubscriptionTier1 = 9,
    UserPremiumGuildSubscriptionTier2 = 10,
    UserPremiumGuildSubscriptionTier3 = 11,
    ChannelFollowAdd = 12,
    GuildDiscoveryDisqualified = 14,
    GuildDiscoveryRequalified = 15,
    Reply = 19,
    ApplicationCommand = 20,
    ThreadStarterMessage = 21,
    GuildInviteReminder = 22,
    ContextMenuCommand = 23,
    AutoModerationAction = 24,
    RoleSubscriptionPurchase = 25,
    InteractionPremiumUpsell = 26,
    StageStart = 27,
    StageEnd = 28,
    StageSpeaker = 29,
    StageTopic = 31,
    GuildApplicationPremiumSubscription = 32
}

/// <summary>
/// Represents a Discord message.
/// </summary>
public class Message : Entity<Snowflake>
{
    public Snowflake ChannelId { get; private set; }
    public Snowflake AuthorId { get; private set; }
    public string? Content { get; private set; }
    public string? CleanContent { get; private set; }
    public DateTime Timestamp { get; private set; }
    public DateTime? EditedTimestamp { get; private set; }
    public MessageType Type { get; private set; }
    public bool IsPinned { get; private set; }
    public bool IsTts { get; private set; }
    public Snowflake? ReferencedMessageId { get; private set; }
    public string? RawJson { get; private set; }
    
    // Navigation
    public Channel? Channel { get; private set; }
    public User? Author { get; private set; }
    public Message? ReferencedMessage { get; private set; }
    
    private readonly List<Attachment> _attachments = [];
    public IReadOnlyCollection<Attachment> Attachments => _attachments.AsReadOnly();
    
    private readonly List<Embed> _embeds = [];
    public IReadOnlyCollection<Embed> Embeds => _embeds.AsReadOnly();
    
    private readonly List<Reaction> _reactions = [];
    public IReadOnlyCollection<Reaction> Reactions => _reactions.AsReadOnly();
    
    private Message() { } // EF Core
    
    public Message(Snowflake id, Snowflake channelId, Snowflake authorId, DateTime timestamp)
    {
        Id = id;
        ChannelId = channelId;
        AuthorId = authorId;
        Timestamp = timestamp;
    }
    
    public void Update(string? content, string? cleanContent, MessageType type, bool isPinned, bool isTts,
        DateTime? editedTimestamp, Snowflake? referencedMessageId, string? rawJson = null)
    {
        Content = content;
        CleanContent = cleanContent;
        Type = type;
        IsPinned = isPinned;
        IsTts = isTts;
        EditedTimestamp = editedTimestamp;
        ReferencedMessageId = referencedMessageId;
        RawJson = rawJson;
    }
    
    public void AddAttachment(Attachment attachment)
    {
        if (!_attachments.Any(a => a.Id == attachment.Id))
            _attachments.Add(attachment);
    }
    
    public void AddEmbed(Embed embed) => _embeds.Add(embed);
    
    public void AddReaction(Reaction reaction)
    {
        var existing = _reactions.FirstOrDefault(r => r.EmoteKey == reaction.EmoteKey);
        if (existing is null)
            _reactions.Add(reaction);
    }
    
    public void ClearAttachments() => _attachments.Clear();
    public void ClearEmbeds() => _embeds.Clear();
    public void ClearReactions() => _reactions.Clear();
}
