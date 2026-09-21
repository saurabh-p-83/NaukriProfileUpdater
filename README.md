# NaukriProfileUpdater

Simple .NET 8 console application using Microsoft.Playwright. It performs one Naukri profile-update cycle and exits. Scheduling is intentionally outside the application; use Windows Task Scheduler.

## Requirements

- .NET 8 SDK
- Windows
- PowerShell
- Internet access
- A Naukri account
- Microsoft.Playwright Chromium browser

Microsoft.Playwright 1.62.0 is referenced in the project.

## Setup

```powershell
dotnet restore
dotnet build
pwsh bin\Debug\net8.0\playwright.ps1 install chromium
```

The Playwright .NET documentation recommends installing the bundled browsers after building the project.

## First run

Run:

```powershell
dotnet run
```

or:

```powershell
dotnet run -- --run-now
```

Both commands intentionally execute exactly one update cycle and then exit. The `--run-now` argument is accepted for convenient Task Scheduler/manual invocation; there is no scheduler inside the application.

On the first run:

1. A visible Chromium window opens.
2. Naukri is opened.
3. Log in manually.
4. Complete OTP, CAPTCHA, or any security verification manually if Naukri asks.
5. The application detects the authenticated profile.
6. The session is kept in `./browser-profile`.
7. The configured update strategy runs.
8. The application reads the profile's last-updated text.
9. Chromium closes and the process exits.

CAPTCHA, OTP, and security challenges are never automated or bypassed.

## Future runs

The application launches Chromium with the same persistent Playwright profile:

```text
./browser-profile
```

Playwright's persistent context stores browser session data such as cookies and local storage in that directory. Do not use your normal Chrome user-data directory; keep a separate automation profile.

If the session is still valid, login is skipped.

If the session has expired, a visible browser opens and the application waits for you to complete the normal Naukri login/security flow. After successful login, it continues with the profile update.

If manual login is not completed within the configured timeout, the run ends as `ManualLoginTimeout`.

## Configuration

`appsettings.json`:

```json
{
  "Automation": {
    "BrowserProfilePath": "./browser-profile",
    "Headless": false,
    "ManualLoginTimeoutMinutes": 5
  },
  "ProfileUpdate": {
    "Strategy": "SkillToggle"
  },
  "SkillToggle": {
    "SkillName": "C#"
  },
  "Resume": {
    "FilePath": "./resume.pdf"
  }
}
```

Supported strategies:

- `SkillToggle`: remove configured skill, save, add the exact same skill, save.
- `HeadlineRefresh`: read headline and make a minimal harmless whitespace change, then save.
- `ResumeReupload`: upload the configured resume and save.

No Naukri password is stored in configuration.

## Access denied and diagnostics

If Naukri/Akamai shows `Access Denied`, the application classifies it as:

```text
AccessDenied
```

It stops without repeatedly retrying or attempting to bypass the security mechanism.

Other failures are classified separately, including:

- `ManualLoginTimeout`
- `Timeout`
- `NetworkError`
- `SelectorOrDomChange`
- `VerificationFailure` / verification exceptions
- `AccessDenied`

For failures, diagnostics are captured before browser shutdown under:

```text
./diagnostics/{timestamp}/
```

The directory contains a screenshot and HTML snapshot where possible.

## Windows Task Scheduler

Because this application has no internal scheduler, create the schedule in Windows Task Scheduler.

Recommended approach:

1. Build/publish the application.
2. Open **Task Scheduler**.
3. Create a task.
4. Add the desired trigger, for example daily.
5. For **Action**, use either:
   - Program: `dotnet`
   - Arguments: `run --project "C:\Path\NaukriProfileUpdater" -- --run-now`
   - Start in: `C:\Path\NaukriProfileUpdater`

   For a deployed application, prefer:
   - Program: `C:\Path\NaukriProfileUpdater\NaukriProfileUpdater.exe`
   - Arguments: `--run-now`
   - Start in: `C:\Path\NaukriProfileUpdater`

Important: `Headless` is false by default because expired authentication requires visible manual interaction. Windows Task Scheduler must therefore run the task in an interactive user session if you want to complete login/OTP/CAPTCHA manually.

## Selector maintenance

Naukri's UI/DOM can change. All Naukri selectors are centralized in:

```text
Selectors/NaukriSelectors.cs
```

If Naukri changes a button, field, or section, update that file instead of scattering selectors through the project.

The exact live Naukri DOM could not be verified from this development environment, so selectors are intentionally isolated and use small fallback sets. Validate the selectors against your current Naukri account before putting the task on a recurring schedule.
