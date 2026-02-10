using DiscordDataMirror.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using Reqnroll;
using Xunit;

namespace DiscordDataMirror.E2E.Tests.StepDefinitions;

[Binding]
public class GuildOverviewSteps(ScenarioContext scenarioContext)
{
    private IPage Page => scenarioContext.GetPage();
    private string DashboardUrl => scenarioContext.GetDashboardUrl();

    [Then(@"I should see the guild name ""(.*)""")]
    public async Task ThenIShouldSeeTheGuildName(string guildName)
    {
        var header = await Page.Locator("h1.page-title, .page-header h1, .page-title").First.TextContentAsync();
        Assert.Contains(guildName, header ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Then(@"I should see a ""(.*)"" button for channels")]
    [Then(@"I should see a ""(.*)"" button for members")]
    public async Task ThenIShouldSeeAViewAllButton(string buttonText)
    {
        var button = Page.Locator($"a, button").Filter(new() { HasText = buttonText });
        var count = await button.CountAsync();
        Assert.True(count > 0, $"Should see '{buttonText}' button");
    }

    [Then(@"I should see channel ""(.*)"" in the list")]
    public async Task ThenIShouldSeeChannelInTheList(string channelName)
    {
        var content = await Page.ContentAsync();
        Assert.Contains(channelName, content, StringComparison.OrdinalIgnoreCase);
    }

    [Then(@"I should see role ""(.*)"" in the roles section")]
    public async Task ThenIShouldSeeRoleInTheRolesSection(string roleName)
    {
        // Look for role chips
        var roleChips = await Page.Locator(".mud-chip").AllAsync();
        var roleTexts = new List<string>();
        foreach (var chip in roleChips)
        {
            var text = await chip.TextContentAsync();
            roleTexts.Add(text ?? "");
        }

        Assert.Contains(roleTexts, t => t.Contains(roleName, StringComparison.OrdinalIgnoreCase));
    }

    [When(@"I click on channel ""(.*)""")]
    public async Task WhenIClickOnChannel(string channelName)
    {
        var channelItem = Page.Locator(".channel-item, [class*='channel']").Filter(new() { HasText = channelName });
        await channelItem.First.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(500);
    }

    [When(@"I click the ""(.*)"" button for channels")]
    public async Task WhenIClickTheViewAllButtonForChannels(string buttonText)
    {
        // Find the button near the Channels heading
        var channelsSection = Page.Locator("text=Channels").Locator("../..");
        var button = channelsSection.Locator($"a, button").Filter(new() { HasText = buttonText });

        if (await button.CountAsync() > 0)
        {
            await button.First.ClickAsync();
        }
        else
        {
            // Fallback: click any View All button that leads to channels
            var viewAllButton = Page.Locator($"a[href*='/channels'], button").Filter(new() { HasText = buttonText });
            await viewAllButton.First.ClickAsync();
        }

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(500);
    }

    [When(@"I click the ""(.*)"" button for members")]
    public async Task WhenIClickTheViewAllButtonForMembers(string buttonText)
    {
        // Find the button near the Members heading
        var membersSection = Page.Locator("text=Members").Locator("../..");
        var button = membersSection.Locator($"a, button").Filter(new() { HasText = buttonText });

        if (await button.CountAsync() > 0)
        {
            await button.First.ClickAsync();
        }
        else
        {
            // Fallback: click any View All button that leads to members
            var viewAllButton = Page.Locator($"a[href*='/members'], button").Filter(new() { HasText = buttonText });
            await viewAllButton.First.ClickAsync();
        }

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(500);
    }
}
