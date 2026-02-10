using Microsoft.Playwright;
using Reqnroll;

namespace DiscordDataMirror.E2E.Tests.Infrastructure;

[Binding]
public class ScenarioHooks(ScenarioContext scenarioContext)
{
    private const string PageKey = "Page";
    private const string BrowserContextKey = "BrowserContext";

    [BeforeScenario]
    public async Task BeforeScenario()
    {
        var context = await TestHooks.PlaywrightFixture.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        
        scenarioContext[BrowserContextKey] = context;
        scenarioContext[PageKey] = page;
    }

    [AfterScenario]
    public async Task AfterScenario()
    {
        if (scenarioContext.TryGetValue<IBrowserContext>(BrowserContextKey, out var context))
        {
            await context.CloseAsync();
        }
    }
}

public static class ScenarioContextExtensions
{
    public static IPage GetPage(this ScenarioContext context) 
        => context.Get<IPage>("Page");
    
    public static string GetDashboardUrl(this ScenarioContext context)
        => TestHooks.AspireFixture.DashboardUrl;
}
