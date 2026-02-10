using Microsoft.Playwright;
using Reqnroll;

namespace DiscordDataMirror.E2E.Tests.Infrastructure;

[Binding]
public class TestHooks
{
    private static AspireFixture? _aspireFixture;
    private static PlaywrightFixture? _playwrightFixture;
    
    public static AspireFixture AspireFixture => _aspireFixture 
        ?? throw new InvalidOperationException("Aspire fixture not initialized");
    
    public static PlaywrightFixture PlaywrightFixture => _playwrightFixture 
        ?? throw new InvalidOperationException("Playwright fixture not initialized");

    [BeforeTestRun]
    public static async Task BeforeTestRun()
    {
        _aspireFixture = new AspireFixture();
        await _aspireFixture.InitializeAsync();
        
        _playwrightFixture = new PlaywrightFixture();
        await _playwrightFixture.InitializeAsync();
    }

    [AfterTestRun]
    public static async Task AfterTestRun()
    {
        if (_playwrightFixture is not null)
        {
            await _playwrightFixture.DisposeAsync();
        }
        
        if (_aspireFixture is not null)
        {
            await _aspireFixture.DisposeAsync();
        }
    }
}
