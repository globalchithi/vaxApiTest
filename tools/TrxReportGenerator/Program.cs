using System.Xml.Linq;

var argsList = args.ToList();

if (argsList.Count < 2)
{
    Console.WriteLine("Usage: dotnet run --project tools/TrxReportGenerator -- <trx-file> <output-html>");
    return 1;
}

var trxPath = Path.GetFullPath(argsList[0]);
var outputPath = Path.GetFullPath(argsList[1]);

if (!File.Exists(trxPath))
{
    Console.Error.WriteLine($"❌ TRX file not found: {trxPath}");
    return 1;
}

Console.WriteLine($"ℹ️  Parsing TRX: {trxPath}");

try
{
    var doc = XDocument.Load(trxPath);
    var (summary, results) = ParseTrx(doc);
    var html = BuildHtml(summary, results);

    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
    await File.WriteAllTextAsync(outputPath, html);

    Console.WriteLine($"✅ TRX summary report generated at {outputPath}");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"❌ Failed to generate TRX report: {ex.Message}");
    return 1;
}

static (TrxSummary, List<TestResult>) ParseTrx(XDocument doc)
{
    XNamespace ns = doc.Root?.Name.Namespace ?? XNamespace.None;

    var summaryElement = doc.Descendants(ns + "ResultSummary").FirstOrDefault()
        ?? throw new InvalidOperationException("Missing ResultSummary element in TRX.");

    var countersElement = summaryElement.Element(ns + "Counters")
        ?? throw new InvalidOperationException("Missing Counters element in TRX.");

    var summary = new TrxSummary
    {
        Outcome = summaryElement.Attribute("outcome")?.Value ?? "Unknown",
        Total = (int?)countersElement.Attribute("total") ?? 0,
        Passed = (int?)countersElement.Attribute("passed") ?? 0,
        Failed = (int?)countersElement.Attribute("failed") ?? 0,
        Skipped = (int?)countersElement.Attribute("notExecuted") ?? 0,
    };

    var results = new List<TestResult>();

    foreach (var unitTestResult in doc.Descendants(ns + "UnitTestResult"))
    {
        var testResult = new TestResult
        {
            TestName = unitTestResult.Attribute("testName")?.Value ?? "Unnamed test",
            Outcome = unitTestResult.Attribute("outcome")?.Value ?? "Unknown",
            Duration = unitTestResult.Attribute("duration")?.Value,
            ErrorMessage = unitTestResult.Element(ns + "Output")?.Element(ns + "ErrorInfo")?.Element(ns + "Message")?.Value?.Trim(),
            ErrorStackTrace = unitTestResult.Element(ns + "Output")?.Element(ns + "ErrorInfo")?.Element(ns + "StackTrace")?.Value?.Trim(),
        };

        results.Add(testResult);
    }

    return (summary, results);
}

static string BuildHtml(TrxSummary summary, List<TestResult> results)
{
    var sb = new System.Text.StringBuilder();
    sb.AppendLine("<!DOCTYPE html>");
    sb.AppendLine("<html lang=\"en\">");
    sb.AppendLine("<head>");
    sb.AppendLine("<meta charset=\"utf-8\"/>");
    sb.AppendLine("<title>SpecFlow TRX Summary</title>");
    sb.AppendLine("<style>");
    sb.AppendLine("body { font-family: Arial, sans-serif; margin: 24px; color: #222; }");
    sb.AppendLine("h1 { margin-bottom: 0.4em; }");
    sb.AppendLine(".summary { margin-bottom: 1.5em; }");
    sb.AppendLine(".badge { display: inline-block; padding: 4px 10px; border-radius: 12px; font-size: 0.85em; margin-right: 8px; }");
    sb.AppendLine(".passed { background: #e6ffed; color: #256029; }");
    sb.AppendLine(".failed { background: #ffe9e9; color: #c02f1d; }");
    sb.AppendLine(".skipped { background: #fff3cd; color: #946200; }");
    sb.AppendLine(".unknown { background: #ececec; color: #333; }");
    sb.AppendLine("table { width: 100%; border-collapse: collapse; }");
    sb.AppendLine("th, td { border: 1px solid #e0e0e0; padding: 8px 10px; font-size: 0.95em; }");
    sb.AppendLine("th { background: #f7f9fb; text-align: left; }");
    sb.AppendLine(".error { white-space: pre-wrap; font-family: Consolas, monospace; background: #fff0f0; padding: 6px; border-radius: 4px; }");
    sb.AppendLine("</style>");
    sb.AppendLine("</head>");
    sb.AppendLine("<body>");

    sb.AppendLine("<h1>SpecFlow TRX Report</h1>");
    sb.AppendLine("<div class=\"summary\">");
    sb.AppendLine($"  <div><strong>Outcome:</strong> {summary.Outcome}</div>");
    sb.AppendLine($"  <span class=\"badge\">Total: {summary.Total}</span>");
    sb.AppendLine($"  <span class=\"badge passed\">Passed: {summary.Passed}</span>");
    sb.AppendLine($"  <span class=\"badge failed\">Failed: {summary.Failed}</span>");
    sb.AppendLine($"  <span class=\"badge skipped\">Skipped: {summary.Skipped}</span>");
    sb.AppendLine("</div>");

    if (results.Count == 0)
    {
        sb.AppendLine("<p>No individual test results were found.</p>");
    }
    else
    {
        sb.AppendLine("<table>");
        sb.AppendLine("<thead><tr><th>Test Name</th><th>Outcome</th><th>Duration</th><th>Details</th></tr></thead>");
        sb.AppendLine("<tbody>");

        foreach (var result in results.OrderByDescending(r => r.Outcome).ThenBy(r => r.TestName))
        {
            var outcomeClass = result.Outcome.ToLowerInvariant() switch
            {
                "passed" => "passed",
                "failed" => "failed",
                "notexecuted" or "skipped" => "skipped",
                _ => "unknown"
            };

            sb.AppendLine("<tr>");
            sb.AppendLine($"  <td>{System.Web.HttpUtility.HtmlEncode(result.TestName)}</td>");
            sb.AppendLine($"  <td><span class=\"badge {outcomeClass}\">{result.Outcome}</span></td>");
            sb.AppendLine($"  <td>{System.Web.HttpUtility.HtmlEncode(result.Duration ?? string.Empty)}</td>");

            if (!string.IsNullOrWhiteSpace(result.ErrorMessage) || !string.IsNullOrWhiteSpace(result.ErrorStackTrace))
            {
                sb.AppendLine("  <td class=\"error\">");
                if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                {
                    sb.AppendLine(System.Web.HttpUtility.HtmlEncode(result.ErrorMessage));
                }

                if (!string.IsNullOrWhiteSpace(result.ErrorStackTrace))
                {
                    sb.AppendLine("<br/><br/>");
                    sb.AppendLine(System.Web.HttpUtility.HtmlEncode(result.ErrorStackTrace));
                }
                sb.AppendLine("  </td>");
            }
            else
            {
                sb.AppendLine("  <td></td>");
            }

            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody>");
        sb.AppendLine("</table>");
    }

    sb.AppendLine("</body>");
    sb.AppendLine("</html>");

    return sb.ToString();
}

record TrxSummary
{
    public string Outcome { get; init; } = "Unknown";
    public int Total { get; init; }
    public int Passed { get; init; }
    public int Failed { get; init; }
    public int Skipped { get; init; }
}

record TestResult
{
    public string TestName { get; init; } = string.Empty;
    public string Outcome { get; init; } = "Unknown";
    public string? Duration { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ErrorStackTrace { get; init; }
}

