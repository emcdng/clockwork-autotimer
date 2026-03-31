# Emcoding ClockWork AutoTimer

A .NET console tool that imports worklog entries from a CSV file and submits them to ClockWork / Jira automatically.

## Disclaimer!

<span style="color: red">Warning:
This tool is provided **as-is** and without any warranty of completeness, correctness, reliability, or fitness for a particular purpose.
</span>

The author does **not** assume any responsibility or liability for:
- incomplete or incorrect processing,
- corrupted, lost, or damaged data records,
- operational interruptions,
- or any direct, indirect, incidental, or consequential damages resulting from the use of this software.

Use of this software is entirely at your own risk and under your own responsibility.

By using this software, the user agrees to indemnify and hold harmless the author from and against any claims, liabilities, damages, losses, or legal actions arising out of or related to its use.

---------


## What this project does

The application:

1. Reads a CSV file with worklog entries.
2. Extracts Jira task keys from each description (format: `#DISPO-123`).
3. Resolves Jira Issue IDs via Jira REST API.
4. Authenticates against Atlassian (including Microsoft login + 2FA flow via browser automation).
5. Captures the ClockWork authorization token.
6. Submits each worklog entry to ClockWork.

Logs are written to console and to a file in the `logs/` folder.

---

## Prerequisites

- **.NET SDK 10.0** (target framework is `net10.0`)
- Internet access to:
    - `*.atlassian.net`
    - `app.clockwork.report`
    - `id.atlassian.com`
- Valid Atlassian/Jira user account with permissions to log work
- Jira API token for your user
- Microsoft Playwright browser dependencies installed (first-time setup)

### Install dependencies

```bash
dotnet restore 
dotnet build
```
After build, install Playwright browser runtime:


> **Note for macOS users:**  
> `pwsh` (PowerShell) is required for this command.  
> If PowerShell is not installed on your Mac, install it first by following:  
> https://learn.microsoft.com/en-us/powershell/scripting/install/install-powershell-on-macos?view=powershell-7.6

(Use the correct output path if you build in `Release`.)

```bash
pwsh ./bin/Debug/net10.0/playwright.ps1 install
```


(Use the correct output path if you build in `Release`.)

---

## Configuration (`appsettings.json`)

Before running, update `ClockWorkAutoTimerOptions` in `appsettings.json`.

At minimum, you asked to adjust:

- `UserId`
- `UserEmail`
- `UserPassword`
- `JiraUserApiToken`
- `CompanyDomain` (e.g. `example.com`)

Also required for correct execution:

- `AppId`
- `AppEnvId`
- `Headless`
- `ClockworkDescriptionColumnNumber`
- `ClockworkDateColumnNumber`
- `ClockworkTimeSpentColumnNumber`
- `WaitingForTwoFactorSeconds`

Example template:

```json
{ "ClockWorkAutoTimerOptions": { "UserId": "your-user-id", "UserEmail": "your.email@company.com", "UserPassword": "your-password", "JiraUserApiToken": "your-jira-api-token", "CompanyDomain": "your-company-domain", "AppId": "your-app-id", "AppEnvId": "your-app-env-id", "Headless": false, "ClockworkDescriptionColumnNumber": 3, "ClockworkDateColumnNumber": 2, "ClockworkTimeSpentColumnNumber": 4, "WaitingForTwoFactorSeconds": 25 } }
```


<span style="color: red">Security note: Never commit real credentials/tokens to version control.</span>

---

## CSV requirements

- The file must exist and be passed as the first CLI argument.
- Separator: `;`
- First row is treated as header (processing starts from row 2).
- Description column must include a Jira key like `#DISPO-123`.
- Date format must be: `yyyy-MM-dd`
- Time format must be: `HH:mm`

If Jira keys cannot be resolved, processing stops with an error.

---

## How to run

From the project directory:

```bash
dotnet run "<path-to-csv-file>"
```

Example:

```bash
dotnet run ./test.csv
```

You can also run the built executable and pass the CSV path as the first argument.

---

## Notes

- During login, the app may wait for Microsoft Authenticator confirmation.
- `WaitingForTwoFactorSeconds` controls how long the app waits for 2FA confirmation.
- If entries fail, check log files in `logs/` for details.
