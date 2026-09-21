using Microsoft.Playwright;
using NaukriProfileUpdater.Services;

namespace NaukriProfileUpdater.Strategies;

public sealed class HeadlineRefreshStrategy : IProfileUpdateStrategy
{
    public string Name => "HeadlineRefresh";

    public Task UpdateAsync(NaukriProfileService service, IPage page) =>
        service.RefreshHeadlineAsync(page);
}
