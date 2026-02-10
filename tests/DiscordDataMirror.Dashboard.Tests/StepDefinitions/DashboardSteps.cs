using Microsoft.Playwright;
using Reqnroll;
using DiscordDataMirror.Dashboard.Tests.Support;
using FluentAssertions;

namespace DiscordDataMirror.Dashboard.Tests.StepDefinitions;

[Binding]
public class DashboardSteps
{
    private readonly BrowserDriver _driver;

    public DashboardSteps(BrowserDriver driver)
    {
        _driver = driver;
    }

    [Then(@"I should see statistics cards for Servers, Channels, Messages, and Users")]
    public async Task ThenIShouldSeeStatisticsCards()
    {
        var page = await _driver.GetPageAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Look for stats cards - MudBlazor components
        var statsCards = page.Locator(".mud-card, [class*='stats-card'], .stats-card");
        var count = await statsCards.CountAsync();
        count.Should().BeGreaterThanOrEqualTo(4, "Expected at least 4 stats cards");
    }

    [Then(@"I should see a ""(.*)"" section")]
    public async Task ThenIShouldSeeASection(string sectionName)
    {
        var page = await _driver.GetPageAsync();
        var section = page.GetByText(sectionName);
        await Assertions.Expect(section).ToBeVisibleAsync();
    }

    [Then(@"I should see at least one server card")]
    public async Task ThenIShouldSeeAtLeastOneServerCard()
    {
        var page = await _driver.GetPageAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Server cards are MudCards
        var cards = page.Locator(".mud-card");
        var count = await cards.CountAsync();
        count.Should().BeGreaterThanOrEqualTo(1, "Expected at least one server card");
    }

    [When(@"I click on a server card")]
    public async Task WhenIClickOnAServerCard()
    {
        var page = await _driver.GetPageAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Click the first server card (after stats cards)
        var serverCards = page.Locator(".mud-card").Filter(new() { HasText = "channels" });
        var firstServer = serverCards.First;
        await firstServer.ClickAsync();
        
        // Wait for navigation
        await _driver.WaitForUrlContainsAsync("/guild/");
    }

    [Then(@"I should be navigated to the guild overview page")]
    public async Task ThenIShouldBeNavigatedToTheGuildOverviewPage()
    {
        var page = await _driver.GetPageAsync();
        page.Url.Should().Contain("/guild/");
    }
}
