using Microsoft.Playwright;
using NaukriProfileUpdater.Services;

namespace NaukriProfileUpdater.Strategies;

public interface IProfileUpdateStrategy
{
    string Name { get; }
    Task UpdateAsync(NaukriProfileService service, IPage page);
}
