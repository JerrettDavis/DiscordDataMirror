using Xunit;

namespace DiscordDataMirror.E2E.Tests.Infrastructure;

/// <summary>
/// Fixture that starts the Dashboard application for E2E testing.
/// Uses WebApplicationFactory with SQLite in-memory instead of full Aspire orchestration.
/// </summary>
public class AspireFixture : IAsyncLifetime
{
    private TestWebApplicationFactory? _factory;
    private HttpClient? _client;

    public string DashboardUrl { get; private set; } = string.Empty;

    public HttpClient Client => _client ?? throw new InvalidOperationException("Client not initialized");

    public async Task InitializeAsync()
    {
        _factory = new TestWebApplicationFactory();

        // Create the client (this also starts the server)
        _client = _factory.CreateClient();

        // The factory's server is automatically started
        DashboardUrl = _client.BaseAddress?.ToString().TrimEnd('/') ?? "http://localhost";

        // Give the server a moment to fully initialize
        await Task.Delay(TimeSpan.FromSeconds(2));
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();

        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }
    }
}
