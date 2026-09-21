using Microsoft.Playwright;

namespace NaukriProfileUpdater.Services;

public sealed class DiagnosticsService
{
    public async Task CaptureAsync(IPage? page, string reason)
    {
        if (page is null)
            return;

        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            var directory = Path.Combine(
                AppContext.BaseDirectory,
                "diagnostics",
                timestamp);

            Directory.CreateDirectory(directory);

            var safeReason = string.Concat(reason.Select(c =>
                Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));

            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(directory, $"{safeReason}.png"),
                FullPage = true
            });

            var html = await page.ContentAsync();
            await File.WriteAllTextAsync(
                Path.Combine(directory, $"{safeReason}.html"),
                html);

            await File.WriteAllTextAsync(
                Path.Combine(directory, $"{safeReason}.txt"),
                $"URL: {page.Url}{Environment.NewLine}" +
                $"Title: {await page.TitleAsync()}{Environment.NewLine}" +
                $"Reason: {reason}{Environment.NewLine}");
        }
        catch
        {
            // Diagnostics must never hide the original failure.
        }
    }
}
