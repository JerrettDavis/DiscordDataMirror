using Microsoft.Playwright;
using Reqnroll;
using DiscordDataMirror.Dashboard.Tests.Support;
using FluentAssertions;

namespace DiscordDataMirror.Dashboard.Tests.StepDefinitions;

[Binding]
public class CommonSteps
{
    private readonly BrowserDriver _driver;

    public CommonSteps(BrowserDriver driver)
    {
        _driver = driver;
    }

    [Given(@"the dashboard application is running")]
    public void GivenTheDashboardApplicationIsRunning()
    {
        // Assumed - tests expect the app to be running
    }

    [When(@"I navigate to the dashboard")]
    [Given(@"I am on the dashboard")]
    public async Task WhenINavigateToTheDashboard()
    {
        await _driver.NavigateToAsync("/");
    }

    [Then(@"the URL should contain ""(.*)""")]
    public async Task ThenTheUrlShouldContain(string urlPart)
    {
        var page = await _driver.GetPageAsync();
        page.Url.Should().Contain(urlPart);
    }

    [Then(@"I should see the main navigation elements")]
    public async Task ThenIShouldSeeTheMainNavigationElements()
    {
        var page = await _driver.GetPageAsync();
        // MudBlazor uses navigation drawer
        var navExists = await page.Locator("nav, .mud-nav-item, a[href]").CountAsync() > 0;
        navExists.Should().BeTrue("Expected navigation elements to be present");
    }
}
