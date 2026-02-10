var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL database - use same volume as clawd version for shared data
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .WithDataVolume("discorddatamirror-postgres-data");

var database = postgres.AddDatabase("discorddatamirror");

// Blazor Dashboard (declared first so Bot can reference it)
var dashboard = builder.AddProject<Projects.DiscordDataMirror_Dashboard>("dashboard")
    .WithReference(database)
    .WaitFor(database)
    .WithExternalHttpEndpoints();

// Discord Bot Worker Service
var bot = builder.AddProject<Projects.DiscordDataMirror_Bot>("bot")
    .WithReference(database)
    .WithReference(dashboard)  // Reference dashboard for SignalR event publishing
    .WaitFor(database)
    .WaitFor(dashboard)
    .WithEnvironment("Discord__Token", builder.Configuration["Discord:Token"] ?? "");

builder.Build().Run();
