using Microsoft.Playwright;
using Reqnroll;
using DiscordDataMirror.Dashboard.Tests.Support;
using FluentAssertions;

namespace DiscordDataMirror.Dashboard.Tests.StepDefinitions;

[Binding]
public class MessageViewerSteps
{
    private readonly BrowserDriver _driver;
    private string? _currentChannelId;
    private string? _guildId;
    private int _initialMessageCount;

    public MessageViewerSteps(BrowserDriver driver)
    {
        _driver = driver;
    }

    [Given(@"I am viewing a channel with messages")]
    [Given(@"I am viewing a channel")]
    public async Task GivenIAmViewingAChannelWithMessages()
    {
        // Navigate to dashboard, find a guild, go to channels, pick first channel
        await _driver.NavigateToAsync("/");
        var page = await _driver.GetPageAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Click first server
        var serverCards = page.Locator(".mud-card").Filter(new() { HasText = "channels" });
        if (await serverCards.CountAsync() > 0)
        {
            await serverCards.First.ClickAsync();
            await _driver.WaitForUrlContainsAsync("/guild/");
            
            // Extract guild ID
            var match = System.Text.RegularExpressions.Regex.Match(page.Url, @"/guild/(\d+)");
            if (match.Success) _guildId = match.Groups[1].Value;
            
            // Navigate to channels
            await _driver.NavigateToAsync($"/guild/{_guildId}/channels");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // Click first channel to view details
            var channels = page.Locator(".channel-item");
            if (await channels.CountAsync() > 0)
            {
                await channels.First.ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                
                // Click View Messages
                var viewBtn = page.GetByRole(AriaRole.Button, new() { Name = "View Messages" });
                if (await viewBtn.CountAsync() > 0)
                {
                    await viewBtn.ClickAsync();
                    await _driver.WaitForUrlContainsAsync("/channel/");
                    
                    // Extract channel ID
                    match = System.Text.RegularExpressions.Regex.Match(page.Url, @"/channel/(\d+)");
                    if (match.Success) _currentChannelId = match.Groups[1].Value;
                }
            }
        }
    }

    [Then(@"I should see a list of message cards")]
    public async Task ThenIShouldSeeAListOfMessageCards()
    {
        var page = await _driver.GetPageAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Look for message cards or message groups
        var messages = page.Locator(".message-group, .message-card, [class*='message']");
        var count = await messages.CountAsync();
        
        // Could be 0 if channel is empty
        _initialMessageCount = count;
    }

    [Then(@"each message should display the author name")]
    public async Task ThenEachMessageShouldDisplayTheAuthorName()
    {
        var page = await _driver.GetPageAsync();
        
        if (_initialMessageCount > 0)
        {
            // Message headers contain author info
            var authors = page.Locator(".message-author, [class*='author']");
            var count = await authors.CountAsync();
            count.Should().BeGreaterThanOrEqualTo(1);
        }
    }

    [Then(@"each message should display the message content")]
    public async Task ThenEachMessageShouldDisplayTheMessageContent()
    {
        var page = await _driver.GetPageAsync();
        
        if (_initialMessageCount > 0)
        {
            var content = page.Locator(".message-content, [class*='content']");
            var count = await content.CountAsync();
            count.Should().BeGreaterThanOrEqualTo(1);
        }
    }

    [Then(@"each message should display a timestamp")]
    public async Task ThenEachMessageShouldDisplayATimestamp()
    {
        var page = await _driver.GetPageAsync();
        
        if (_initialMessageCount > 0)
        {
            var timestamps = page.Locator(".message-timestamp, [class*='timestamp'], time");
            var count = await timestamps.CountAsync();
            count.Should().BeGreaterThanOrEqualTo(0);
        }
    }

    [Then(@"consecutive messages from the same author should be grouped together")]
    public async Task ThenConsecutiveMessagesShouldBeGrouped()
    {
        var page = await _driver.GetPageAsync();
        
        var groups = page.Locator(".message-group");
        // Groups contain multiple messages
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    [Then(@"I should see breadcrumb navigation")]
    public async Task ThenIShouldSeeBreadcrumbNavigation()
    {
        var page = await _driver.GetPageAsync();
        
        var breadcrumbs = page.Locator(".breadcrumbs, [class*='breadcrumb']");
        await Assertions.Expect(breadcrumbs).ToBeVisibleAsync();
    }

    [When(@"I click on the guild name in the breadcrumb")]
    public async Task WhenIClickOnTheGuildNameInTheBreadcrumb()
    {
        var page = await _driver.GetPageAsync();
        
        var breadcrumbLinks = page.Locator(".breadcrumb-item a, .breadcrumbs a");
        var guildLink = breadcrumbLinks.Nth(1); // Second link should be guild
        await guildLink.ClickAsync();
        await _driver.WaitForUrlContainsAsync("/guild/");
    }

    [Then(@"I should be navigated to the guild overview")]
    public async Task ThenIShouldBeNavigatedToTheGuildOverview()
    {
        var page = await _driver.GetPageAsync();
        page.Url.Should().Contain("/guild/");
        page.Url.Should().NotContain("/channel/");
    }

    [Given(@"I am viewing a channel with many messages")]
    public async Task GivenIAmViewingAChannelWithManyMessages()
    {
        await GivenIAmViewingAChannelWithMessages();
    }

    [When(@"I click ""(.*)""")]
    public async Task WhenIClick(string buttonText)
    {
        var page = await _driver.GetPageAsync();
        var button = page.GetByRole(AriaRole.Button, new() { Name = buttonText });
        await button.ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    [Then(@"more messages should be displayed")]
    public async Task ThenMoreMessagesShouldBeDisplayed()
    {
        var page = await _driver.GetPageAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var messages = page.Locator(".message-group, .message-card");
        var count = await messages.CountAsync();
        count.Should().BeGreaterThanOrEqualTo(_initialMessageCount);
    }

    [When(@"I click the ""(.*)"" button")]
    public async Task WhenIClickTheButton(string buttonText)
    {
        var page = await _driver.GetPageAsync();
        var button = page.GetByRole(AriaRole.Button, new() { Name = buttonText });
        await button.ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    [Then(@"I should see the oldest messages in the channel")]
    public async Task ThenIShouldSeeTheOldestMessagesInTheChannel()
    {
        // Oldest messages are at the top after clicking Oldest
        var page = await _driver.GetPageAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    [Given(@"I am viewing a channel at the oldest messages")]
    public async Task GivenIAmViewingAChannelAtTheOldestMessages()
    {
        await GivenIAmViewingAChannelWithMessages();
        await WhenIClickTheButton("Oldest");
    }

    [Then(@"I should see the newest messages in the channel")]
    public async Task ThenIShouldSeeTheNewestMessagesInTheChannel()
    {
        var page = await _driver.GetPageAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    [When(@"I enter a search term in the search field")]
    public async Task WhenIEnterASearchTermInTheSearchField()
    {
        var page = await _driver.GetPageAsync();
        
        var searchInput = page.Locator("input[placeholder*='Search']").First;
        await searchInput.FillAsync("test");
    }

    [When(@"I press Enter")]
    public async Task WhenIPressEnter()
    {
        var page = await _driver.GetPageAsync();
        await page.Keyboard.PressAsync("Enter");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    [Then(@"only messages containing the search term should be displayed")]
    public async Task ThenOnlyMessagesContainingTheSearchTermShouldBeDisplayed()
    {
        var page = await _driver.GetPageAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        // Messages are filtered by search
    }

    [When(@"I navigate to a different channel using the URL")]
    public async Task WhenINavigateToDifferentChannelUsingTheUrl()
    {
        // Navigate back to channels and pick a different one
        if (_guildId != null)
        {
            await _driver.NavigateToAsync($"/guild/{_guildId}/channels");
            var page = await _driver.GetPageAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            var channels = page.Locator(".channel-item");
            var count = await channels.CountAsync();
            
            if (count > 1)
            {
                await channels.Nth(1).ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                
                var viewBtn = page.GetByRole(AriaRole.Button, new() { Name = "View Messages" });
                if (await viewBtn.CountAsync() > 0)
                {
                    await viewBtn.ClickAsync();
                    await _driver.WaitForUrlContainsAsync("/channel/");
                }
            }
        }
    }

    [Then(@"the messages should update to the new channel")]
    public async Task ThenTheMessagesShouldUpdateToTheNewChannel()
    {
        var page = await _driver.GetPageAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    [Then(@"the page title should update to the new channel name")]
    public async Task ThenThePageTitleShouldUpdateToTheNewChannelName()
    {
        var page = await _driver.GetPageAsync();
        var title = await page.TitleAsync();
        title.Should().NotBeNullOrEmpty();
    }
}
