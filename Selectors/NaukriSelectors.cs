namespace NaukriProfileUpdater.Selectors;

/// <summary>
/// Keep Naukri-specific selectors in this file.
/// Naukri can change its DOM; update this file rather than spreading selectors
/// throughout the services/strategies.
/// </summary>
public static class NaukriSelectors
{
    public const string LoginUrl = "https://www.naukri.com/nlogin/login";
    public const string ProfileUrl = "https://www.naukri.com/mnjuser/profile";

    // Authentication
    public static readonly string[] LoggedInMarkers =
    [
        "a[href*='mnjuser/profile']",
        "a[href*='mynaukri']",
        "text=My Naukri",
        "[data-ga-track*='profile']"
    ];

    public static readonly string[] LoginMarkers =
    [
        "input[type='password']",
        "text=Login",
        "text=Sign in"
    ];

    // Profile sections/buttons. Multiple alternatives make DOM changes easier to maintain.
    public static readonly string[] SkillsSection =
    [
        "text=Key Skills",
        "text=Skills"
    ];

    public static readonly string[] EditButtons =
    [
        "button:has-text('Edit')",
        "[class*='edit']",
        "span:has-text('Edit')"
    ];

    public static readonly string[] SaveButtons =
    [
        "button:has-text('Save')",
        "button:has-text('SAVE')",
        "[class*='save']"
    ];

    public static readonly string[] AddSkillButtons =
    [
        "button:has-text('Add Skill')",
        "button:has-text('Add skills')",
        "text=Add Skill"
    ];

    public static readonly string[] SkillInputs =
    [
        "input[placeholder*='skill' i]",
        "input[placeholder*='Skill']",
        "input[name*='skill' i]"
    ];

    public static readonly string[] HeadlineInputs =
    [
        "textarea[placeholder*='headline' i]",
        "input[placeholder*='headline' i]",
        "textarea[name*='headline' i]",
        "input[name*='headline' i]"
    ];

    public static readonly string[] ResumeInputs =
    [
        "input[type='file']"
    ];

    public static readonly string[] LastUpdatedMarkers =
    [
        "text=Profile last updated",
        "text=Last updated",
        "[class*='lastUpdated' i]",
        "[class*='last-updated' i]"
    ];
}
