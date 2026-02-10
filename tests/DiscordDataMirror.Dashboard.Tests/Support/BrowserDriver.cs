using Microsoft.Playwright;

namespace DiscordDataMirror.Dashboard.Tests.Support;

public class BrowserDriver : IAsyncDisposable
{
    private readonly PlaywrightFixture _fixture;
    private IBrowserContext? _context;
    private IPage? _page;

    public string BaseUrl { get; set; } = "http://localhost:5207";

    public BrowserDriver(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task<IPage> GetPageAsync()
    {
        if (_page == null)
        {
            _context = await _fixture.Browser.NewContextAsync(new BrowserNewContextOptions
            {
                IgnoreHTTPSErrors = true
            });
            _page = await _context.NewPageAsync();
        }
        return _page;
    }

    public async Task NavigateToAsync(string path)
    {
        var page = await GetPageAsync();
        var url = path.StartsWith("http") ? path : $"{BaseUrl}{path}";
        await page.GotoAsync(url, new PageGotoOptions 
        { 
            WaitUntil = WaitUntilState.NetworkIdle 
        });
    }

    public async Task WaitForUrlContainsAsync(string urlPart, int timeoutMs = 10000)
    {
        var page = await GetPageAsync();
        var start = DateTime.UtcNow;
        while ((DateTime.UtcNow - start).TotalMilliseconds < timeoutMs)
        {
            if (page.Url.Contains(urlPart))
                return;
            await Task.Delay(100);
        }
        throw new TimeoutException($"URL did not contain '{urlPart}' within {timeoutMs}ms. Current URL: {page.Url}");
    }

    public async ValueTask DisposeAsync()
    {
        if (_page != null)
        {
            await _page.CloseAsync();
        }
        if (_context != null)
        {
            await _context.DisposeAsync();
        }
    }
}
