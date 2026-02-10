using System.Net;
using DiscordDataMirror.Application.Configuration;
using DiscordDataMirror.Application.Services;
using DiscordDataMirror.Domain.Repositories;
using DiscordDataMirror.Infrastructure.Persistence;
using DiscordDataMirror.Infrastructure.Persistence.Repositories;
using DiscordDataMirror.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace DiscordDataMirror.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        // DbContext with factory for Blazor Server concurrency support
        services.AddDbContextFactory<DiscordMirrorDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddDbContext<DiscordMirrorDbContext>(options =>
            options.UseNpgsql(connectionString));
        
        return services.AddInfrastructureCore();
    }
    
    /// <summary>
    /// For Aspire integration - uses pre-configured DbContext.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        return services.AddInfrastructureCore();
    }
    
    private static IServiceCollection AddInfrastructureCore(this IServiceCollection services)
    {
        // Unit of Work
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<DiscordMirrorDbContext>());
        
        // Repositories
        services.AddScoped(typeof(IRepository<,>), typeof(GenericRepository<,>));
        services.AddScoped<IGuildRepository, GuildRepository>();
        services.AddScoped<IChannelRepository, ChannelRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IGuildMemberRepository, GuildMemberRepository>();
        services.AddScoped<IReactionRepository, ReactionRepository>();
        services.AddScoped<IThreadRepository, ThreadRepository>();
        services.AddScoped<IAttachmentRepository, AttachmentRepository>();
        services.AddScoped<IEmbedRepository, EmbedRepository>();
        services.AddScoped<ISyncStateRepository, SyncStateRepository>();
        services.AddScoped<IUserMapRepository, UserMapRepository>();
        
        // Sync Services
        services.AddScoped<IGuildSyncService, GuildSyncService>();
        services.AddScoped<IChannelSyncService, ChannelSyncService>();
        services.AddScoped<IRoleSyncService, RoleSyncService>();
        services.AddScoped<IUserSyncService, UserSyncService>();
        services.AddScoped<IMessageSyncService, MessageSyncService>();
        services.AddScoped<IReactionSyncService, ReactionSyncService>();
        
        // Attachment Services
        services.AddScoped<IAttachmentStorageService, LocalAttachmentStorageService>();
        services.AddScoped<IAttachmentCleanupService, AttachmentCleanupService>();
        
        // Configure HttpClient for Discord CDN downloads
        services.AddHttpClient("DiscordCdn", client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "DiscordDataMirror/1.0");
            client.Timeout = TimeSpan.FromMinutes(5);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        })
        .AddPolicyHandler(GetRetryPolicy());
        
        return services;
    }
    
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(3, retryAttempt => 
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000)));
    }
}
