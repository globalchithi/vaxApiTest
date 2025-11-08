using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace VaxCareApiTests.Services
{
    public class TestResultCollector : IDisposable
    {
        private readonly TestReportService _reportService;
        private readonly ILogger<TestResultCollector> _logger;
        private readonly Dictionary<string, TestResult> _runningTests;
        private readonly Stopwatch _overallStopwatch;

        public TestResultCollector(TestReportService reportService, ILogger<TestResultCollector> logger)
        {
            _reportService = reportService;
            _logger = logger;
            _runningTests = new Dictionary<string, TestResult>();
            _overallStopwatch = Stopwatch.StartNew();
        }

        public void StartTest(string testName, string className)
        {
            var testResult = new TestResult
            {
                TestName = testName,
                ClassName = className,
                StartTime = DateTime.Now,
                Status = TestStatus.Passed // Default to passed, will be updated if failed
            };

            _runningTests[testName] = testResult;
            _logger.LogInformation($"Starting test: {testName} in class {className}");
        }

        public void EndTest(string testName, TestStatus status, string? errorMessage = null, string? stackTrace = null)
        {
            if (_runningTests.TryGetValue(testName, out var testResult))
            {
                testResult.Status = status;
                testResult.EndTime = DateTime.Now;
                testResult.Duration = testResult.EndTime - testResult.StartTime;
                
                if (status == TestStatus.Failed)
                {
                    testResult.ErrorMessage = errorMessage ?? "Test failed";
                    testResult.StackTrace = stackTrace ?? string.Empty;
                }

                _reportService.AddTestResult(testResult);
                _runningTests.Remove(testName);
                
                var statusIcon = status == TestStatus.Passed ? "✅" : 
                                status == TestStatus.Failed ? "❌" : "⏭️";
                
                _logger.LogInformation($"{statusIcon} Test completed: {testName} ({testResult.Duration.TotalMilliseconds:F0}ms)");
            }
        }

        public async Task GenerateReportsAsync()
        {
            try
            {
                _overallStopwatch.Stop();
                _logger.LogInformation($"All tests completed in {_overallStopwatch.Elapsed.TotalSeconds:F2} seconds");
                
                await _reportService.GenerateReportsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating test reports");
                throw;
            }
        }

        public void Dispose()
        {
            _overallStopwatch?.Stop();
        }
    }

    // Simple test execution wrapper for manual test tracking
    public class SimpleTestRunner
    {
        private readonly TestResultCollector _resultCollector;

        public SimpleTestRunner(TestResultCollector resultCollector)
        {
            _resultCollector = resultCollector;
        }

        public async Task<T> ExecuteTestAsync<T>(Func<Task<T>> testAction, string testName, string className)
        {
            _resultCollector.StartTest(testName, className);
            
            try
            {
                var result = await testAction();
                _resultCollector.EndTest(testName, TestStatus.Passed);
                return result;
            }
            catch (Exception ex)
            {
                _resultCollector.EndTest(testName, TestStatus.Failed, ex.Message, ex.StackTrace);
                throw;
            }
        }

        public async Task ExecuteTestAsync(Func<Task> testAction, string testName, string className)
        {
            _resultCollector.StartTest(testName, className);
            
            try
            {
                await testAction();
                _resultCollector.EndTest(testName, TestStatus.Passed);
            }
            catch (Exception ex)
            {
                _resultCollector.EndTest(testName, TestStatus.Failed, ex.Message, ex.StackTrace);
                throw;
            }
        }
    }
}
