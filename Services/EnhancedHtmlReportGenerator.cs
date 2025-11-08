using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace VaxCareApiTests.Services
{
    public class EnhancedHtmlReportGenerator
    {
        private readonly ILogger<EnhancedHtmlReportGenerator> _logger;

        public EnhancedHtmlReportGenerator(ILogger<EnhancedHtmlReportGenerator> logger)
        {
            _logger = logger;
        }

        public async Task<string> GenerateHtmlReportAsync(string xmlFilePath, string outputPath)
        {
            try
            {
                var testResults = ParseXmlResults(xmlFilePath);
                var testInfo = LoadTestInfo();
                var htmlContent = GenerateHtmlContent(testResults, testInfo);
                await File.WriteAllTextAsync(outputPath, htmlContent);
                _logger.LogInformation($"Enhanced HTML report generated: {outputPath}");
                return outputPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating enhanced HTML report");
                throw;
            }
        }

        private TestResultsData ParseXmlResults(string xmlFilePath)
        {
            if (!File.Exists(xmlFilePath))
            {
                _logger.LogWarning($"XML file not found: {xmlFilePath}");
                return new TestResultsData();
            }

            try
            {
                var doc = XDocument.Load(xmlFilePath);
                var testResults = new TestResultsData();

                // Parse test results from XML
                var testCases = doc.Descendants("TestCase");
                foreach (var testCase in testCases)
                {
                    var testName = testCase.Attribute("name")?.Value ?? "Unknown";
                    var className = testCase.Attribute("className")?.Value ?? "Unknown";
                    var result = testCase.Attribute("result")?.Value ?? "Unknown";
                    var duration = testCase.Attribute("duration")?.Value ?? "0";
                    var startTime = testCase.Attribute("startTime")?.Value ?? DateTime.Now.ToString();
                    var endTime = testCase.Attribute("endTime")?.Value ?? DateTime.Now.ToString();

                    var testResult = new TestResult
                    {
                        TestName = testName,
                        ClassName = className,
                        Status = ParseTestStatus(result),
                        Duration = TimeSpan.FromMilliseconds(double.Parse(duration)),
                        StartTime = DateTime.Parse(startTime),
                        EndTime = DateTime.Parse(endTime),
                        ErrorMessage = testCase.Descendants("ErrorInfo").FirstOrDefault()?.Element("Message")?.Value,
                        StackTrace = testCase.Descendants("ErrorInfo").FirstOrDefault()?.Element("StackTrace")?.Value
                    };

                    testResults.TestResults.Add(testResult);
                }

                // Calculate statistics
                testResults.TotalTests = testResults.TestResults.Count;
                testResults.PassedTests = testResults.TestResults.Count(t => t.Status == TestStatus.Passed);
                testResults.FailedTests = testResults.TestResults.Count(t => t.Status == TestStatus.Failed);
                testResults.SkippedTests = testResults.TestResults.Count(t => t.Status == TestStatus.Skipped);
                testResults.SuccessRate = testResults.TotalTests > 0 ? (double)testResults.PassedTests / testResults.TotalTests * 100 : 0;
                
                // Calculate total runtime
                if (testResults.TestResults.Any())
                {
                    var startTime = testResults.TestResults.Min(t => t.StartTime);
                    var endTime = testResults.TestResults.Max(t => t.EndTime);
                    testResults.TotalRuntime = endTime - startTime;
                }

                return testResults;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing XML results");
                return new TestResultsData();
            }
        }

        private TestStatus ParseTestStatus(string result)
        {
            return result.ToLower() switch
            {
                "passed" => TestStatus.Passed,
                "failed" => TestStatus.Failed,
                "skipped" => TestStatus.Skipped,
                _ => TestStatus.Skipped
            };
        }

        private Dictionary<string, TestInfo> LoadTestInfo()
        {
            try
            {
                var testInfoPath = Path.Combine(Directory.GetCurrentDirectory(), "TestInfo.json");
                if (File.Exists(testInfoPath))
                {
                    var json = File.ReadAllText(testInfoPath);
                    var testInfoData = JsonSerializer.Deserialize<TestInfoData>(json);
                    return testInfoData?.TestInfo ?? new Dictionary<string, TestInfo>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load test info from TestInfo.json");
            }
            return new Dictionary<string, TestInfo>();
        }

        private string GenerateHtmlContent(TestResultsData testResults, Dictionary<string, TestInfo> testInfo)
        {
            var passedTests = testResults.PassedTests;
            var failedTests = testResults.FailedTests;
            var skippedTests = testResults.SkippedTests;
            var totalTests = testResults.TotalTests;
            var successRate = testResults.SuccessRate;
            var totalRuntime = testResults.TotalRuntime;

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
        .summary-stats {{ display: flex; justify-content: space-around; margin: 20px 0; }}
        .summary-stat {{ text-align: center; }}
        .summary-stat .number {{ font-size: 2em; font-weight: bold; color: #28a745; }}
        .summary-stat .label {{ color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🧪 VaxCare API Test Report</h1>
            <p>Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
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

            foreach (var test in testResults.TestResults)
            {
                var statusClass = test.Status.ToString().ToLower();
                var statusIcon = test.Status == TestStatus.Passed ? "✅" : 
                                test.Status == TestStatus.Failed ? "❌" : "⏭️";
                
                // Get test info if available
                var currentTestInfo = testInfo.ContainsKey(test.TestName) ? testInfo[test.TestName] : null;
                
                html += $@"
            <div class='test-result {statusClass}'>
                <div class='test-name'>{statusIcon} {test.TestName}</div>
                <div class='test-class'>{test.ClassName}</div>
                <div class='test-duration'>Duration: {test.Duration.TotalMilliseconds:F0}ms</div>";
                
                if (currentTestInfo != null)
                {
                    html += $@"
                <div class='test-info' style='margin-top: 10px; padding: 10px; background: #e9ecef; border-radius: 4px;'>
                    <div><strong>📋 Description:</strong> {currentTestInfo.Description}</div>
                    <div><strong>🎯 Test Type:</strong> {currentTestInfo.TestType}</div>
                    <div><strong>🔗 Endpoint:</strong> {currentTestInfo.Endpoint}</div>
                    <div><strong>📊 Expected Result:</strong> {currentTestInfo.ExpectedResult}</div>
                </div>";
                }

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
    }

    public class TestResultsData
    {
        public List<TestResult> TestResults { get; set; } = new List<TestResult>();
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
        public int SkippedTests { get; set; }
        public double SuccessRate { get; set; }
        public TimeSpan TotalRuntime { get; set; }
    }

    public class TestInfo
    {
        public string Description { get; set; } = "";
        public string TestType { get; set; } = "";
        public string Endpoint { get; set; } = "";
        public string ExpectedResult { get; set; } = "";
    }

    public class TestInfoData
    {
        public Dictionary<string, TestInfo> TestInfo { get; set; } = new();
    }
}

