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

PowerShell equivalent on Windows:

```powershell
dotnet tool install --global SpecFlow.Plus.LivingDoc.CLI
$env:API_BASE_URL = "https://vhapistg.vaxcare.com"
dotnet test SpecFlowTests/SpecFlowTests.csproj --logger "trx"
livingdoc test-assembly SpecFlowTests/bin/Debug/net8.0/SpecFlowTests.dll `
    -t SpecFlowTests/bin/Debug/net8.0/TestExecution.json `
    --output SpecFlowTests/TestResults/LivingDoc.html
Start-Process "SpecFlowTests\TestResults\LivingDoc.html"
```

The generated `LivingDoc.html` provides an interactive view of scenarios, steps, and the latest execution results.

