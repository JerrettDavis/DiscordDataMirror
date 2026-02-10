using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordDataMirror.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnName("id")
            .HasMaxLength(20)
            .HasConversion(
                v => v.Value,
                v => new Snowflake(v));

        builder.Property(u => u.Username)
            .HasColumnName("username")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(u => u.Discriminator)
            .HasColumnName("discriminator")
            .HasMaxLength(4);

        builder.Property(u => u.GlobalName)
            .HasColumnName("global_name")
            .HasMaxLength(32);

        builder.Property(u => u.AvatarUrl)
            .HasColumnName("avatar_url");

        builder.Property(u => u.IsBot)
            .HasColumnName("is_bot")
            .HasDefaultValue(false);

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(u => u.LastSeenAt)
            .HasColumnName("last_seen_at");

        builder.Property(u => u.RawJson)
            .HasColumnName("raw_json")
            .HasColumnType("jsonb");

        builder.HasMany(u => u.Memberships)
            .WithOne(m => m.User)
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.Messages)
            .WithOne(m => m.Author)
            .HasForeignKey(m => m.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
