using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DiscordDataMirror.E2E.Tests.Infrastructure;

public class AspireFixture : IAsyncLifetime
{
    private DistributedApplication? _app;
    
    public string DashboardUrl { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.DiscordDataMirror_AppHost>();

        _app = await appHost.BuildAsync();
        
        await _app.StartAsync();
        
        // Give resources time to start
        await Task.Delay(TimeSpan.FromSeconds(10));
        
        // Get the dashboard endpoint
        DashboardUrl = _app.GetEndpoint("dashboard", "https")?.ToString() 
            ?? _app.GetEndpoint("dashboard", "http")?.ToString()
            ?? throw new InvalidOperationException("Could not get dashboard URL");
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
