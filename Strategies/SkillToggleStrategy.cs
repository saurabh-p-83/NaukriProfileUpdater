using Microsoft.Playwright;
using NaukriProfileUpdater.Services;

namespace NaukriProfileUpdater.Strategies;

public sealed class SkillToggleStrategy : IProfileUpdateStrategy
{
    private readonly string _skillName;

    public SkillToggleStrategy(string skillName)
    {
        _skillName = skillName;
        LogMessage.Log(
            "SkillToggleStrategy",
            "UpdateAsync",
            "NaukriProfileService.ToggleSkillAsync",
            $"Skill Name: {_skillName}",
            false,
            false);
    }

    public string Name => "SkillToggle";

    public Task UpdateAsync(NaukriProfileService service, IPage page) =>
        service.ToggleSkillAsync(page, _skillName);
}
