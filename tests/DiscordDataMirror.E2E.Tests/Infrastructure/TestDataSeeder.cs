using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.ValueObjects;
using DiscordDataMirror.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DiscordDataMirror.E2E.Tests.Infrastructure;

/// <summary>
/// Seeds the database with test data for E2E tests.
/// </summary>
public static class TestDataSeeder
{
    public static readonly Snowflake TestGuildId1 = new Snowflake(123456789012345678ul);
    public static readonly Snowflake TestGuildId2 = new Snowflake(223456789012345678ul);
    
    public static readonly Snowflake TestChannelId1 = new Snowflake(111111111111111111ul);
    public static readonly Snowflake TestChannelId2 = new Snowflake(222222222222222222ul);
    public static readonly Snowflake TestChannelId3 = new Snowflake(333333333333333333ul);
    public static readonly Snowflake TestCategoryId1 = new Snowflake(444444444444444444ul);
    public static readonly Snowflake TestVoiceChannelId = new Snowflake(555555555555555555ul);
    
    public static readonly Snowflake TestUserId1 = new Snowflake(999999999999999991ul);
    public static readonly Snowflake TestUserId2 = new Snowflake(999999999999999992ul);
    public static readonly Snowflake TestUserId3 = new Snowflake(999999999999999993ul);
    public static readonly Snowflake TestBotUserId = new Snowflake(999999999999999999ul);
    
    public static readonly Snowflake TestRoleId1 = new Snowflake(888888888888888881ul);
    public static readonly Snowflake TestRoleId2 = new Snowflake(888888888888888882ul);
    public static readonly Snowflake TestRoleId3 = new Snowflake(888888888888888883ul);

    public static async Task SeedAsync(DiscordMirrorDbContext context)
    {
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Clear existing data
        context.Messages.RemoveRange(context.Messages);
        context.Reactions.RemoveRange(context.Reactions);
        context.Attachments.RemoveRange(context.Attachments);
        context.Embeds.RemoveRange(context.Embeds);
        context.GuildMembers.RemoveRange(context.GuildMembers);
        context.Channels.RemoveRange(context.Channels);
        context.Roles.RemoveRange(context.Roles);
        context.Users.RemoveRange(context.Users);
        context.Guilds.RemoveRange(context.Guilds);
        context.SyncStates.RemoveRange(context.SyncStates);
        await context.SaveChangesAsync();

        // Seed users first
        var users = CreateUsers();
        context.Users.AddRange(users);
        await context.SaveChangesAsync();

        // Seed guilds
        var guilds = CreateGuilds();
        context.Guilds.AddRange(guilds);
        await context.SaveChangesAsync();

        // Seed roles
        var roles = CreateRoles();
        context.Roles.AddRange(roles);
        await context.SaveChangesAsync();

        // Seed channels
        var channels = CreateChannels();
        context.Channels.AddRange(channels);
        await context.SaveChangesAsync();

        // Seed guild members
        var members = CreateGuildMembers();
        context.GuildMembers.AddRange(members);
        await context.SaveChangesAsync();

        // Seed messages
        var messages = CreateMessages(users);
        context.Messages.AddRange(messages);
        await context.SaveChangesAsync();

        // Seed sync states
        var syncStates = CreateSyncStates();
        context.SyncStates.AddRange(syncStates);
        await context.SaveChangesAsync();
    }

    private static List<User> CreateUsers()
    {
        return
        [
            CreateUser(TestUserId1, "TestUser1", "Test User One", false),
            CreateUser(TestUserId2, "TestUser2", "Test User Two", false),
            CreateUser(TestUserId3, "AdminUser", "Admin User", false),
            CreateUser(TestBotUserId, "TestBot", "Test Bot", true),
        ];
    }

    private static User CreateUser(Snowflake id, string username, string? globalName, bool isBot)
    {
        var user = new User(id, username, DateTime.UtcNow.AddDays(-30));
        user.Update(username, null, globalName, $"https://cdn.discordapp.com/avatars/{id}/test.png", isBot);
        return user;
    }

    private static List<Guild> CreateGuilds()
    {
        return
        [
            CreateGuild(TestGuildId1, "Test Server Alpha", "A test Discord server for E2E testing"),
            CreateGuild(TestGuildId2, "Test Server Beta", "Another test server with less activity"),
        ];
    }

    private static Guild CreateGuild(Snowflake id, string name, string description)
    {
        var guild = new Guild(id, name, TestUserId3, DateTime.UtcNow.AddDays(-90));
        guild.Update(name, $"https://cdn.discordapp.com/icons/{id}/test.png", description, TestUserId3);
        guild.MarkSynced();
        return guild;
    }

    private static List<Role> CreateRoles()
    {
        return
        [
            CreateRole(TestRoleId1, TestGuildId1, "Admin", 0xFF0000, 10, true),
            CreateRole(TestRoleId2, TestGuildId1, "Moderator", 0x00FF00, 5, true),
            CreateRole(TestRoleId3, TestGuildId1, "Member", 0x0000FF, 1, false),
            CreateRole(new Snowflake(888888888888888884ul), TestGuildId2, "Owner", 0xFFD700, 10, true),
        ];
    }

    private static Role CreateRole(Snowflake id, Snowflake guildId, string name, int color, int position, bool isHoisted)
    {
        var role = new Role(id, guildId, name);
        role.Update(name, color, position, "0", isHoisted, true, false);
        return role;
    }

    private static List<Channel> CreateChannels()
    {
        return
        [
            // Guild 1 channels
            CreateChannel(TestCategoryId1, TestGuildId1, "General", ChannelType.Category, null, 0),
            CreateChannel(TestChannelId1, TestGuildId1, "general", ChannelType.Text, TestCategoryId1, 0, "Welcome to our server!"),
            CreateChannel(TestChannelId2, TestGuildId1, "announcements", ChannelType.Text, TestCategoryId1, 1, "Important announcements"),
            CreateChannel(TestChannelId3, TestGuildId1, "random", ChannelType.Text, TestCategoryId1, 2, "Off-topic discussions"),
            CreateChannel(TestVoiceChannelId, TestGuildId1, "Voice Chat", ChannelType.Voice, TestCategoryId1, 3),
            
            // Guild 2 channels
            CreateChannel(new Snowflake(666666666666666666ul), TestGuildId2, "chat", ChannelType.Text, null, 0, "Main chat channel"),
        ];
    }

    private static Channel CreateChannel(Snowflake id, Snowflake guildId, string name, ChannelType type, Snowflake? parentId, int position, string? topic = null)
    {
        var channel = new Channel(id, guildId, name, type, DateTime.UtcNow.AddDays(-60));
        channel.Update(name, type, topic, position, false, parentId);
        channel.MarkSynced();
        return channel;
    }

    private static List<GuildMember> CreateGuildMembers()
    {
        return
        [
            CreateGuildMember(TestUserId1, TestGuildId1, "Alpha Tester", [TestRoleId3.ToString()]),
            CreateGuildMember(TestUserId2, TestGuildId1, "Beta Tester", [TestRoleId3.ToString(), TestRoleId2.ToString()]),
            CreateGuildMember(TestUserId3, TestGuildId1, null, [TestRoleId1.ToString(), TestRoleId2.ToString(), TestRoleId3.ToString()]),
            CreateGuildMember(TestBotUserId, TestGuildId1, "Helpful Bot", [TestRoleId3.ToString()]),
            CreateGuildMember(TestUserId1, TestGuildId2, null, []),
            CreateGuildMember(TestUserId3, TestGuildId2, "Server Owner", []),
        ];
    }

    private static GuildMember CreateGuildMember(Snowflake userId, Snowflake guildId, string? nickname, List<string> roleIds)
    {
        var member = new GuildMember(userId, guildId);
        member.Update(nickname, DateTime.UtcNow.AddDays(-30), false, roleIds);
        member.MarkSynced();
        return member;
    }

    private static List<Message> CreateMessages(List<User> users)
    {
        var messages = new List<Message>();
        var random = new Random(42); // Fixed seed for reproducibility

        // Generate messages for channel 1 (general)
        var messageContents = new[]
        {
            "Hello everyone! 👋",
            "Welcome to the server!",
            "How is everyone doing today?",
            "Check out this cool feature!",
            "Does anyone know how to fix this?",
            "Thanks for the help!",
            "This is amazing!",
            "I agree with that.",
            "Let me look into it.",
            "Great work team!",
            "Has anyone tried the new update?",
            "I'm having some issues with...",
            "Can someone help me with this?",
            "That's a really good point.",
            "I'll work on it tomorrow.",
        };

        var messageId = 100000000000000000ul;
        var baseTime = DateTime.UtcNow.AddDays(-7);

        // Generate 50 messages in general channel
        for (int i = 0; i < 50; i++)
        {
            var authorIndex = random.Next(users.Count);
            var content = messageContents[random.Next(messageContents.Length)];
            var timestamp = baseTime.AddMinutes(i * 15);
            
            var message = new Message(
                new Snowflake(messageId++),
                TestChannelId1,
                users[authorIndex].Id,
                timestamp
            );
            message.Update(content, content, MessageType.Default, false, false, null, null);
            messages.Add(message);
        }

        // Generate 20 messages in announcements channel
        var announcementContents = new[]
        {
            "📢 Server rules have been updated",
            "🎉 New feature released!",
            "⚠️ Scheduled maintenance tonight",
            "🔔 Important community update",
            "📅 Upcoming event this weekend",
        };

        for (int i = 0; i < 20; i++)
        {
            var content = announcementContents[i % announcementContents.Length];
            var timestamp = baseTime.AddHours(i * 12);
            
            var message = new Message(
                new Snowflake(messageId++),
                TestChannelId2,
                TestUserId3, // Admin user for announcements
                timestamp
            );
            message.Update(content, content, MessageType.Default, i < 5, false, null, null);
            messages.Add(message);
        }

        // Generate 30 messages in random channel
        for (int i = 0; i < 30; i++)
        {
            var authorIndex = random.Next(users.Count);
            var content = $"Random message #{i + 1}: {messageContents[random.Next(messageContents.Length)]}";
            var timestamp = baseTime.AddMinutes(i * 30);
            
            var message = new Message(
                new Snowflake(messageId++),
                TestChannelId3,
                users[authorIndex].Id,
                timestamp
            );
            message.Update(content, content, MessageType.Default, false, false, null, null);
            messages.Add(message);
        }

        return messages;
    }

    private static List<SyncState> CreateSyncStates()
    {
        return
        [
            CreateSyncState("Guild", TestGuildId1.ToString(), SyncStatus.Completed),
            CreateSyncState("Guild", TestGuildId2.ToString(), SyncStatus.Completed),
            CreateSyncState("Channel", TestChannelId1.ToString(), SyncStatus.Completed),
            CreateSyncState("Channel", TestChannelId2.ToString(), SyncStatus.Completed),
            CreateSyncState("Channel", TestChannelId3.ToString(), SyncStatus.InProgress),
        ];
    }

    private static SyncState CreateSyncState(string entityType, string entityId, SyncStatus status)
    {
        var state = new SyncState(entityType, entityId);
        
        switch (status)
        {
            case SyncStatus.InProgress:
                state.StartSync();
                break;
            case SyncStatus.Completed:
                state.StartSync();
                state.CompleteSync();
                break;
            case SyncStatus.Failed:
                state.StartSync();
                state.FailSync("Test error message");
                break;
        }
        
        return state;
    }
}
