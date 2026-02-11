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

    [Then(@"I should see a server card for ""(.*)""")]
    public async Task ThenIShouldSeeAServerCardFor(string serverName)
    {
        // Wait for server cards to load
        await Page.WaitForSelectorAsync(".mud-card", new() { Timeout = 10000 });

        var cards = await Page.Locator(".mud-card").AllAsync();
        var cardTexts = new List<string>();
        foreach (var card in cards)
        {
            var text = await card.TextContentAsync();
            cardTexts.Add(text ?? "");
        }

        Assert.Contains(cardTexts, t => t.Contains(serverName, StringComparison.OrdinalIgnoreCase));
    }

    [Then(@"the server card ""(.*)"" should show sync status")]
    public async Task ThenTheServerCardShouldShowSyncStatus(string serverName)
    {
        var card = Page.Locator(".mud-card").Filter(new() { HasText = serverName });
        var cardContent = await card.TextContentAsync();

        // Check for sync-related content (Synced, Never synced, etc.)
        var hasSyncStatus = cardContent?.Contains("Synced", StringComparison.OrdinalIgnoreCase) == true ||
                           cardContent?.Contains("Never", StringComparison.OrdinalIgnoreCase) == true;

        Assert.True(hasSyncStatus, $"Server card '{serverName}' should display sync status");
    }

    [When(@"I click on the server card ""(.*)""")]
    public async Task WhenIClickOnTheServerCard(string serverName)
    {
        // Wait for Blazor Server circuit to be established
        // The circuit creates a WebSocket connection that enables interactivity
        try
        {
            await Page.WaitForFunctionAsync(@"() => {
                // Blazor Server uses a WebSocket connection. Check for circuit state
                const blazorScript = document.querySelector('script[autostart=""false""]');
                return !blazorScript || window._blazorCircuitStarted;
            }", null, new() { Timeout = 5000 });
        }
        catch
        {
            // Fallback wait
        }
        await Task.Delay(2000); // Give Blazor extra time to hydrate components

        var card = Page.Locator(".mud-card").Filter(new() { HasText = serverName });
        await card.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });

        // Try clicking multiple times in case the first click doesn't register
        for (int i = 0; i < 3; i++)
        {
            await card.First.ClickAsync();
            try
            {
                await Page.WaitForURLAsync(url => url.Contains("/guild/"), new() { Timeout = 3000 });
                return; // Success - navigation occurred
            }
            catch
            {
                // Navigation didn't happen, wait a bit and try again
                await Task.Delay(1000);
            }
        }

        // If we got here, navigation never happened
        throw new Exception($"Failed to navigate to guild page after clicking server card '{serverName}'");
    }

    [Then(@"I should be on the guild overview page for ""(.*)""")]
    public async Task ThenIShouldBeOnTheGuildOverviewPageFor(string serverName)
    {
        Assert.Contains("/guild/", Page.Url, StringComparison.OrdinalIgnoreCase);

        var pageContent = await Page.ContentAsync();
        Assert.Contains(serverName, pageContent, StringComparison.OrdinalIgnoreCase);
    }

    [Then(@"I should be on the guild overview page")]
    public void ThenIShouldBeOnTheGuildOverviewPage()
    {
        Assert.Contains("/guild/", Page.Url, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/channels", Page.Url, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/members", Page.Url, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/channel/", Page.Url, StringComparison.OrdinalIgnoreCase);
    }

    [Then(@"I should be on the dashboard home page")]
    public void ThenIShouldBeOnTheDashboardHomePage()
    {
        var path = new Uri(Page.Url).AbsolutePath;
        Assert.True(path == "/" || path == "", "Should be on the home page");
    }
}
