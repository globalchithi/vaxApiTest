using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace VaxCareApiTests.Services
{
    public class TestExecutionWrapper
    {
        private readonly ILogger<TestExecutionWrapper> _logger;
        private readonly TestReportService _reportService;
        private readonly TestResultCollector _resultCollector;
        private readonly Stopwatch _executionStopwatch;

        public TestExecutionWrapper(ILogger<TestExecutionWrapper> logger, TestReportService reportService, TestResultCollector resultCollector)
        {
            _logger = logger;
            _reportService = reportService;
            _resultCollector = resultCollector;
            _executionStopwatch = Stopwatch.StartNew();
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

        public async Task GenerateFinalReportsAsync()
        {
            try
            {
                _executionStopwatch.Stop();
                _logger.LogInformation($"Test execution completed in {_executionStopwatch.Elapsed.TotalSeconds:F2} seconds");
                
                await _resultCollector.GenerateReportsAsync();
                
                _logger.LogInformation("Test reports generated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating final test reports");
                throw;
            }
        }
    }

    // Service registration extension
    public static class TestServicesExtensions
    {
        public static IServiceCollection AddTestReporting(this IServiceCollection services)
        {
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            services.AddSingleton<TestReportService>();
            services.AddSingleton<TestResultCollector>();
            services.AddSingleton<TestExecutionWrapper>();
            
            return services;
        }
    }

    // Global test execution handler
    public class GlobalTestExecutionHandler
    {
        private static TestExecutionWrapper _wrapper;
        private static bool _isInitialized = false;

        public static void Initialize()
        {
            if (_isInitialized) return;

            var services = new ServiceCollection();
            services.AddTestReporting();
            
            var serviceProvider = services.BuildServiceProvider();
            _wrapper = serviceProvider.GetRequiredService<TestExecutionWrapper>();
            
            _isInitialized = true;
        }

        public static async Task<T> ExecuteTest<T>(Func<Task<T>> testAction, string testName, string className)
        {
            Initialize();
            return await _wrapper.ExecuteTestAsync(testAction, testName, className);
        }

        public static async Task ExecuteTest(Func<Task> testAction, string testName, string className)
        {
            Initialize();
            await _wrapper.ExecuteTestAsync(testAction, testName, className);
        }

        public static async Task GenerateReports()
        {
            if (_wrapper != null)
            {
                await _wrapper.GenerateFinalReportsAsync();
            }
        }
    }
}

