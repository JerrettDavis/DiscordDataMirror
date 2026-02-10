using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordDataMirror.Infrastructure.Persistence.Configurations;

public class GuildConfiguration : IEntityTypeConfiguration<Guild>
{
    public void Configure(EntityTypeBuilder<Guild> builder)
    {
        builder.ToTable("guilds");
        
        builder.HasKey(g => g.Id);
        
        builder.Property(g => g.Id)
            .HasColumnName("id")
            .HasMaxLength(20)
            .HasConversion(
                v => v.Value,
                v => new Snowflake(v));
        
        builder.Property(g => g.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();
        
        builder.Property(g => g.IconUrl)
            .HasColumnName("icon_url");
        
        builder.Property(g => g.Description)
            .HasColumnName("description");
        
        builder.Property(g => g.OwnerId)
            .HasColumnName("owner_id")
            .HasMaxLength(20)
            .HasConversion(
                v => v.Value,
                v => new Snowflake(v));
        
        builder.Property(g => g.CreatedAt)
            .HasColumnName("created_at");
        
        builder.Property(g => g.LastSyncedAt)
            .HasColumnName("last_synced_at");
        
        builder.Property(g => g.RawJson)
            .HasColumnName("raw_json")
            .HasColumnType("jsonb");
        
        builder.HasMany(g => g.Channels)
            .WithOne(c => c.Guild)
            .HasForeignKey(c => c.GuildId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(g => g.Roles)
            .WithOne(r => r.Guild)
            .HasForeignKey(r => r.GuildId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(g => g.Members)
            .WithOne(m => m.Guild)
            .HasForeignKey(m => m.GuildId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
