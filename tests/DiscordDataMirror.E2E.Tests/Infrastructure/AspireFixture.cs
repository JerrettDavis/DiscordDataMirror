using Xunit;

namespace DiscordDataMirror.E2E.Tests.Infrastructure;

/// <summary>
/// Fixture that starts the Dashboard application for E2E testing.
/// Uses a real Kestrel server with SQLite in-memory instead of full Aspire orchestration.
/// </summary>
public class AspireFixture : IAsyncLifetime
{
    private TestWebApplicationFactory? _factory;

    public string DashboardUrl { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        _factory = new TestWebApplicationFactory();

        // Start the real web server
        await _factory.StartAsync();

        DashboardUrl = _factory.BaseUrl;

        // Give the server a moment to fully initialize
        await Task.Delay(TimeSpan.FromSeconds(1));
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }
    }
}
