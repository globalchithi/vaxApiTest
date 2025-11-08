using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using HtmlAgilityPack;

namespace VaxCareApiTests.Services
{
    public class TestReportService
    {
        private readonly ILogger<TestReportService> _logger;
        private readonly string _reportsDirectory;
        private readonly List<TestResult> _testResults;

        public TestReportService(ILogger<TestReportService> logger)
        {
            _logger = logger;
            _reportsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "TestReports");
            _testResults = new List<TestResult>();
            
            // Ensure reports directory exists
            if (!Directory.Exists(_reportsDirectory))
            {
                Directory.CreateDirectory(_reportsDirectory);
            }
        }

        public void AddTestResult(TestResult testResult)
        {
            _testResults.Add(testResult);
        }

        public async Task GenerateReportsAsync()
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                
                // Generate HTML report
                await GenerateHtmlReportAsync(timestamp);
                
                // Generate JSON report
                await GenerateJsonReportAsync(timestamp);
                
                // Generate Markdown report
                await GenerateMarkdownReportAsync(timestamp);
                
                // Generate summary report
                await GenerateSummaryReportAsync(timestamp);
                
                _logger.LogInformation($"Test reports generated successfully at {_reportsDirectory}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating test reports");
                throw;
            }
        }

        private async Task GenerateHtmlReportAsync(string timestamp)
        {
            var htmlContent = GenerateHtmlContent();
            var filePath = Path.Combine(_reportsDirectory, $"TestReport_{timestamp}.html");
            await File.WriteAllTextAsync(filePath, htmlContent);
            _logger.LogInformation($"HTML report generated: {filePath}");
        }

        private async Task GenerateJsonReportAsync(string timestamp)
        {
            // Calculate total runtime
            var totalRuntime = TimeSpan.Zero;
            if (_testResults.Any())
            {
                var startTime = _testResults.Min(t => t.StartTime);
                var endTime = _testResults.Max(t => t.EndTime);
                totalRuntime = endTime - startTime;
            }

            var reportData = new
            {
                GeneratedAt = DateTime.Now,
                TotalTests = _testResults.Count,
                PassedTests = _testResults.Count(t => t.Status == TestStatus.Passed),
                FailedTests = _testResults.Count(t => t.Status == TestStatus.Failed),
                SkippedTests = _testResults.Count(t => t.Status == TestStatus.Skipped),
                SuccessRate = _testResults.Count > 0 ? (double)_testResults.Count(t => t.Status == TestStatus.Passed) / _testResults.Count * 100 : 0,
                TotalRuntime = totalRuntime,
                TestResults = _testResults.Select(tr => new
                {
                    tr.TestName,
                    tr.ClassName,
                    tr.Status,
                    tr.Duration,
                    tr.ErrorMessage,
                    tr.StackTrace,
                    tr.StartTime,
                    tr.EndTime
                })
            };

            var jsonContent = JsonSerializer.Serialize(reportData, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var filePath = Path.Combine(_reportsDirectory, $"TestReport_{timestamp}.json");
            await File.WriteAllTextAsync(filePath, jsonContent);
            _logger.LogInformation($"JSON report generated: {filePath}");
        }

        private async Task GenerateMarkdownReportAsync(string timestamp)
        {
            var markdownContent = GenerateMarkdownContent();
            var filePath = Path.Combine(_reportsDirectory, $"TestReport_{timestamp}.md");
            await File.WriteAllTextAsync(filePath, markdownContent);
            _logger.LogInformation($"Markdown report generated: {filePath}");
        }

        private async Task GenerateSummaryReportAsync(string timestamp)
        {
            var summaryContent = GenerateSummaryContent();
            var filePath = Path.Combine(_reportsDirectory, $"TestSummary_{timestamp}.txt");
            await File.WriteAllTextAsync(filePath, summaryContent);
            _logger.LogInformation($"Summary report generated: {filePath}");
        }

        private string GenerateHtmlContent()
        {
            var passedTests = _testResults.Count(t => t.Status == TestStatus.Passed);
            var failedTests = _testResults.Count(t => t.Status == TestStatus.Failed);
            var skippedTests = _testResults.Count(t => t.Status == TestStatus.Skipped);
            var totalTests = _testResults.Count;
            var successRate = totalTests > 0 ? (double)passedTests / totalTests * 100 : 0;
            
            // Calculate total runtime
            var totalRuntime = TimeSpan.Zero;
            if (_testResults.Any())
            {
                var startTime = _testResults.Min(t => t.StartTime);
                var endTime = _testResults.Max(t => t.EndTime);
                totalRuntime = endTime - startTime;
            }

            var html = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>VaxCare API Test Report</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }}
        .container {{ max-width: 1200px; margin: 0 auto; background: white; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; border-radius: 8px 8px 0 0; }}
        .header h1 {{ margin: 0; font-size: 2.5em; }}
        .header p {{ margin: 10px 0 0 0; opacity: 0.9; }}
        .stats {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 20px; padding: 30px; }}
        .stat-card {{ background: #f8f9fa; padding: 20px; border-radius: 8px; text-align: center; border-left: 4px solid #007bff; }}
        .stat-card.passed {{ border-left-color: #28a745; }}
        .stat-card.failed {{ border-left-color: #dc3545; }}
        .stat-card.skipped {{ border-left-color: #ffc107; }}
        .stat-number {{ font-size: 2.5em; font-weight: bold; margin: 0; }}
        .stat-label {{ color: #666; margin: 5px 0 0 0; }}
        .test-results {{ padding: 0 30px 30px; }}
        .test-result {{ background: #f8f9fa; margin: 10px 0; padding: 15px; border-radius: 6px; border-left: 4px solid #ddd; }}
        .test-result.passed {{ border-left-color: #28a745; background: #d4edda; }}
        .test-result.failed {{ border-left-color: #dc3545; background: #f8d7da; }}
        .test-result.skipped {{ border-left-color: #ffc107; background: #fff3cd; }}
        .test-name {{ font-weight: bold; font-size: 1.1em; }}
        .test-class {{ color: #666; font-size: 0.9em; }}
        .test-duration {{ color: #666; font-size: 0.9em; }}
        .error-message {{ background: #f8d7da; padding: 10px; border-radius: 4px; margin-top: 10px; font-family: monospace; }}
        .footer {{ background: #f8f9fa; padding: 20px; text-align: center; border-radius: 0 0 8px 8px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🧪 VaxCare API Test Report</h1>
            <p>Generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
        </div>
        
        <div class='stats'>
            <div class='stat-card passed'>
                <div class='stat-number'>{passedTests}</div>
                <div class='stat-label'>Passed</div>
            </div>
            <div class='stat-card failed'>
                <div class='stat-number'>{failedTests}</div>
                <div class='stat-label'>Failed</div>
            </div>
            <div class='stat-card skipped'>
                <div class='stat-number'>{skippedTests}</div>
                <div class='stat-label'>Skipped</div>
            </div>
            <div class='stat-card'>
                <div class='stat-number'>{totalTests}</div>
                <div class='stat-label'>Total</div>
            </div>
            <div class='stat-card'>
                <div class='stat-number'>{successRate:F1}%</div>
                <div class='stat-label'>Success Rate</div>
            </div>
            <div class='stat-card'>
                <div class='stat-number'>{totalRuntime.TotalSeconds:F1}s</div>
                <div class='stat-label'>Total Runtime</div>
            </div>
        </div>
        
        <div class='test-results'>
            <h2>Test Results</h2>";

            foreach (var test in _testResults)
            {
                var statusClass = test.Status.ToString().ToLower();
                var statusIcon = test.Status == TestStatus.Passed ? "✅" : 
                                test.Status == TestStatus.Failed ? "❌" : "⏭️";
                
                html += $@"
            <div class='test-result {statusClass}'>
                <div class='test-name'>{statusIcon} {test.TestName}</div>
                <div class='test-class'>{test.ClassName}</div>
                <div class='test-duration'>Duration: {test.Duration.TotalMilliseconds:F0}ms</div>";

                if (test.Status == TestStatus.Failed && !string.IsNullOrEmpty(test.ErrorMessage))
                {
                    html += $@"
                <div class='error-message'>
                    <strong>Error:</strong> {test.ErrorMessage}
                </div>";
                }

                html += @"
            </div>";
            }

            html += $@"
        </div>
        
        <div class='footer'>
            <p>Report generated by VaxCare API Test Suite | {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
        </div>
    </div>
</body>
</html>";

            return html;
        }

        private string GenerateMarkdownContent()
        {
            var passedTests = _testResults.Count(t => t.Status == TestStatus.Passed);
            var failedTests = _testResults.Count(t => t.Status == TestStatus.Failed);
            var skippedTests = _testResults.Count(t => t.Status == TestStatus.Skipped);
            var totalTests = _testResults.Count;
            var successRate = totalTests > 0 ? (double)passedTests / totalTests * 100 : 0;
            
            // Calculate total runtime
            var totalRuntime = TimeSpan.Zero;
            if (_testResults.Any())
            {
                var startTime = _testResults.Min(t => t.StartTime);
                var endTime = _testResults.Max(t => t.EndTime);
                totalRuntime = endTime - startTime;
            }

            var markdown = $@"# 🧪 VaxCare API Test Report

**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}

## 📊 Test Summary

| Status | Count | Percentage |
|--------|-------|------------|
| ✅ Passed | {passedTests} | {(double)passedTests / totalTests * 100:F1}% |
| ❌ Failed | {failedTests} | {(double)failedTests / totalTests * 100:F1}% |
| ⏭️ Skipped | {skippedTests} | {(double)skippedTests / totalTests * 100:F1}% |
| **Total** | **{totalTests}** | **{successRate:F1}%** |

## ⏱️ Execution Summary

- **Total Runtime:** {totalRuntime.TotalSeconds:F1} seconds ({totalRuntime.TotalMinutes:F1} minutes)
- **Average Test Duration:** {(totalTests > 0 ? _testResults.Average(t => t.Duration.TotalMilliseconds) : 0):F0}ms

## 📋 Test Results

";

            foreach (var test in _testResults)
            {
                var statusIcon = test.Status == TestStatus.Passed ? "✅" : 
                                test.Status == TestStatus.Failed ? "❌" : "⏭️";
                
                markdown += $@"### {statusIcon} {test.TestName}

- **Class:** {test.ClassName}
- **Status:** {test.Status}
- **Duration:** {test.Duration.TotalMilliseconds:F0}ms
- **Start Time:** {test.StartTime:yyyy-MM-dd HH:mm:ss}
- **End Time:** {test.EndTime:yyyy-MM-dd HH:mm:ss}";

                if (test.Status == TestStatus.Failed && !string.IsNullOrEmpty(test.ErrorMessage))
                {
                    markdown += $@"

**Error Message:**
```
{test.ErrorMessage}
```";
                }

                markdown += "\n\n---\n\n";
            }

            return markdown;
        }

        private string GenerateSummaryContent()
        {
            var passedTests = _testResults.Count(t => t.Status == TestStatus.Passed);
            var failedTests = _testResults.Count(t => t.Status == TestStatus.Failed);
            var skippedTests = _testResults.Count(t => t.Status == TestStatus.Skipped);
            var totalTests = _testResults.Count;
            var successRate = totalTests > 0 ? (double)passedTests / totalTests * 100 : 0;
            
            // Calculate total runtime
            var totalRuntime = TimeSpan.Zero;
            if (_testResults.Any())
            {
                var startTime = _testResults.Min(t => t.StartTime);
                var endTime = _testResults.Max(t => t.EndTime);
                totalRuntime = endTime - startTime;
            }

            return $@"VaxCare API Test Suite - Summary Report
Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

Test Results:
=============
Total Tests: {totalTests}
Passed: {passedTests}
Failed: {failedTests}
Skipped: {skippedTests}
Success Rate: {successRate:F1}%
Total Runtime: {totalRuntime.TotalSeconds:F1} seconds ({totalRuntime.TotalMinutes:F1} minutes)
Average Test Duration: {(totalTests > 0 ? _testResults.Average(t => t.Duration.TotalMilliseconds) : 0):F0}ms

Test Details:
=============
{string.Join("\n", _testResults.Select(t => $"{t.Status} - {t.TestName} ({t.ClassName}) - {t.Duration.TotalMilliseconds:F0}ms"))}

Report generated by VaxCare API Test Suite
";
        }
    }

    public class TestResult
    {
        public string TestName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public TestStatus Status { get; set; }
        public TimeSpan Duration { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string StackTrace { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    public enum TestStatus
    {
        Passed,
        Failed,
        Skipped
    }
}

