using Microsoft.Playwright;
using NaukriProfileUpdater.Configuration;

namespace NaukriProfileUpdater.Services;

public sealed class BrowserSession : IAsyncDisposable
{
    public BrowserSession(IPlaywright playwright, IBrowserContext context, IPage page)
    {
        Playwright = playwright;
        Context = context;
        Page = page;
    }

    public IPlaywright Playwright { get; }
    public IBrowserContext Context { get; }
    public IPage Page { get; }

    public async ValueTask DisposeAsync()
    {
        await Context.CloseAsync();
        Playwright.Dispose();
    }
}

public sealed class BrowserSessionService
{
    private readonly AutomationOptions _options;
    private readonly DiagnosticsService _diagnostics;

    public BrowserSessionService(
        AutomationOptions options,
        DiagnosticsService diagnostics)
    {
        _options = options;
        _diagnostics = diagnostics;
    }

    public IPage? CurrentPage { get; private set; }

    public async Task<BrowserSession> StartAsync()
    {
        var profilePath = Path.GetFullPath(_options.Automation.BrowserProfilePath);
        Directory.CreateDirectory(profilePath);

        var playwright = await Playwright.CreateAsync();

        // Persistent context stores cookies/local storage in browser-profile.
        // Do not use the user's normal Chrome profile.
        var context = await playwright.Chromium.LaunchPersistentContextAsync(
            profilePath,
            new BrowserTypeLaunchPersistentContextOptions
            {
                Headless = _options.Automation.Headless,
                AcceptDownloads = true,
                ViewportSize = new ViewportSize { Width = 1440, Height = 1000 }
            });

        var page = context.Pages.FirstOrDefault() ?? await context.NewPageAsync();
        CurrentPage = page;

        page.SetDefaultTimeout(15_000);
        page.SetDefaultNavigationTimeout(30_000);

        return new BrowserSession(playwright, context, page);
    }
}
