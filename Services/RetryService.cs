using Microsoft.Extensions.Logging;
using VaxCareApiTests.Models;
using System.Net.Sockets;

namespace VaxCareApiTests.Services;

public class RetryService
{
    private readonly ILogger<RetryService> _logger;
    private readonly RetryConfiguration _retryConfig;

    public RetryService(ILogger<RetryService> logger, RetryConfiguration retryConfig)
    {
        _logger = logger;
        _retryConfig = retryConfig;
    }

    public async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        string operationName,
        Func<Exception, bool>? shouldRetry = null)
    {
        var attempt = 0;
        var lastException = new Exception();

        while (attempt <= _retryConfig.MaxRetryAttempts)
        {
            try
            {
                _logger.LogInformation($"Executing {operationName} - Attempt {attempt + 1}/{_retryConfig.MaxRetryAttempts + 1}");
                
                var result = await operation();
                
                if (attempt > 0)
                {
                    _logger.LogInformation($"✅ {operationName} succeeded on attempt {attempt + 1}");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                lastException = ex;
                attempt++;

                // Check if we should retry this exception
                if (attempt > _retryConfig.MaxRetryAttempts || !ShouldRetryException(ex, shouldRetry))
                {
                    _logger.LogError(ex, $"❌ {operationName} failed after {attempt} attempts. Giving up.");
                    throw;
                }

                var delay = CalculateDelay(attempt);
                _logger.LogWarning($"⚠️ {operationName} failed on attempt {attempt}. Retrying in {delay}ms. Error: {ex.Message}");
                
                await Task.Delay(delay);
            }
        }

        throw lastException;
    }

    private bool ShouldRetryException(Exception ex, Func<Exception, bool>? customShouldRetry)
    {
        // Use custom retry logic if provided
        if (customShouldRetry != null)
        {
            return customShouldRetry(ex);
        }

        // Default retry logic for common HTTP exceptions
        return ex switch
        {
            HttpRequestException httpEx when httpEx.Message.Contains("timeout") => true,
            HttpRequestException httpEx when httpEx.Message.Contains("connection") => true,
            HttpRequestException httpEx when httpEx.Message.Contains("network") => true,
            TaskCanceledException when ex.InnerException is TimeoutException => true,
            TaskCanceledException when ex.Message.Contains("timeout") => true,
            SocketException => true,
            _ => false
        };
    }

    private int CalculateDelay(int attempt)
    {
        if (!_retryConfig.ExponentialBackoff)
        {
            return _retryConfig.RetryDelayMs;
        }

        // Exponential backoff: delay = baseDelay * (2 ^ (attempt - 1))
        var delay = _retryConfig.RetryDelayMs * Math.Pow(2, attempt - 1);
        
        // Cap at maximum delay
        var cappedDelay = Math.Min(delay, _retryConfig.MaxRetryDelayMs);
        
        return (int)cappedDelay;
    }

    public async Task<HttpResponseMessage> ExecuteHttpRequestWithRetryAsync(
        Func<Task<HttpResponseMessage>> httpRequest,
        string requestName)
    {
        return await ExecuteWithRetryAsync(
            httpRequest,
            requestName,
            ShouldRetryHttpException
        );
    }

    private static bool ShouldRetryHttpException(Exception ex)
    {
        return ex switch
        {
            HttpRequestException httpEx => 
                httpEx.Message.Contains("timeout") ||
                httpEx.Message.Contains("connection") ||
                httpEx.Message.Contains("network") ||
                httpEx.Message.Contains("nodename nor servname provided") ||
                httpEx.Message.Contains("Name or service not known") ||
                httpEx.Message.Contains("No such host"),
            TaskCanceledException when ex.InnerException is TimeoutException => true,
            TaskCanceledException when ex.Message.Contains("timeout") => true,
            SocketException => true,
            _ => false
        };
    }
}
