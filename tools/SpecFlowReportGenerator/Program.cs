using System.Net;
using System.Text;
using System.Text.Json;

var argsList = args.ToList();

var jsonPath = argsList.Count > 0 ? argsList[0] : null;
var outputPath = argsList.Count > 1 ? argsList[1] : null;

var testExecutionFile = ResolveExecutionJson(jsonPath);
if (testExecutionFile is null)
{
    Console.Error.WriteLine("❌ Could not find TestExecution.json. Provide the path explicitly or run `dotnet test` first.");
    return 1;
}

var reportFile = ResolveOutputPath(outputPath);

Console.WriteLine($"ℹ️  Using execution file: {testExecutionFile}");
Console.WriteLine($"ℹ️  Writing report to:   {reportFile}");

var json = await File.ReadAllTextAsync(testExecutionFile);
using var document = JsonDocument.Parse(json);

var summary = BuildSummary(document.RootElement);

var html = BuildHtml(summary);
Directory.CreateDirectory(Path.GetDirectoryName(reportFile)!);
await File.WriteAllTextAsync(reportFile, html, Encoding.UTF8);

Console.WriteLine($"✅ Custom SpecFlow report generated at {reportFile}");
return 0;

static string? ResolveExecutionJson(string? jsonPath)
{
    if (!string.IsNullOrWhiteSpace(jsonPath))
    {
        var fullPath = Path.GetFullPath(jsonPath);
        return File.Exists(fullPath) ? fullPath : null;
    }

    var candidates = new[]
    {
        Path.Combine("SpecFlowTests", "bin", "Debug", "net8.0", "TestExecution.json"),
        Path.Combine("SpecFlowTests", "bin", "Release", "net8.0", "TestExecution.json")
    };

    foreach (var candidate in candidates)
    {
        var fullCandidate = Path.GetFullPath(candidate);
        if (File.Exists(fullCandidate))
        {
            return fullCandidate;
        }
    }

    return null;
}

static string ResolveOutputPath(string? output)
{
    if (!string.IsNullOrWhiteSpace(output))
    {
        return Path.GetFullPath(output);
    }

    return Path.GetFullPath(Path.Combine("SpecFlowTests", "TestResults", "CustomReport.html"));
}

static TestRunSummary BuildSummary(JsonElement root)
{
    if (!root.TryGetProperty("testRun", out var testRun))
    {
        throw new InvalidOperationException("Invalid TestExecution.json format: missing testRun element.");
    }

    var summary = new TestRunSummary();

    if (!testRun.TryGetProperty("features", out var featuresElement))
    {
        return summary;
    }

    foreach (var featureElement in featuresElement.EnumerateArray())
    {
        var feature = new FeatureReport
        {
            Name = featureElement.TryGetProperty("name", out var featureNameEl) ? featureNameEl.GetString() ?? "Unnamed feature" : "Unnamed feature",
            Description = featureElement.TryGetProperty("description", out var descEl) ? descEl.GetString() ?? string.Empty : string.Empty
        };

        if (featureElement.TryGetProperty("scenarios", out var scenariosElement))
        {
            foreach (var scenarioElement in scenariosElement.EnumerateArray())
            {
                var scenario = ParseScenario(scenarioElement);
                feature.Scenarios.Add(scenario);
                summary.TotalScenarios++;

                switch (scenario.Status)
                {
                    case "Passed":
                        summary.Passed++;
                        break;
                    case "Failed":
                        summary.Failed++;
                        break;
                    case "Skipped":
                        summary.Skipped++;
                        break;
                    default:
                        summary.Other++;
                        break;
                }
            }
        }

        summary.Features.Add(feature);
    }

    return summary;
}

static ScenarioReport ParseScenario(JsonElement scenarioElement)
{
    var scenario = new ScenarioReport
    {
        Name = scenarioElement.TryGetProperty("name", out var scenarioNameEl) ? scenarioNameEl.GetString() ?? "Unnamed scenario" : "Unnamed scenario",
        Status = ExtractStatus(scenarioElement),
        Duration = ExtractDuration(scenarioElement)
    };

    if (scenarioElement.TryGetProperty("steps", out var stepsElement))
    {
        foreach (var stepElement in stepsElement.EnumerateArray())
        {
            var step = new StepReport
            {
                Name = stepElement.TryGetProperty("name", out var stepNameEl) ? stepNameEl.GetString() ?? "Unnamed step" : "Unnamed step",
                Keyword = stepElement.TryGetProperty("keyword", out var keywordEl) ? keywordEl.GetString() ?? string.Empty : string.Empty,
                Status = ExtractStatus(stepElement),
                Duration = ExtractDuration(stepElement)
            };

            scenario.Steps.Add(step);
        }
    }

    return scenario;
}

static string ExtractStatus(JsonElement element)
{
    if (element.TryGetProperty("result", out var resultEl))
    {
        if (resultEl.ValueKind == JsonValueKind.String)
        {
            return NormalizeStatus(resultEl.GetString());
        }

        if (resultEl.ValueKind == JsonValueKind.Object)
        {
            if (resultEl.TryGetProperty("status", out var statusEl))
            {
                return NormalizeStatus(statusEl.GetString());
            }
        }
    }

    if (element.TryGetProperty("status", out var bareStatus))
    {
        return NormalizeStatus(bareStatus.GetString());
    }

    return "Unknown";
}

static string NormalizeStatus(string? status) =>
    status switch
    {
        null => "Unknown",
        var s when s.Equals("Success", StringComparison.OrdinalIgnoreCase) => "Passed",
        var s when s.Equals("Fail", StringComparison.OrdinalIgnoreCase) => "Failed",
        var s when s.Equals("Pending", StringComparison.OrdinalIgnoreCase) => "Skipped",
        var s when s.Equals("Skip", StringComparison.OrdinalIgnoreCase) => "Skipped",
        var s when s.Equals("Passed", StringComparison.OrdinalIgnoreCase) => "Passed",
        var s when s.Equals("Failed", StringComparison.OrdinalIgnoreCase) => "Failed",
        var s when s.Equals("Skipped", StringComparison.OrdinalIgnoreCase) => "Skipped",
        var s => char.ToUpperInvariant(s[0]) + s[1..]
    };

static TimeSpan? ExtractDuration(JsonElement element)
{
    if (element.TryGetProperty("duration", out var durationEl))
    {
        return ParseDuration(durationEl);
    }

    if (element.TryGetProperty("executionTime", out var executionEl))
    {
        return ParseDuration(executionEl);
    }

    return null;
}

static TimeSpan? ParseDuration(JsonElement element)
{
    try
    {
        return element.ValueKind switch
        {
            JsonValueKind.Number when element.TryGetInt64(out var ticks) => TimeSpan.FromTicks(ticks),
            JsonValueKind.Number when element.TryGetDouble(out var seconds) => TimeSpan.FromSeconds(seconds),
            JsonValueKind.String when TimeSpan.TryParse(element.GetString(), out var ts) => ts,
            JsonValueKind.String when long.TryParse(element.GetString(), out var ticks) => TimeSpan.FromTicks(ticks),
            _ => null
        };
    }
    catch
    {
        return null;
    }
}

static string BuildHtml(TestRunSummary summary)
{
    var sb = new StringBuilder();
    sb.AppendLine("<!DOCTYPE html>");
    sb.AppendLine("<html lang=\"en\">");
    sb.AppendLine("<head>");
    sb.AppendLine("<meta charset=\"utf-8\"/>");
    sb.AppendLine("<title>SpecFlow Custom Report</title>");
    sb.AppendLine("<style>");
    sb.AppendLine("body { font-family: Arial, sans-serif; margin: 24px; color: #222; }");
    sb.AppendLine("h1 { margin-bottom: 0.2em; }");
    sb.AppendLine(".summary { margin-bottom: 1.5em; }");
    sb.AppendLine(".badge { display: inline-block; padding: 4px 10px; border-radius: 12px; font-size: 0.85em; margin-right: 8px; }");
    sb.AppendLine(".passed { background: #e6ffed; color: #256029; }");
    sb.AppendLine(".failed { background: #ffe9e9; color: #c02f1d; }");
    sb.AppendLine(".skipped { background: #fff3cd; color: #946200; }");
    sb.AppendLine(".unknown { background: #ececec; color: #333; }");
    sb.AppendLine(".feature { border: 1px solid #dce3e8; border-radius: 8px; padding: 16px; margin-bottom: 16px; }");
    sb.AppendLine(".feature h2 { margin-top: 0; }");
    sb.AppendLine(".scenario { margin-left: 16px; margin-bottom: 8px; }");
    sb.AppendLine(".scenario-title { font-weight: bold; }");
    sb.AppendLine(".steps { border-collapse: collapse; width: 100%; margin-top: 6px; }");
    sb.AppendLine(".steps th, .steps td { border: 1px solid #e0e0e0; padding: 6px 10px; font-size: 0.95em; }");
    sb.AppendLine(".steps th { background: #f7f9fb; text-align: left; }");
    sb.AppendLine("</style>");
    sb.AppendLine("</head>");
    sb.AppendLine("<body>");

    sb.AppendLine("<h1>SpecFlow Custom Report</h1>");
    sb.AppendLine("<div class=\"summary\">");
    sb.AppendLine($"  <span class=\"badge passed\">Passed: {summary.Passed}</span>");
    sb.AppendLine($"  <span class=\"badge failed\">Failed: {summary.Failed}</span>");
    sb.AppendLine($"  <span class=\"badge skipped\">Skipped: {summary.Skipped}</span>");
    if (summary.Other > 0)
    {
        sb.AppendLine($"  <span class=\"badge unknown\">Other: {summary.Other}</span>");
    }
    sb.AppendLine($"  <span class=\"badge\">Total: {summary.TotalScenarios}</span>");
    sb.AppendLine("</div>");

    foreach (var feature in summary.Features)
    {
        sb.AppendLine("<div class=\"feature\">");
        sb.AppendLine($"  <h2>{WebUtility.HtmlEncode(feature.Name)}</h2>");
        if (!string.IsNullOrWhiteSpace(feature.Description))
        {
            sb.AppendLine($"  <p>{WebUtility.HtmlEncode(feature.Description)}</p>");
        }

        foreach (var scenario in feature.Scenarios)
        {
            var statusClass = scenario.Status.ToLowerInvariant() switch
            {
                "passed" => "passed",
                "failed" => "failed",
                "skipped" => "skipped",
                _ => "unknown"
            };

            var durationText = scenario.Duration.HasValue ? $" • {scenario.Duration.Value.TotalSeconds:F2}s" : string.Empty;

            sb.AppendLine("  <div class=\"scenario\">");
            sb.AppendLine($"    <div class=\"scenario-title\">{WebUtility.HtmlEncode(scenario.Name)} <span class=\"badge {statusClass}\">{scenario.Status}</span>{durationText}</div>");

            if (scenario.Steps.Count > 0)
            {
                sb.AppendLine("    <table class=\"steps\">");
                sb.AppendLine("      <thead><tr><th>Step</th><th>Status</th><th>Duration</th></tr></thead>");
                sb.AppendLine("      <tbody>");
                foreach (var step in scenario.Steps)
                {
                    var stepStatusClass = step.Status.ToLowerInvariant() switch
                    {
                        "passed" => "passed",
                        "failed" => "failed",
                        "skipped" => "skipped",
                        _ => "unknown"
                    };
                    var stepDuration = step.Duration.HasValue ? $"{step.Duration.Value.TotalSeconds:F2}s" : string.Empty;
                    var stepName = string.IsNullOrWhiteSpace(step.Keyword)
                        ? WebUtility.HtmlEncode(step.Name)
                        : $"{WebUtility.HtmlEncode(step.Keyword)} {WebUtility.HtmlEncode(step.Name)}";

                    sb.AppendLine("        <tr>");
                    sb.AppendLine($"          <td>{stepName}</td>");
                    sb.AppendLine($"          <td><span class=\"badge {stepStatusClass}\">{step.Status}</span></td>");
                    sb.AppendLine($"          <td>{stepDuration}</td>");
                    sb.AppendLine("        </tr>");
                }
                sb.AppendLine("      </tbody>");
                sb.AppendLine("    </table>");
            }

            sb.AppendLine("  </div>");
        }

        sb.AppendLine("</div>");
    }

    sb.AppendLine("</body>");
    sb.AppendLine("</html>");

    return sb.ToString();
}

record TestRunSummary
{
    public List<FeatureReport> Features { get; } = new();
    public int TotalScenarios { get; set; }
    public int Passed { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }
    public int Other { get; set; }
}

record FeatureReport
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<ScenarioReport> Scenarios { get; } = new();
}

record ScenarioReport
{
    public string Name { get; init; } = string.Empty;
    public string Status { get; init; } = "Unknown";
    public TimeSpan? Duration { get; init; }
    public List<StepReport> Steps { get; } = new();
}

record StepReport
{
    public string Name { get; init; } = string.Empty;
    public string Keyword { get; init; } = string.Empty;
    public string Status { get; init; } = "Unknown";
    public TimeSpan? Duration { get; init; }
}

