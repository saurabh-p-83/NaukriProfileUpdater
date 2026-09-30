using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using NaukriProfileUpdater.Configuration;
using NaukriProfileUpdater.Services;
using NaukriProfileUpdater.Strategies;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddEnvironmentVariables("NAUKRI_")
    .Build();

var options = new AutomationOptions
{
    Automation = new AutomationOptions.AutomationSettings
    {
        BrowserProfilePath =
            configuration["Automation:BrowserProfilePath"]
            ?? "./browser-profile",

        Headless =
            bool.TryParse(configuration["Automation:Headless"], out var headless)
                ? headless
                : false,

        ManualLoginTimeoutMinutes =
            int.TryParse(configuration["Automation:ManualLoginTimeoutMinutes"], out var timeout)
                ? timeout
                : 5
    }
};

var strategyName =
    configuration["ProfileUpdate:Strategy"]
    ?? "SkillToggle";

IProfileUpdateStrategy strategy;

switch (strategyName.Trim().ToLowerInvariant())
{
    case "skilltoggle":
        {
            var skillName =
                configuration["SkillToggle:SkillName"]
                ?? "C#";

            strategy = new SkillToggleStrategy(skillName);
            break;
        }

    case "headlinerefresh":
        strategy = new HeadlineRefreshStrategy();
        break;

    case "resumereupload":
        {
            var resumePath =
                configuration["Resume:FilePath"]
                ?? "./resume.pdf";

            strategy = new ResumeReuploadStrategy(resumePath);
            break;
        }

    default:
        throw new InvalidOperationException(
            $"Unknown ProfileUpdate:Strategy '{strategyName}'. " +
            "Valid values: SkillToggle, HeadlineRefresh, ResumeReupload.");
}
LogMessage.Log(
    "Program",
    "Main",
    "NaukriProfileUpdater",
    $"Using strategy: {strategy.Name}",
    false,
    false);
var diagnostics = new DiagnosticsService();
var browserService = new BrowserSessionService(options, diagnostics);
var naukri = new NaukriProfileService(browserService, diagnostics);

try
{
    await using var session = await browserService.StartAsync();

    var page = session.Page;

    var result = await naukri.EnsureAuthenticatedAsync(page);

    if (!result.Success)
    {
        Console.Error.WriteLine(
            $"[{result.ErrorCode}] {result.Message}");

        return;
    }

    await naukri.OpenProfileAsync(page);

    Console.WriteLine($"Running strategy: {strategy.Name}");

    await strategy.UpdateAsync(naukri, page);

    var updatedDate =
        await naukri.ReadLastUpdatedDateAsync(page);

    if (string.IsNullOrWhiteSpace(updatedDate))
    {
        throw new VerificationException(
            "Profile update completed, but the last updated date " +
            "could not be verified.");
    }

    Console.WriteLine(
        $"Profile last updated: {updatedDate}");

    Console.WriteLine(
        "Profile update cycle completed.");
}
catch (AccessDeniedException ex)
{
    Console.Error.WriteLine(
        $"[AccessDenied] {ex.Message}");

    await diagnostics.CaptureAsync(
        browserService.CurrentPage,
        "AccessDenied");

    Environment.ExitCode = 2;
}
catch (VerificationException ex)
{
    Console.Error.WriteLine(
        $"[VerificationFailure] {ex.Message}");

    await diagnostics.CaptureAsync(
        browserService.CurrentPage,
        "VerificationFailure");

    Environment.ExitCode = 3;
}
catch (TimeoutException ex)
{
    Console.Error.WriteLine(
        $"[Timeout] {ex.Message}");

    await diagnostics.CaptureAsync(
        browserService.CurrentPage,
        "Timeout");

    Environment.ExitCode = 4;
}
catch (SelectorOrDomChangeException ex)
{

    Console.Error.WriteLine(
        $"[SelectorOrDomChange] {ex.Message}");

    await diagnostics.CaptureAsync(
        browserService.CurrentPage,
        "SelectorOrDomChange");

    Environment.ExitCode = 5;
}
catch (PlaywrightException ex)
{
    Console.Error.WriteLine(
        $"[PlaywrightError] {ex.Message}");

    await diagnostics.CaptureAsync(
        browserService.CurrentPage,
        "PlaywrightError");

    Environment.ExitCode = 6;
}
catch (Exception ex)
{
    Console.Error.WriteLine(
        $"[UnhandledError] {ex.GetType().Name}: {ex.Message}");

    await diagnostics.CaptureAsync(
        browserService.CurrentPage,
        "UnhandledError");

    Environment.ExitCode = 1;
}