using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordDataMirror.Infrastructure.Persistence.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("messages");
        
        builder.HasKey(m => m.Id);
        
        builder.Property(m => m.Id)
            .HasColumnName("id")
            .HasMaxLength(20)
            .HasConversion(
                v => v.Value,
                v => new Snowflake(v));
        
        builder.Property(m => m.ChannelId)
            .HasColumnName("channel_id")
            .HasMaxLength(20)
            .HasConversion(
                v => v.Value,
                v => new Snowflake(v));
        
        builder.Property(m => m.AuthorId)
            .HasColumnName("author_id")
            .HasMaxLength(20)
            .HasConversion(
                v => v.Value,
                v => new Snowflake(v));
        
        builder.Property(m => m.Content)
            .HasColumnName("content");
        
        builder.Property(m => m.CleanContent)
            .HasColumnName("clean_content");
        
        builder.Property(m => m.Timestamp)
            .HasColumnName("timestamp");
        
        builder.Property(m => m.EditedTimestamp)
            .HasColumnName("edited_timestamp");
        
        builder.Property(m => m.Type)
            .HasColumnName("type")
            .HasDefaultValue(MessageType.Default);
        
        builder.Property(m => m.IsPinned)
            .HasColumnName("is_pinned")
            .HasDefaultValue(false);
        
        builder.Property(m => m.IsTts)
            .HasColumnName("is_tts")
            .HasDefaultValue(false);
        
        builder.Property(m => m.ReferencedMessageId)
            .HasColumnName("referenced_message_id")
            .HasMaxLength(20)
            .HasConversion(
                v => v == null ? null : v.Value.Value,
                v => string.IsNullOrWhiteSpace(v) ? null : new Snowflake(v));
        
        builder.Property(m => m.RawJson)
            .HasColumnName("raw_json")
            .HasColumnType("jsonb");
        
        builder.HasOne(m => m.ReferencedMessage)
            .WithMany()
            .HasForeignKey(m => m.ReferencedMessageId)
            .OnDelete(DeleteBehavior.SetNull);
        
        builder.HasMany(m => m.Attachments)
            .WithOne(a => a.Message)
            .HasForeignKey(a => a.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(m => m.Embeds)
            .WithOne(e => e.Message)
            .HasForeignKey(e => e.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(m => m.Reactions)
            .WithOne(r => r.Message)
            .HasForeignKey(r => r.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasIndex(m => new { m.ChannelId, m.Timestamp })
            .HasDatabaseName("idx_messages_channel_timestamp")
            .IsDescending(false, true);
        
        builder.HasIndex(m => m.AuthorId)
            .HasDatabaseName("idx_messages_author");
    }
}
