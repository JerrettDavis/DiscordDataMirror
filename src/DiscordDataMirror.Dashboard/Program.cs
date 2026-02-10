using DiscordDataMirror.Application;
using DiscordDataMirror.Application.Configuration;
using DiscordDataMirror.Application.Services;
using DiscordDataMirror.Dashboard.Components;
using DiscordDataMirror.Dashboard.Endpoints;
using DiscordDataMirror.Dashboard.Hubs;
using DiscordDataMirror.Dashboard.Services;
using DiscordDataMirror.Infrastructure;
using DiscordDataMirror.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults
builder.AddServiceDefaults();

// Add PostgreSQL DbContext via Aspire with pooled factory for Blazor Server
builder.AddNpgsqlDbContext<DiscordMirrorDbContext>("discorddatamirror", 
    configureDbContextOptions: options => options.UseNpgsql());

// Also register the factory for Blazor components
builder.Services.AddPooledDbContextFactory<DiscordMirrorDbContext>((sp, options) =>
{
    var connectionString = builder.Configuration.GetConnectionString("discorddatamirror");
    if (!string.IsNullOrEmpty(connectionString))
    {
        options.UseNpgsql(connectionString);
    }
});

// Add application and infrastructure services
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

// Configure attachment options
builder.Services.Configure<AttachmentOptions>(
    builder.Configuration.GetSection(AttachmentOptions.SectionName));

// Add SignalR services
builder.Services.AddSignalR();
builder.Services.AddScoped<SyncHubConnection>();
builder.Services.AddSingleton<ISyncEventPublisher, SignalRSyncEventPublisher>();

// Add MudBlazor services
builder.Services.AddMudServices();

// Add Blazor services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map API endpoints
app.MapAttachmentEndpoints();
app.MapSyncEventEndpoints();

// Map SignalR hub
app.MapHub<SyncHub>("/hubs/sync");

// Map Aspire health endpoints
app.MapDefaultEndpoints();

app.Run();
