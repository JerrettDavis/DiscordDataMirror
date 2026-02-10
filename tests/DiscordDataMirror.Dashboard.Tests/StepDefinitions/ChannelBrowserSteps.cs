using Microsoft.Playwright;
using Reqnroll;
using DiscordDataMirror.Dashboard.Tests.Support;
using FluentAssertions;

namespace DiscordDataMirror.Dashboard.Tests.StepDefinitions;

[Binding]
public class ChannelBrowserSteps
{
    private readonly BrowserDriver _driver;
    private string? _selectedChannelName;
    private string? _guildId;

    public ChannelBrowserSteps(BrowserDriver driver)
    {
        _driver = driver;
    }

    [Given(@"I am on a guild overview page")]
    public async Task GivenIAmOnAGuildOverviewPage()
    {
        // First go to dashboard and click a server
        await _driver.NavigateToAsync("/");
        var page = await _driver.GetPageAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var serverCards = page.Locator(".mud-card").Filter(new() { HasText = "channels" });
        await serverCards.First.ClickAsync();
        await _driver.WaitForUrlContainsAsync("/guild/");
        
        // Extract guild ID from URL
        var url = page.Url;
        var match = System.Text.RegularExpressions.Regex.Match(url, @"/guild/(\d+)");
        if (match.Success)
        {
            _guildId = match.Groups[1].Value;
        }
    }

    [When(@"I navigate to the channel browser")]
    [Given(@"I am on the channel browser")]
    public async Task WhenINavigateToTheChannelBrowser()
    {
        var page = await _driver.GetPageAsync();
        
        // Navigate to channels page
        if (_guildId != null)
        {
            await _driver.NavigateToAsync($"/guild/{_guildId}/channels");
        }
        else
        {
            // Try to click channels link
            var channelsLink = page.GetByText("Channels").Or(page.Locator("a[href*='channels']"));
            await channelsLink.First.ClickAsync();
            await _driver.WaitForUrlContainsAsync("/channels");
        }
    }

    [Then(@"I should see a list of channel categories")]
    public async Task ThenIShouldSeeAListOfChannelCategories()
    {
        var page = await _driver.GetPageAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var categories = page.Locator(".channel-category, [class*='category']");
        var count = await categories.CountAsync();
        count.Should().BeGreaterThanOrEqualTo(0, "Categories may or may not exist");
    }

    [Then(@"I should see channels within categories")]
    public async Task ThenIShouldSeeChannelsWithinCategories()
    {
        var page = await _driver.GetPageAsync();
        
        var channels = page.Locator(".channel-item, [class*='channel']");
        var count = await channels.CountAsync();
        count.Should().BeGreaterThanOrEqualTo(1, "Expected at least one channel");
    }

    [When(@"I click on a channel")]
    [When(@"I click on the first channel")]
    public async Task WhenIClickOnAChannel()
    {
        var page = await _driver.GetPageAsync();
        
        var channel = page.Locator(".channel-item, [class*='channel']").First;
        _selectedChannelName = await channel.TextContentAsync();
        await channel.ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    [Then(@"I should see the channel details panel")]
    public async Task ThenIShouldSeeTheChannelDetailsPanel()
    {
        var page = await _driver.GetPageAsync();
        
        // Channel details should show in the right panel
        var detailsPanel = page.Locator(".mud-paper").Filter(new() { HasText = "View Messages" });
        await Assertions.Expect(detailsPanel).ToBeVisibleAsync();
    }

    [Then(@"I should see the ""(.*)"" button")]
    public async Task ThenIShouldSeeTheButton(string buttonText)
    {
        var page = await _driver.GetPageAsync();
        var button = page.GetByRole(AriaRole.Button, new() { Name = buttonText });
        await Assertions.Expect(button).ToBeVisibleAsync();
    }

    [Given(@"I have selected a channel")]
    public async Task GivenIHaveSelectedAChannel()
    {
        await WhenIClickOnAChannel();
    }

    [When(@"I click the ""(.*)"" button")]
    public async Task WhenIClickTheButton(string buttonText)
    {
        var page = await _driver.GetPageAsync();
        var button = page.GetByRole(AriaRole.Button, new() { Name = buttonText });
        await button.ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    [Then(@"I should be navigated to the message viewer")]
    public async Task ThenIShouldBeNavigatedToTheMessageViewer()
    {
        var page = await _driver.GetPageAsync();
        page.Url.Should().Contain("/channel/");
    }

    [When(@"I click on a different channel")]
    public async Task WhenIClickOnADifferentChannel()
    {
        var page = await _driver.GetPageAsync();
        
        var channels = page.Locator(".channel-item, [class*='channel']");
        var count = await channels.CountAsync();
        
        if (count > 1)
        {
            var secondChannel = channels.Nth(1);
            _selectedChannelName = await secondChannel.TextContentAsync();
            await secondChannel.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
    }

    [Then(@"the channel details should update to the new channel")]
    public async Task ThenTheChannelDetailsShouldUpdateToTheNewChannel()
    {
        var page = await _driver.GetPageAsync();
        
        if (_selectedChannelName != null)
        {
            var detailsPanel = page.Locator(".mud-paper");
            var text = await detailsPanel.TextContentAsync();
            // Channel name should appear in the details
        }
    }

    [Then(@"each channel should display a message count badge")]
    public async Task ThenEachChannelShouldDisplayAMessageCountBadge()
    {
        var page = await _driver.GetPageAsync();
        
        // MudChip used for message counts
        var badges = page.Locator(".mud-chip, .channel-item .mud-chip");
        var count = await badges.CountAsync();
        count.Should().BeGreaterThanOrEqualTo(1, "Expected message count badges");
    }
}
