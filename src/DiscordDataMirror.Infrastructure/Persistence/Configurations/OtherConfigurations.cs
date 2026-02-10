using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thread = DiscordDataMirror.Domain.Entities.Thread;

namespace DiscordDataMirror.Infrastructure.Persistence.Configurations;

public class GuildMemberConfiguration : IEntityTypeConfiguration<GuildMember>
{
    public void Configure(EntityTypeBuilder<GuildMember> builder)
    {
        builder.ToTable("guild_members");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id").HasMaxLength(50);

        builder.Property(m => m.UserId)
            .HasColumnName("user_id")
            .HasMaxLength(20)
            .HasConversion(v => v.Value, v => new Snowflake(v));

        builder.Property(m => m.GuildId)
            .HasColumnName("guild_id")
            .HasMaxLength(20)
            .HasConversion(v => v.Value, v => new Snowflake(v));

        builder.Property(m => m.Nickname).HasColumnName("nickname").HasMaxLength(32);
        builder.Property(m => m.JoinedAt).HasColumnName("joined_at");
        builder.Property(m => m.IsPending).HasColumnName("is_pending").HasDefaultValue(false);
        builder.Property(m => m.RoleIds).HasColumnName("role_ids").HasColumnType("jsonb");
        builder.Property(m => m.LastSyncedAt).HasColumnName("last_synced_at");
        builder.Property(m => m.RawJson).HasColumnName("raw_json").HasColumnType("jsonb");

        builder.HasIndex(m => m.GuildId).HasDatabaseName("idx_guild_members_guild");
        builder.HasIndex(m => new { m.UserId, m.GuildId }).IsUnique();
    }
}

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasColumnName("id").HasMaxLength(20)
            .HasConversion(v => v.Value, v => new Snowflake(v));

        builder.Property(r => r.GuildId)
            .HasColumnName("guild_id").HasMaxLength(20)
            .HasConversion(v => v.Value, v => new Snowflake(v));

        builder.Property(r => r.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(r => r.Color).HasColumnName("color");
        builder.Property(r => r.Position).HasColumnName("position");
        builder.Property(r => r.Permissions).HasColumnName("permissions").HasMaxLength(20);
        builder.Property(r => r.IsHoisted).HasColumnName("is_hoisted").HasDefaultValue(false);
        builder.Property(r => r.IsMentionable).HasColumnName("is_mentionable").HasDefaultValue(false);
        builder.Property(r => r.IsManaged).HasColumnName("is_managed").HasDefaultValue(false);
        builder.Property(r => r.RawJson).HasColumnName("raw_json").HasColumnType("jsonb");
    }
}

public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("attachments");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasColumnName("id").HasMaxLength(20)
            .HasConversion(v => v.Value, v => new Snowflake(v));

        builder.Property(a => a.MessageId)
            .HasColumnName("message_id").HasMaxLength(20)
            .HasConversion(v => v.Value, v => new Snowflake(v));

        builder.Property(a => a.Filename).HasColumnName("filename").HasMaxLength(255).IsRequired();
        builder.Property(a => a.Url).HasColumnName("url").IsRequired();
        builder.Property(a => a.ProxyUrl).HasColumnName("proxy_url");
        builder.Property(a => a.Size).HasColumnName("size");
        builder.Property(a => a.Width).HasColumnName("width");
        builder.Property(a => a.Height).HasColumnName("height");
        builder.Property(a => a.ContentType).HasColumnName("content_type").HasMaxLength(100);
        builder.Property(a => a.LocalPath).HasColumnName("local_path");
        builder.Property(a => a.IsCached).HasColumnName("is_cached").HasDefaultValue(false);

        // Download tracking fields
        builder.Property(a => a.DownloadStatus).HasColumnName("download_status").HasDefaultValue(AttachmentDownloadStatus.Pending);
        builder.Property(a => a.ContentHash).HasColumnName("content_hash").HasMaxLength(64);
        builder.Property(a => a.DownloadedAt).HasColumnName("downloaded_at");
        builder.Property(a => a.DownloadAttempts).HasColumnName("download_attempts").HasDefaultValue(0);
        builder.Property(a => a.LastDownloadError).HasColumnName("last_download_error");
        builder.Property(a => a.SkipReason).HasColumnName("skip_reason").HasMaxLength(255);

        builder.HasIndex(a => a.MessageId).HasDatabaseName("idx_attachments_message");
        builder.HasIndex(a => a.DownloadStatus).HasDatabaseName("idx_attachments_download_status");
        builder.HasIndex(a => a.ContentHash).HasDatabaseName("idx_attachments_content_hash");
    }
}

public class EmbedConfiguration : IEntityTypeConfiguration<Embed>
{
    public void Configure(EntityTypeBuilder<Embed> builder)
    {
        builder.ToTable("embeds");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(e => e.MessageId)
            .HasColumnName("message_id").HasMaxLength(20)
            .HasConversion(v => v.Value, v => new Snowflake(v));

        builder.Property(e => e.Index).HasColumnName("index");
        builder.Property(e => e.Type).HasColumnName("type").HasMaxLength(20);
        builder.Property(e => e.Title).HasColumnName("title");
        builder.Property(e => e.Description).HasColumnName("description");
        builder.Property(e => e.Url).HasColumnName("url");
        builder.Property(e => e.Timestamp).HasColumnName("timestamp");
        builder.Property(e => e.Color).HasColumnName("color");
        builder.Property(e => e.Data).HasColumnName("data").HasColumnType("jsonb").IsRequired();

        builder.HasIndex(e => e.MessageId).HasDatabaseName("idx_embeds_message");
    }
}

public class ReactionConfiguration : IEntityTypeConfiguration<Reaction>
{
    public void Configure(EntityTypeBuilder<Reaction> builder)
    {
        builder.ToTable("reactions");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").HasMaxLength(150);

        builder.Property(r => r.MessageId)
            .HasColumnName("message_id").HasMaxLength(20)
            .HasConversion(v => v.Value, v => new Snowflake(v));

        builder.Property(r => r.EmoteKey).HasColumnName("emote_key").HasMaxLength(100).IsRequired();
        builder.Property(r => r.Count).HasColumnName("count").HasDefaultValue(0);
        builder.Property(r => r.UserIds).HasColumnName("user_ids").HasColumnType("jsonb");

        builder.HasIndex(r => new { r.MessageId, r.EmoteKey }).IsUnique();
    }
}

public class ThreadConfiguration : IEntityTypeConfiguration<Thread>
{
    public void Configure(EntityTypeBuilder<Thread> builder)
    {
        builder.ToTable("threads");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasColumnName("id").HasMaxLength(20)
            .HasConversion(v => v.Value, v => new Snowflake(v));

        builder.Property(t => t.ParentChannelId)
            .HasColumnName("parent_channel_id").HasMaxLength(20)
            .HasConversion(v => v.Value, v => new Snowflake(v));

        builder.Property(t => t.OwnerId)
            .HasColumnName("owner_id").HasMaxLength(20)
            .HasConversion(v => v == null ? null : v.Value.Value, v => string.IsNullOrWhiteSpace(v) ? null : new Snowflake(v));

        builder.Property(t => t.MessageCount).HasColumnName("message_count").HasDefaultValue(0);
        builder.Property(t => t.MemberCount).HasColumnName("member_count").HasDefaultValue(0);
        builder.Property(t => t.IsArchived).HasColumnName("is_archived").HasDefaultValue(false);
        builder.Property(t => t.IsLocked).HasColumnName("is_locked").HasDefaultValue(false);
        builder.Property(t => t.ArchiveTimestamp).HasColumnName("archive_timestamp");
        builder.Property(t => t.AutoArchiveDuration).HasColumnName("auto_archive_duration");

        builder.HasOne(t => t.Channel).WithMany().HasForeignKey(t => t.Id);
        builder.HasOne(t => t.ParentChannel).WithMany().HasForeignKey(t => t.ParentChannelId);
        builder.HasOne(t => t.Owner).WithMany().HasForeignKey(t => t.OwnerId);
    }
}

public class SyncStateConfiguration : IEntityTypeConfiguration<SyncState>
{
    public void Configure(EntityTypeBuilder<SyncState> builder)
    {
        builder.ToTable("sync_state");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(s => s.EntityType).HasColumnName("entity_type").HasMaxLength(50).IsRequired();
        builder.Property(s => s.EntityId).HasColumnName("entity_id").HasMaxLength(20).IsRequired();
        builder.Property(s => s.LastSyncedAt).HasColumnName("last_synced_at");
        builder.Property(s => s.LastMessageId)
            .HasColumnName("last_message_id").HasMaxLength(20)
            .HasConversion(v => v == null ? null : v.Value.Value, v => string.IsNullOrWhiteSpace(v) ? null : new Snowflake(v));
        builder.Property(s => s.Status).HasColumnName("status").HasDefaultValue(SyncStatus.Idle);
        builder.Property(s => s.ErrorMessage).HasColumnName("error_message");

        builder.HasIndex(s => new { s.EntityType, s.EntityId }).IsUnique();
    }
}

public class UserMapConfiguration : IEntityTypeConfiguration<UserMap>
{
    public void Configure(EntityTypeBuilder<UserMap> builder)
    {
        builder.ToTable("user_maps");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(m => m.CanonicalUserId)
            .HasColumnName("canonical_user_id").HasMaxLength(20)
            .HasConversion(v => v.Value, v => new Snowflake(v));

        builder.Property(m => m.MappedUserId)
            .HasColumnName("mapped_user_id").HasMaxLength(20)
            .HasConversion(v => v.Value, v => new Snowflake(v));

        builder.Property(m => m.Confidence).HasColumnName("confidence").HasPrecision(3, 2);
        builder.Property(m => m.MappingType).HasColumnName("mapping_type");
        builder.Property(m => m.CreatedAt).HasColumnName("created_at");
        builder.Property(m => m.Notes).HasColumnName("notes");

        builder.HasOne(m => m.CanonicalUser).WithMany().HasForeignKey(m => m.CanonicalUserId);
        builder.HasOne(m => m.MappedUser).WithMany().HasForeignKey(m => m.MappedUserId);

        builder.HasIndex(m => new { m.CanonicalUserId, m.MappedUserId }).IsUnique();
    }
}
