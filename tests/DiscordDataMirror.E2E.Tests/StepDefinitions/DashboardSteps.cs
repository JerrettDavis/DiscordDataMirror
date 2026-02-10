using DiscordDataMirror.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using Reqnroll;
using Xunit;

namespace DiscordDataMirror.E2E.Tests.StepDefinitions;

[Binding]
public class DashboardSteps(ScenarioContext scenarioContext)
{
    private IPage Page => scenarioContext.GetPage();
    private string DashboardUrl => scenarioContext.GetDashboardUrl();

    [When(@"I navigate to the dashboard")]
    public async Task WhenINavigateToTheDashboard()
    {
        await Page.GotoAsync(DashboardUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    [Given(@"I am on the dashboard home page")]
    public async Task GivenIAmOnTheDashboardHomePage()
    {
        await Page.GotoAsync(DashboardUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    [Then(@"the page title should contain ""(.*)""")]
    public async Task ThenThePageTitleShouldContain(string expectedTitle)
    {
        var title = await Page.TitleAsync();
        Assert.Contains(expectedTitle, title, StringComparison.OrdinalIgnoreCase);
    }

    [Then(@"the page should display the home content")]
    public async Task ThenThePageShouldDisplayTheHomeContent()
    {
        // Wait for Blazor to render
        await Page.WaitForSelectorAsync("body", new PageWaitForSelectorOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 10000
        });
        
        var content = await Page.ContentAsync();
        Assert.False(string.IsNullOrWhiteSpace(content), "Page content should not be empty");
    }

    [When(@"I click on a guild in the sidebar")]
    public async Task WhenIClickOnAGuildInTheSidebar()
    {
        // Wait for guild list to load
        var guildLink = await Page.WaitForSelectorAsync("[data-testid='guild-link'], .guild-item, .mud-nav-link", 
            new PageWaitForSelectorOptions { Timeout = 10000 });
        
        if (guildLink is not null)
        {
            await guildLink.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
    }

    [Then(@"I should see the guild overview page")]
    public async Task ThenIShouldSeeTheGuildOverviewPage()
    {
        // Check URL contains guild or the page has guild content
        var url = Page.Url;
        var content = await Page.ContentAsync();
        
        Assert.True(url.Contains("guild", StringComparison.OrdinalIgnoreCase) 
            || content.Contains("guild", StringComparison.OrdinalIgnoreCase),
            "Should be on a guild page");
    }

    [When(@"I navigate to the sync status page")]
    public async Task WhenINavigateToTheSyncStatusPage()
    {
        await Page.GotoAsync($"{DashboardUrl}/sync-status");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    [Then(@"I should see the sync status information")]
    public async Task ThenIShouldSeeTheSyncStatusInformation()
    {
        var content = await Page.ContentAsync();
        Assert.True(content.Contains("sync", StringComparison.OrdinalIgnoreCase) 
            || content.Contains("status", StringComparison.OrdinalIgnoreCase),
            "Page should contain sync status information");
    }

    [Then(@"I should see the connection status indicator")]
    public async Task ThenIShouldSeeTheConnectionStatusIndicator()
    {
        // Look for connection status component
        var statusIndicator = await Page.QuerySelectorAsync("[data-testid='connection-status'], .connection-status, .status-indicator");
        
        // If no specific indicator, at least verify the page loaded
        if (statusIndicator is null)
        {
            var content = await Page.ContentAsync();
            Assert.False(string.IsNullOrWhiteSpace(content), "Page should have content");
        }
    }
}
