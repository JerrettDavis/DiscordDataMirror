using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordDataMirror.Infrastructure.Persistence.Configurations;

public class ChannelConfiguration : IEntityTypeConfiguration<Channel>
{
    public void Configure(EntityTypeBuilder<Channel> builder)
    {
        builder.ToTable("channels");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .HasMaxLength(20)
            .HasConversion(
                v => v.Value,
                v => new Snowflake(v));

        builder.Property(c => c.GuildId)
            .HasColumnName("guild_id")
            .HasMaxLength(20)
            .HasConversion(
                v => v.Value,
                v => new Snowflake(v));

        builder.Property(c => c.ParentId)
            .HasColumnName("parent_id")
            .HasMaxLength(20)
            .HasConversion(
                v => v == null ? null : v.Value.Value,
                v => string.IsNullOrWhiteSpace(v) ? null : new Snowflake(v));

        builder.Property(c => c.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Type)
            .HasColumnName("type");

        builder.Property(c => c.Topic)
            .HasColumnName("topic");

        builder.Property(c => c.Position)
            .HasColumnName("position");

        builder.Property(c => c.IsNsfw)
            .HasColumnName("is_nsfw")
            .HasDefaultValue(false);

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(c => c.LastSyncedAt)
            .HasColumnName("last_synced_at");

        builder.Property(c => c.RawJson)
            .HasColumnName("raw_json")
            .HasColumnType("jsonb");

        builder.HasOne(c => c.Parent)
            .WithMany()
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(c => c.Messages)
            .WithOne(m => m.Channel)
            .HasForeignKey(m => m.ChannelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.GuildId).HasDatabaseName("idx_channels_guild");
    }
}
