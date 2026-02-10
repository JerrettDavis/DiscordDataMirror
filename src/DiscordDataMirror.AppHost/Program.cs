var builder = DistributedApplication.CreateBuilder(args);

// Add Docker Compose environment for container deployment
var dockerCompose = builder.AddDockerComposeEnvironment("docker-compose");

// PostgreSQL database with persistent volume
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .WithDataVolume("discorddatamirror-postgres-data")
    .WithLifetime(ContainerLifetime.Persistent);

var database = postgres.AddDatabase("discorddatamirror");

// Discord bot token as a parameter (will be resolved at deploy time)
var discordToken = builder.AddParameter("discord-token", secret: true);

// Blazor Dashboard (declared first so Bot can reference it)
var dashboard = builder.AddProject<Projects.DiscordDataMirror_Dashboard>("dashboard")
    .WithReference(database)
    .WaitFor(database)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health");

// Discord Bot Worker Service
var bot = builder.AddProject<Projects.DiscordDataMirror_Bot>("bot")
    .WithReference(database)
    .WithReference(dashboard)  // Reference dashboard for SignalR event publishing
    .WaitFor(database)
    .WaitFor(dashboard)
    .WithEnvironment("Discord__Token", discordToken);

builder.Build().Run();
