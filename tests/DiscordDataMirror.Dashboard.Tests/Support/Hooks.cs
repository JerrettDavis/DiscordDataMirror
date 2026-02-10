using Microsoft.Playwright;
using Reqnroll;

namespace DiscordDataMirror.Dashboard.Tests.Support;

[Binding]
public class Hooks
{
    private static PlaywrightFixture? _fixture;

    [BeforeTestRun]
    public static async Task BeforeTestRun()
    {
        _fixture = new PlaywrightFixture();
        await _fixture.InitializeAsync();
    }

    [AfterTestRun]
    public static async Task AfterTestRun()
    {
        if (_fixture != null)
        {
            await _fixture.DisposeAsync();
        }
    }

    [BeforeScenario]
    public void BeforeScenario(ScenarioContext scenarioContext)
    {
        var driver = new BrowserDriver(_fixture!);
        scenarioContext.ScenarioContainer.RegisterInstanceAs(driver);
    }

    [AfterScenario]
    public async Task AfterScenario(ScenarioContext scenarioContext)
    {
        var driver = scenarioContext.ScenarioContainer.Resolve<BrowserDriver>();
        await driver.DisposeAsync();
    }
}
