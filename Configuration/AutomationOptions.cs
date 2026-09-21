namespace NaukriProfileUpdater.Configuration;

public sealed class AutomationOptions
{
    public AutomationOptions() => Automation = new();

    public AutomationSettings Automation { get; set; }

    public sealed class AutomationSettings
    {
        public string BrowserProfilePath { get; set; } = "./browser-profile";
        public bool Headless { get; set; } = false;
        public int ManualLoginTimeoutMinutes { get; set; } = 5;
    }
}
