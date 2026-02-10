using DiscordDataMirror.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using Reqnroll;
using Xunit;

namespace DiscordDataMirror.E2E.Tests.StepDefinitions;

[Binding]
public class CommonSteps(ScenarioContext scenarioContext)
{
    private IPage Page => scenarioContext.GetPage();
    private string DashboardUrl => scenarioContext.GetDashboardUrl();

    [Given(@"the application is running with test data")]
    public void GivenTheApplicationIsRunningWithTestData()
    {
        // Test data is seeded by the TestWebApplicationFactory
        // This step just confirms the precondition
        Assert.NotNull(TestHooks.AspireFixture);
        Assert.NotEmpty(DashboardUrl);
    }

    [Given(@"I am on the dashboard home page")]
    [When(@"I navigate to the dashboard")]
    public async Task WhenINavigateToTheDashboard()
    {
        await Page.GotoAsync(DashboardUrl);
        await WaitForPageLoad();
    }

    [When(@"I navigate to the guild overview page for guild ""(.*)""")]
    [Given(@"I am on the guild overview page for guild ""(.*)""")]
    public async Task WhenINavigateToTheGuildOverviewPageForGuild(string guildId)
    {
        await Page.GotoAsync($"{DashboardUrl}/guild/{guildId}");
        await WaitForPageLoad();
    }

    [When(@"I navigate to the channel browser page for guild ""(.*)""")]
    [Given(@"I am on the channel browser page for guild ""(.*)""")]
    public async Task WhenINavigateToTheChannelBrowserPageForGuild(string guildId)
    {
        await Page.GotoAsync($"{DashboardUrl}/guild/{guildId}/channels");
        await WaitForPageLoad();
    }

    [When(@"I navigate to the message viewer for channel ""(.*)"" in guild ""(.*)""")]
    [Given(@"I am on the message viewer for channel ""(.*)"" in guild ""(.*)""")]
    public async Task WhenINavigateToTheMessageViewerForChannel(string channelId, string guildId)
    {
        await Page.GotoAsync($"{DashboardUrl}/guild/{guildId}/channel/{channelId}");
        await WaitForPageLoad();
    }

    [When(@"I navigate to the member list page for guild ""(.*)""")]
    [Given(@"I am on the member list page for guild ""(.*)""")]
    public async Task WhenINavigateToTheMemberListPageForGuild(string guildId)
    {
        await Page.GotoAsync($"{DashboardUrl}/guild/{guildId}/members");
        await WaitForPageLoad();
    }

    [When(@"I navigate to the sync status page")]
    [Given(@"I am on the sync status page")]
    public async Task WhenINavigateToTheSyncStatusPage()
    {
        await Page.GotoAsync($"{DashboardUrl}/sync");
        await WaitForPageLoad();
    }

    [When(@"I navigate directly to ""(.*)""")]
    public async Task WhenINavigateDirectlyTo(string path)
    {
        await Page.GotoAsync($"{DashboardUrl}{path}");
        await WaitForPageLoad();
    }

    [When(@"I navigate back")]
    public async Task WhenINavigateBack()
    {
        await Page.GoBackAsync();
        await WaitForPageLoad();
    }

    [Then(@"the page title should contain ""(.*)""")]
    public async Task ThenThePageTitleShouldContain(string expectedTitle)
    {
        var title = await Page.TitleAsync();
        Assert.Contains(expectedTitle, title, StringComparison.OrdinalIgnoreCase);
    }

    [Then(@"the URL should contain ""(.*)""")]
    public void ThenTheUrlShouldContain(string expectedPart)
    {
        Assert.Contains(expectedPart, Page.Url, StringComparison.OrdinalIgnoreCase);
    }

    [Then(@"I should see the page header ""(.*)""")]
    public async Task ThenIShouldSeeThePageHeader(string expectedHeader)
    {
        var header = await Page.Locator("h1.page-title, .page-header h1").TextContentAsync();
        Assert.Contains(expectedHeader, header, StringComparison.OrdinalIgnoreCase);
    }

    [Then(@"I should see the subtitle ""(.*)""")]
    public async Task ThenIShouldSeeTheSubtitle(string expectedSubtitle)
    {
        var subtitle = await Page.Locator("p.page-subtitle, .page-header p").TextContentAsync();
        Assert.Contains(expectedSubtitle, subtitle ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Then(@"I should see the heading ""(.*)""")]
    public async Task ThenIShouldSeeTheHeading(string expectedHeading)
    {
        var headings = await Page.Locator("h5, h6, .mud-typography-h5, .mud-typography-h6").AllTextContentsAsync();
        Assert.Contains(headings, h => h.Contains(expectedHeading, StringComparison.OrdinalIgnoreCase));
    }

    [Then(@"I should see ""(.*)""")]
    public async Task ThenIShouldSee(string expectedText)
    {
        var content = await Page.ContentAsync();
        Assert.Contains(expectedText, content, StringComparison.OrdinalIgnoreCase);
    }

    [Then(@"I should see a stats card showing ""(.*)""")]
    public async Task ThenIShouldSeeAStatsCardShowing(string label)
    {
        var statsCards = await Page.Locator(".stats-card, [class*='StatsCard']").AllAsync();
        if (!statsCards.Any())
        {
            // Try alternate selector for MudBlazor
            var content = await Page.ContentAsync();
            Assert.Contains(label, content, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            var cardTexts = new List<string>();
            foreach (var card in statsCards)
            {
                var text = await card.TextContentAsync();
                cardTexts.Add(text ?? "");
            }
            Assert.Contains(cardTexts, t => t.Contains(label, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Then(@"I should see breadcrumb ""(.*)""")]
    public async Task ThenIShouldSeeBreadcrumb(string breadcrumbText)
    {
        var breadcrumbs = await Page.Locator(".breadcrumbs, .breadcrumb-item, .breadcrumbs-bar").TextContentAsync();
        Assert.Contains(breadcrumbText, breadcrumbs ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [When(@"I click on breadcrumb ""(.*)""")]
    public async Task WhenIClickOnBreadcrumb(string breadcrumbText)
    {
        var link = Page.Locator($".breadcrumbs a, .breadcrumb-item a, .breadcrumbs-bar a").Filter(new() { HasText = breadcrumbText });
        await link.First.ClickAsync();
        await WaitForPageLoad();
    }

    [When(@"I click the ""(.*)"" button")]
    public async Task WhenIClickTheButton(string buttonText)
    {
        var button = Page.Locator("button, a.mud-button, a").Filter(new() { HasText = buttonText });
        await button.First.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(500);
    }

    private async Task WaitForPageLoad()
    {
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        // Wait for Blazor Server SignalR circuit to be established
        // This is indicated by the blazor-reconnect-indicator not being visible
        try
        {
            // Wait up to 5 seconds for interactive mode to be ready
            await Page.WaitForFunctionAsync(@"() => {
                // Check if Blazor has initialized (circuit is connected)
                return window.Blazor && window.Blazor._internal;
            }", null, new() { Timeout = 5000 });
        }
        catch
        {
            // Fallback: just wait a bit if Blazor check fails
        }
        // Additional wait for Blazor to render
        await Task.Delay(500);
    }
}
