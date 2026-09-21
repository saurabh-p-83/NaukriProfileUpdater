using Microsoft.Playwright;
using NaukriProfileUpdater.Services;

namespace NaukriProfileUpdater.Strategies;

public sealed class ResumeReuploadStrategy(string filePath) : IProfileUpdateStrategy
{
    public string Name => "ResumeReupload";

    public Task UpdateAsync(NaukriProfileService service, IPage page) =>
        service.ReuploadResumeAsync(page, filePath);
}
