using Microsoft.Playwright;
using NaukriProfileUpdater.Services;

namespace NaukriProfileUpdater.Strategies;

public sealed class SkillToggleStrategy(string skillName) : IProfileUpdateStrategy
{
    public string Name => "SkillToggle";

    public Task UpdateAsync(NaukriProfileService service, IPage page) =>
        service.ToggleSkillAsync(page, skillName);
}
