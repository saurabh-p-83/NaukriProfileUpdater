using Microsoft.Playwright;
using NaukriProfileUpdater.Selectors;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NaukriProfileUpdater.Services;

public sealed record OperationResult(bool Success, string ErrorCode, string Message)
{
    public static OperationResult Ok(string message = "OK") =>
        new(true, string.Empty, message);

    public static OperationResult Fail(string code, string message) =>
        new(false, code, message);
}

public sealed class NaukriProfileService
{
    private readonly BrowserSessionService _browser;
    private readonly DiagnosticsService _diagnostics;

    public NaukriProfileService(
        BrowserSessionService browser,
        DiagnosticsService diagnostics)
    {
        _browser = browser;
        _diagnostics = diagnostics;
    }

    public async Task<OperationResult> EnsureAuthenticatedAsync(IPage page)
    {
        try
        {
            await page.GotoAsync(NaukriSelectors.ProfileUrl,
                new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

            if (await IsAccessDeniedAsync(page))
                return await FailAsync(page, "AccessDenied",
                    "Naukri/Akamai returned Access Denied. Stopping without retry.");

            if (await IsAuthenticatedAsync(page))
            {
                Console.WriteLine("Existing Naukri session is authenticated.");
                return OperationResult.Ok();
            }

            Console.WriteLine("Manual login required.");
            Console.WriteLine("Complete username/password, OTP, CAPTCHA, or security verification in the visible browser.");

            await page.GotoAsync(NaukriSelectors.LoginUrl,
                new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

            var deadline = DateTime.UtcNow.AddMinutes(5);

            while (DateTime.UtcNow < deadline)
            {
                if (await IsAccessDeniedAsync(page))
                    return await FailAsync(page, "AccessDenied",
                        "Naukri/Akamai returned Access Denied. Stopping without retry.");

                if (await IsAuthenticatedAsync(page))
                {
                    Console.WriteLine("Manual login detected.");
                    return OperationResult.Ok();
                }

                await page.WaitForTimeoutAsync(1000);
            }

            return await FailAsync(page, "ManualLoginTimeout",
                "Manual login was not detected before the configured timeout.");
        }
        catch (TimeoutException ex)
        {
            return await FailAsync(page, "Timeout",
                $"Naukri operation timed out: {ex.Message}");
        }
        catch (PlaywrightException ex) when (
            ex.Message.Contains("net::", StringComparison.OrdinalIgnoreCase))
        {
            return await FailAsync(page, "NetworkError",
                $"Naukri network/navigation error: {ex.Message}");
        }
        catch (PlaywrightException ex)
        {
            return await FailAsync(page, "SelectorOrDomChange",
                $"Playwright/DOM error: {ex.Message}");
        }
    }

    public async Task OpenProfileAsync(IPage page)
    {
        await page.GotoAsync(NaukriSelectors.ProfileUrl,
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        await page.WaitForTimeoutAsync(1500);

        if (await IsAccessDeniedAsync(page))
            throw new AccessDeniedException("Naukri/Akamai returned Access Denied.");
    }

    public async Task<string?> ReadLastUpdatedDateAsync(IPage page)
    {
        foreach (var selector in NaukriSelectors.LastUpdatedMarkers)
        {
            var locator = page.Locator(selector).First;
            if (await locator.IsVisibleAsync(new LocatorIsVisibleOptions { Timeout = 1500 }))
            {
                var text = (await locator.InnerTextAsync()).Trim();
                if (!string.IsNullOrWhiteSpace(text))
                    return text;
            }
        }

        // Final fallback: inspect visible body text without treating a missing
        // selector as success.
        var body = await page.Locator("body").InnerTextAsync();
        var line = body.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .FirstOrDefault(x =>
                x.Contains("last updated", StringComparison.OrdinalIgnoreCase));

        return line;
    }

    public async Task ToggleSkillAsync(IPage page, string skillName)
    {
        Console.WriteLine($"Updating skill: {skillName}");

        // Remove existing skill
        await OpenSkillsEditorAsync(page);
        
        await RemoveSkillAsync(page, skillName);
        await SaveAsync(page);

        Console.WriteLine($"Skill removal saved: {skillName}");

        // Add the same skill again
        await OpenSkillsEditorAsync(page);
        await AddSkillAsync(page, skillName);
        await SaveAsync(page);

        Console.WriteLine($"Skill added and saved: {skillName}");
    }

    private async Task RemoveSkillAsync(IPage page, string skillName)
    {
        Console.WriteLine($"Removing skill: {skillName}");

        var skillChip = page.Locator($"div.chipsContainer div.chip[title=\"{skillName}\"]").First;

        //var skillChip = page.Locator(
        //    $"div.chipsContainer div.chip[title=\"{skillName}\"]"
        //).First;

        if (!await skillChip.IsVisibleAsync())
        {
            throw new SelectorOrDomChangeException(
                $"Could not find editable skill '{skillName}'.");
        }

        Console.WriteLine($"Found skill: {skillName}");

        var removeButton = skillChip.Locator(
            "a.material-icons.close"
        ).First;

        if (!await removeButton.IsVisibleAsync())
        {
            throw new SelectorOrDomChangeException(
                $"Could not find the remove control for skill '{skillName}'.");
        }

        await removeButton.ClickAsync();

        await page.WaitForTimeoutAsync(500);

        var skillAfterRemove = page.Locator(
            $"div.chipsContainer div.chip[title=\"{skillName}\"]"
        ).First;

        if (await skillAfterRemove.IsVisibleAsync())
        {
            throw new SelectorOrDomChangeException(
                $"Skill '{skillName}' still exists after removal.");
        }

        Console.WriteLine($"Skill removed: {skillName}");
    }

    private async Task AddSkillAsync(IPage page, string skillName)
    {
        Console.WriteLine($"Adding skill: {skillName}");

        var editor = page.Locator(
            "div.lightbox.profileEditDrawer.keySkillsEdit"
        );



        await editor.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 10000
        });

        Console.WriteLine("Key Skills editor opened.");

        // Exact input from the DOM
        var skillInput = editor
            .Locator("#keySkillSugg")
            .First;
        if (!await skillInput.IsVisibleAsync())
        {
            throw new SelectorOrDomChangeException(
                "Key Skills input (#keySkillSugg) was not found or not visible.");
        }

        Console.WriteLine("Key Skills input found.");

        // Enter skill name
        string firstLetter = skillName.Substring(0, 1);
        string remainingLetters = skillName.Substring(1);

        await skillInput.FillAsync(firstLetter);

        await page.WaitForTimeoutAsync(1000);

        await skillInput.PressSequentiallyAsync(remainingLetters, new LocatorPressSequentiallyOptions
        {
            Delay = 200
        });
        Console.WriteLine($"Entered skill: {skillName}");

        // Give Naukri time to load suggestions
        await page.WaitForTimeoutAsync(1000);

        var suggestionContainer = editor
        .Locator("#sugDrp_keySkillSugg")
        .First;

        if (await suggestionContainer.IsVisibleAsync())
        {
            Console.WriteLine("Skill suggestion dropdown is visible.");

            var suggestions = suggestionContainer
                .Locator("li.sugTouple div.Sbtn");

            var suggestionCount = await suggestions.CountAsync();

            Console.WriteLine(
                $"Suggestions found: {suggestionCount}");

            for (var i = 0; i < suggestionCount; i++)
            {
                var suggestion = suggestions.Nth(i);

                var text = (await suggestion.InnerTextAsync()).Trim();

                Console.WriteLine(
                    $"Suggestion [{i}]: {text}");

                if (string.Equals(
                    text,
                    skillName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine(
                        $"Exact suggestion found: {skillName}");

                    await suggestion.ClickAsync();

                    Console.WriteLine(
                        $"Clicked suggestion: {skillName}");

                    break;
                }
            }
        }
        else
        {
            Console.WriteLine(
                "Dynamic skill suggestion dropdown is not visible.");
        }

        //var skillInput = editor.Locator("input[type='text']").First;

        //if (!await skillInput.IsVisibleAsync())
        //{
        //    throw new SelectorOrDomChangeException(
        //        "Key Skills input was not found or not visible.");
        //}

        //Console.WriteLine("Key Skills input found.");

        //await skillInput.FillAsync(skillName);

        //await page.WaitForTimeoutAsync(500);

        var suggestedSkill = editor
            .Locator("div.suggChipsCont div.chip")
            .Filter(new LocatorFilterOptions
            {
                HasText = skillName
            })
            .First;

        if (await suggestedSkill.IsVisibleAsync())
        {
            Console.WriteLine($"Skill suggestion found: {skillName}");

            await suggestedSkill.ClickAsync();

            Console.WriteLine($"Skill added from suggestion: {skillName}");
        }
        else
        {
            Console.WriteLine(
                $"Suggestion not found. Trying Enter for skill: {skillName}");

            await skillInput.PressAsync("Enter");
        }

        var addedSkill = editor
            .Locator($"div.chipsContainer div.chip[title=\"{skillName}\"]")
            .First;

        if (!await addedSkill.IsVisibleAsync())
        {
            throw new SelectorOrDomChangeException(
                $"Skill '{skillName}' was not added to Key Skills.");
        }

        Console.WriteLine($"Skill successfully added: {skillName}");
    }

    private async Task OpenSkillsEditorAsync(IPage page)
    {
        Console.WriteLine("Opening Key Skills editor...");

        // Find the actual Key Skills widget title.
        // This avoids matching the "Key skills" item inside Quick links.
        var keySkillsTitle = page.Locator(
            "span.widgetTitle"
        ).Filter(new LocatorFilterOptions
        {
            HasText = "Key skills"
        }).First;

        if (!await keySkillsTitle.IsVisibleAsync())
        {
            throw new SelectorOrDomChangeException(
                "Could not find the actual Key Skills widget.");
        }

        Console.WriteLine("Actual Key Skills widget found.");

        // Move up to the card containing the Key Skills widget.
        var card = keySkillsTitle.Locator(
            "xpath=ancestor::div[contains(@class,'card')][1]");

        if (!await card.IsVisibleAsync())
        {
            throw new SelectorOrDomChangeException(
                "Could not find the Key Skills card.");
        }

        Console.WriteLine("Key Skills card found.");

        var editIcon = card.Locator("span.edit.icon").First;

        if (!await editIcon.IsVisibleAsync())
        {
            throw new SelectorOrDomChangeException(
                "Could not find the edit icon inside the actual Key Skills card.");
        }

        Console.WriteLine("Key Skills edit icon found.");

        await editIcon.ClickAsync();

        await page.WaitForTimeoutAsync(1000);

        Console.WriteLine("Key Skills editor opened.");
    }
    public async Task RefreshHeadlineAsync(IPage page)
    {
        await ClickFirstVisibleAsync(page, NaukriSelectors.EditButtons);

        var input = await FirstVisibleAsync(page, NaukriSelectors.HeadlineInputs);
        var current = await input.InputValueAsync();

        if (string.IsNullOrWhiteSpace(current))
            throw new VerificationException("Could not read the current resume headline.");

        // Minimal harmless change: preserve the original text and add/remove one
        // trailing space. The saved visible value remains semantically identical.
        var changed = current.EndsWith(" ") ? current.TrimEnd() : current + " ";
        await input.FillAsync(changed);
        await SaveAsync(page);
    }

    public async Task ReuploadResumeAsync(IPage page, string filePath)
    {
        var fullPath = Path.GetFullPath(filePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Configured resume file was not found.", fullPath);

        await ClickFirstVisibleAsync(page, NaukriSelectors.EditButtons);

        var input = await FirstVisibleAsync(page, NaukriSelectors.ResumeInputs);
        await input.SetInputFilesAsync(fullPath);

        await SaveAsync(page);
    }

    private async Task SaveAsync(IPage page)
    {
        Console.WriteLine("Looking for Key Skills Save button...");

        var saveButton = page.Locator("#saveKeySkills").First;

        if (!await saveButton.IsVisibleAsync())
        {
            throw new SelectorOrDomChangeException(
                "Key Skills Save button (#saveKeySkills) was not visible.");
        }

        Console.WriteLine("Key Skills Save button found.");

        await saveButton.ClickAsync();

        // Wait for the editor to close after saving.
        await page.Locator("div.lightbox.profileEditDrawer.keySkillsEdit")
            .WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Hidden,
                Timeout = 10000
            });

        Console.WriteLine("Key Skills saved successfully.");
    }

    private static async Task<bool> IsAuthenticatedAsync(IPage page)
    {
        if (page.Url.Contains("/nlogin/", StringComparison.OrdinalIgnoreCase))
            return false;

        foreach (var selector in NaukriSelectors.LoggedInMarkers)
        {
            try
            {
                if (await page.Locator(selector).First.IsVisibleAsync(
                    new LocatorIsVisibleOptions { Timeout = 1000 }))
                    return true;
            }
            catch { }
        }

        // Profile URL itself is a useful signal, but only after checking that
        // login controls are absent.
        var password = page.Locator("input[type='password']").First;
        return page.Url.Contains("mnjuser/profile", StringComparison.OrdinalIgnoreCase)
               && !await password.IsVisibleAsync(new LocatorIsVisibleOptions { Timeout = 500 });
    }

    private static async Task<bool> IsAccessDeniedAsync(IPage page)
    {
        var title = await page.TitleAsync();
        var body = await page.Locator("body").InnerTextAsync();

        return title.Contains("Access Denied", StringComparison.OrdinalIgnoreCase)
            || body.Contains("Access Denied", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<OperationResult> FailAsync(
        IPage page, string code, string message)
    {
        await _diagnostics.CaptureAsync(page, code);
        return OperationResult.Fail(code, message);
    }

    private static async Task<ILocator> FirstVisibleAsync(
        IPage page, IEnumerable<string> selectors)
    {
        foreach (var selector in selectors)
        {
            var locator = page.Locator(selector).First;
            if (await locator.IsVisibleAsync(new LocatorIsVisibleOptions { Timeout = 1500 }))
                return locator;
        }

        throw new SelectorOrDomChangeException(
            $"None of the configured selectors matched a visible element: {string.Join(", ", selectors)}");
    }

    private static async Task ClickFirstVisibleAsync(
        IPage page, IEnumerable<string> selectors)
    {
        var locator = await FirstVisibleAsync(page, selectors);
        await locator.ClickAsync();
    }

    
}

public sealed class AccessDeniedException(string message) : Exception(message);
public sealed class SelectorOrDomChangeException(string message) : Exception(message);
public sealed class VerificationException(string message) : Exception(message);
