# SpecFlow API Test Suite

This directory contains the SpecFlow-based API test framework that expresses end-to-end API checks using Gherkin scenarios and step definitions.

## Project layout

- `Features/` – Gherkin feature files describing behaviour in plain language.
- `Steps/` – Step definition bindings that translate Gherkin steps into executable C# code.
- `Hooks/` – Scenario lifecycle hooks that initialise shared services such as `HttpClient`.
- `Support/` – Test context helpers used to share state between steps.

## Prerequisites

- .NET 8 SDK
- Access to the target API endpoint (defaults to the public `http://httpbin.org` echo service for demonstration purposes).

## Running the tests

1. Restore dependencies and generate SpecFlow bindings (automatically executed by `dotnet test`):

   ```bash
   dotnet restore SpecFlowTests/SpecFlowTests.csproj
   ```

2. Execute the SpecFlow suite, optionally overriding the API base URL:

   ```bash
   API_BASE_URL=http://httpbin.org dotnet test SpecFlowTests/SpecFlowTests.csproj
   ```

   Set `API_BASE_URL` (or `ApiConfiguration:BaseUrl` in `appsettings.json`) to point at your environment under test. Additional configuration values can be supplied via `appsettings.{Environment}.json` files or environment variables. Use `TEST_ENVIRONMENT=QA` to load `appsettings.QA.json`, etc.

### Python helper

If you prefer Python, the repository includes `run-all-tests.py`, which wraps the full workflow (dotnet test + LivingDoc):

```bash
python run-all-tests.py --base-url https://vhapistg.vaxcare.com --open-report
```

Flags:
- `--base-url` (default `https://vhapistg.vaxcare.com`)
- `--environment` sets `TEST_ENVIRONMENT`
- `--configuration` / `--framework` override build output paths
- `--no-report` skips LivingDoc generation
- `--open-report` launches the HTML after generation

## Customising scenarios

- Add new `.feature` files under `Features/` and SpecFlow will generate strongly typed glue code on build.
- Use the existing step definitions to send `GET`, `POST`, or `PUT` requests and assert on headers or JSON payloads. Extend `Steps/ApiStepDefinitions.cs` for additional verbs or specialised assertions.
- Common setup logic (headers, authentication tokens, etc.) belongs in `Hooks/Hook.cs` where the shared `HttpClient` is configured per scenario.

## Living documentation

The project references the SpecFlow LivingDoc plugin. After running the tests you can generate rich HTML documentation:

### One-liner: run suite & refresh LivingDoc

Install the LivingDoc CLI once:

```bash
dotnet tool install --global SpecFlow.Plus.LivingDoc.CLI
```

Then run the entire suite, regenerate the report, and open it:

```bash
API_BASE_URL=https://vhapistg.vaxcare.com dotnet test SpecFlowTests/SpecFlowTests.csproj --logger:"trx" \
  && livingdoc test-assembly SpecFlowTests/bin/Debug/net8.0/SpecFlowTests.dll \
      -t SpecFlowTests/bin/Debug/net8.0/TestExecution.json \
      --output SpecFlowTests/TestResults/LivingDoc.html \
  && open SpecFlowTests/TestResults/LivingDoc.html
```

PowerShell equivalent on Windows (add `%USERPROFILE%\.dotnet\tools` to PATH first so `livingdoc` resolves):

```powershell
# current session
$env:PATH = "$env:USERPROFILE\.dotnet\tools;$env:PATH"

# persist for future sessions (adds to user PATH)
setx PATH "$env:USERPROFILE\.dotnet\tools;$env:PATH"
```

```powershell
dotnet tool install --global SpecFlow.Plus.LivingDoc.CLI
$env:API_BASE_URL = "https://vhapistg.vaxcare.com"
dotnet test SpecFlowTests/SpecFlowTests.csproj --logger "trx"
livingdoc test-assembly SpecFlowTests/bin/Debug/net8.0/SpecFlowTests.dll `
    -t SpecFlowTests/bin/Debug/net8.0/TestExecution.json `
    --output SpecFlowTests/TestResults/LivingDoc.html
Start-Process "SpecFlowTests\TestResults\LivingDoc.html"
```

Verify the tools folder and `livingdoc.exe`:

```powershell
# Locate the dotnet global tools folder
$toolPath = Join-Path $env:USERPROFILE ".dotnet\tools"
Write-Host "Tools folder: $toolPath"

# Confirm it exists
Test-Path $toolPath

# Inspect the contents (should list livingdoc.exe)
Get-ChildItem $toolPath
```

If you see “Access is denied” when invoking the tool, try:

```powershell
# Unblock the downloaded executable
Unblock-File -Path "$env:USERPROFILE\.dotnet\tools\livingdoc.exe"

# Run it directly to confirm
& "$env:USERPROFILE\.dotnet\tools\livingdoc.exe" --help
```

Still blocked? Launch an elevated PowerShell or CMD:

```cmd
"%USERPROFILE%\.dotnet\tools\livingdoc.exe" --help
```

If the problem persists, check permissions:

```powershell
icacls "$env:USERPROFILE\.dotnet\tools\livingdoc.exe"
```

Ensure your account has execute rights (adjust with `icacls` if necessary). Once the tool runs via full path, add the tools folder to PATH and use `livingdoc` normally.

As a last resort, you can call LivingDoc through the dotnet tool host (bypasses direct EXE execution):

```powershell
dotnet tool run livingdoc -- test-assembly SpecFlowTests/bin/Debug/net8.0/SpecFlowTests.dll `
    -t SpecFlowTests/bin/Debug/net8.0/TestExecution.json `
    --output SpecFlowTests/TestResults/LivingDoc.html
```

The generated `LivingDoc.html` provides an interactive view of scenarios, steps, and the latest execution results.

