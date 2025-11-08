using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VaxCareApiTests.Models;
using System.Net.Sockets;

namespace VaxCareApiTests.Services;

public class HttpClientService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpClientService> _logger;
    private readonly IConfiguration _configuration;
    private readonly ApiConfiguration _apiConfig;
    private readonly HeadersConfiguration _headersConfig;
    private readonly RetryService? _retryService;

    public HttpClientService(HttpClient httpClient, IConfiguration configuration, ILogger<HttpClientService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
        
        // Read configuration directly from IConfiguration with validation
        _apiConfig = new ApiConfiguration
        {
            BaseUrl = configuration["ApiConfiguration:BaseUrl"] ?? "https://vhapistg.vaxcare.com",
            Timeout = ParseIntWithDefault(configuration["ApiConfiguration:Timeout"], 30000),
            InsecureHttps = ParseBoolWithDefault(configuration["ApiConfiguration:InsecureHttps"], true)
        };
        
        _headersConfig = new HeadersConfiguration
        {
            IsCalledByJob = configuration["Headers:IsCalledByJob"] ?? "",
            XVaxHubIdentifier = configuration["Headers:X-VaxHub-Identifier"] ?? "",
            Traceparent = configuration["Headers:traceparent"] ?? "",
            MobileData = configuration["Headers:MobileData"] ?? "",
            UserSessionId = configuration["Headers:UserSessionId"] ?? "",
            MessageSource = configuration["Headers:MessageSource"] ?? "",
            Host = configuration["Headers:Host"] ?? "",
            Connection = configuration["Headers:Connection"] ?? "",
            UserAgent = configuration["Headers:User-Agent"] ?? ""
        };
        
        // Initialize retry service if retry configuration is available
        if (_apiConfig.RetryConfiguration != null)
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            _retryService = new RetryService(
                loggerFactory.CreateLogger<RetryService>(), 
                _apiConfig.RetryConfiguration
            );
        }

        // Debug: Log configuration values
        _logger.LogInformation($"BaseUrl: {_apiConfig.BaseUrl}");
        _logger.LogInformation($"IsCalledByJob: '{_headersConfig.IsCalledByJob}'");
        _logger.LogInformation($"XVaxHubIdentifier: '{_headersConfig.XVaxHubIdentifier}'");
        _logger.LogInformation($"UserAgent: '{_headersConfig.UserAgent}'");
        _logger.LogInformation($"Timeout: {_apiConfig.Timeout}ms");
        if (_apiConfig.RetryConfiguration != null)
        {
            _logger.LogInformation($"Retry: {_apiConfig.RetryConfiguration.MaxRetryAttempts} attempts, {_apiConfig.RetryConfiguration.RetryDelayMs}ms base delay");
        }
        
        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        // Set base URL
        _httpClient.BaseAddress = new Uri(_apiConfig.BaseUrl);
        
        // Set timeout
        _httpClient.Timeout = TimeSpan.FromMilliseconds(_apiConfig.Timeout);
        
        // Configure insecure HTTPS if needed (equivalent to curl --insecure)
        if (_apiConfig.InsecureHttps)
        {
            // Note: In production, you should use proper certificate validation
            // This is only for testing purposes
            // For .NET 6.0, we'll handle this in the HttpClient configuration
        }
        
        // Configure proxy if specified
        var proxyUrl = _configuration["ApiConfiguration:ProxyUrl"];
        if (!string.IsNullOrEmpty(proxyUrl))
        {
            // Note: Proxy configuration would need to be set up in the HttpClientHandler
            // For now, we'll just log that proxy is configured
            _logger.LogInformation($"Proxy configured: {proxyUrl}");
        }
        
        // Add default headers
        AddDefaultHeaders();
    }

    private void AddDefaultHeaders()
    {
        _httpClient.DefaultRequestHeaders.Clear();
        
        // Only add headers with non-empty values
        if (!string.IsNullOrEmpty(_headersConfig.IsCalledByJob))
            _httpClient.DefaultRequestHeaders.Add("IsCalledByJob", _headersConfig.IsCalledByJob);
        
        if (!string.IsNullOrEmpty(_headersConfig.XVaxHubIdentifier))
            _httpClient.DefaultRequestHeaders.Add("X-VaxHub-Identifier", _headersConfig.XVaxHubIdentifier);
        
        if (!string.IsNullOrEmpty(_headersConfig.Traceparent))
            _httpClient.DefaultRequestHeaders.Add("traceparent", _headersConfig.Traceparent);
        
        if (!string.IsNullOrEmpty(_headersConfig.MobileData))
            _httpClient.DefaultRequestHeaders.Add("MobileData", _headersConfig.MobileData);
        
        if (!string.IsNullOrEmpty(_headersConfig.UserSessionId))
            _httpClient.DefaultRequestHeaders.Add("UserSessionId", _headersConfig.UserSessionId);
        
        if (!string.IsNullOrEmpty(_headersConfig.MessageSource))
            _httpClient.DefaultRequestHeaders.Add("MessageSource", _headersConfig.MessageSource);
        
        if (!string.IsNullOrEmpty(_headersConfig.Host))
            _httpClient.DefaultRequestHeaders.Add("Host", _headersConfig.Host);
        
        if (!string.IsNullOrEmpty(_headersConfig.Connection))
            _httpClient.DefaultRequestHeaders.Add("Connection", _headersConfig.Connection);
        
        if (!string.IsNullOrEmpty(_headersConfig.UserAgent))
            _httpClient.DefaultRequestHeaders.Add("User-Agent", _headersConfig.UserAgent);
    }

    public async Task<HttpResponseMessage> GetAsync(string endpoint)
    {
        if (_retryService != null)
        {
            return await _retryService.ExecuteHttpRequestWithRetryAsync(
                async () => await ExecuteGetRequestAsync(endpoint),
                $"GET {endpoint}"
            );
        }
        else
        {
            return await ExecuteGetRequestAsync(endpoint);
        }
    }

    private async Task<HttpResponseMessage> ExecuteGetRequestAsync(string endpoint)
    {
        try
        {
            _logger.LogInformation($"Making GET request to: {_httpClient.BaseAddress}{endpoint}");
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var response = await _httpClient.GetAsync(endpoint);
            stopwatch.Stop();
            
            _logger.LogInformation($"Request completed in: {stopwatch.ElapsedMilliseconds}ms");
            
            // Log response details
            _logger.LogInformation($"Response Status: {response.StatusCode}");
            _logger.LogInformation($"Response Reason: {response.ReasonPhrase}");
            _logger.LogInformation($"Response Version: {response.Version}");
            
            // Log response headers
            _logger.LogInformation("=== RESPONSE HEADERS ===");
            foreach (var header in response.Headers)
            {
                _logger.LogInformation($"  {header.Key}: {string.Join(", ", header.Value)}");
            }
            
            // Log content headers
            if (response.Content != null)
            {
                _logger.LogInformation("=== CONTENT HEADERS ===");
                foreach (var header in response.Content.Headers)
                {
                    _logger.LogInformation($"  {header.Key}: {string.Join(", ", header.Value)}");
                }
                
                // Log response body
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("=== RESPONSE BODY ===");
                _logger.LogInformation($"Content Length: {content.Length} characters");
                _logger.LogInformation($"Content Type: {response.Content.Headers.ContentType}");
                
                // Log first 2000 characters of response body (increased from 1000)
                var preview = content.Length > 2000 ? content.Substring(0, 2000) + "..." : content;
                _logger.LogInformation($"Response Body Preview:\n{preview}");
                
                // Log full response body if it's small enough
                if (content.Length <= 5000)
                {
                    _logger.LogInformation($"=== FULL RESPONSE BODY ===\n{content}");
                }
                
                // If it's JSON, try to format it
                if (response.Content.Headers.ContentType?.MediaType?.Contains("json") == true)
                {
                    try
                    {
                        var jsonObject = System.Text.Json.JsonSerializer.Deserialize<object>(content);
                        var formattedJson = System.Text.Json.JsonSerializer.Serialize(jsonObject, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                        _logger.LogInformation("=== FORMATTED JSON RESPONSE ===");
                        _logger.LogInformation(formattedJson);
                    }
                    catch (System.Text.Json.JsonException jsonEx)
                    {
                        _logger.LogWarning($"Could not parse JSON response: {jsonEx.Message}");
                    }
                    catch (System.InvalidOperationException invalidOpEx)
                    {
                        _logger.LogWarning($"Invalid operation during JSON processing: {invalidOpEx.Message}");
                    }
                    catch (Exception jsonEx)
                    {
                        _logger.LogWarning($"Unexpected error during JSON processing: {jsonEx.Message}");
                    }
                }
            }
            
            return response;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed");
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timeout");
            throw;
        }
    }

    public async Task<HttpResponseMessage> PostAsync(string endpoint, HttpContent content)
    {
        if (_retryService != null)
        {
            return await _retryService.ExecuteHttpRequestWithRetryAsync(
                async () => await ExecutePostRequestAsync(endpoint, content),
                $"POST {endpoint}"
            );
        }
        else
        {
            return await ExecutePostRequestAsync(endpoint, content);
        }
    }

    private async Task<HttpResponseMessage> ExecutePostRequestAsync(string endpoint, HttpContent content)
    {
        try
        {
            _logger.LogInformation($"Making POST request to: {_httpClient.BaseAddress}{endpoint}");
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var response = await _httpClient.PostAsync(endpoint, content);
            stopwatch.Stop();
            
            _logger.LogInformation($"Request completed in: {stopwatch.ElapsedMilliseconds}ms");
            
            // Log response details
            _logger.LogInformation($"Response Status: {response.StatusCode}");
            _logger.LogInformation($"Response Reason: {response.ReasonPhrase}");
            _logger.LogInformation($"Response Version: {response.Version}");
            
            // Log response headers
            _logger.LogInformation("=== RESPONSE HEADERS ===");
            foreach (var header in response.Headers)
            {
                _logger.LogInformation($"  {header.Key}: {string.Join(", ", header.Value)}");
            }
            
            // Log content headers
            if (response.Content != null)
            {
                _logger.LogInformation("=== CONTENT HEADERS ===");
                foreach (var header in response.Content.Headers)
                {
                    _logger.LogInformation($"  {header.Key}: {string.Join(", ", header.Value)}");
                }
                
                // Log response body
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("=== RESPONSE BODY ===");
                _logger.LogInformation($"Content Length: {responseContent.Length} characters");
                _logger.LogInformation($"Content Type: {response.Content.Headers.ContentType}");
                
                // Log first 2000 characters of response body
                var preview = responseContent.Length > 2000 ? responseContent.Substring(0, 2000) + "..." : responseContent;
                _logger.LogInformation($"Response Body Preview:\n{preview}");
                
                // Log full response body if it's small enough
                if (responseContent.Length <= 5000)
                {
                    _logger.LogInformation($"=== FULL RESPONSE BODY ===\n{responseContent}");
                }
                
                // If it's JSON, try to format it
                if (response.Content.Headers.ContentType?.MediaType?.Contains("json") == true)
                {
                    try
                    {
                        var jsonObject = System.Text.Json.JsonSerializer.Deserialize<object>(responseContent);
                        var formattedJson = System.Text.Json.JsonSerializer.Serialize(jsonObject, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                        _logger.LogInformation("=== FORMATTED JSON RESPONSE ===");
                        _logger.LogInformation(formattedJson);
                    }
                    catch (System.Text.Json.JsonException jsonEx)
                    {
                        _logger.LogWarning($"Could not parse JSON response: {jsonEx.Message}");
                    }
                    catch (System.InvalidOperationException invalidOpEx)
                    {
                        _logger.LogWarning($"Invalid operation during JSON processing: {invalidOpEx.Message}");
                    }
                    catch (Exception jsonEx)
                    {
                        _logger.LogWarning($"Unexpected error during JSON processing: {jsonEx.Message}");
                    }
                }
            }
            
            return response;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed");
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timeout");
            throw;
        }
    }

    public Dictionary<string, string> GetHeaders()
    {
        return new Dictionary<string, string>
        {
            ["IsCalledByJob"] = _headersConfig.IsCalledByJob,
            ["X-VaxHub-Identifier"] = _headersConfig.XVaxHubIdentifier,
            ["traceparent"] = _headersConfig.Traceparent,
            ["MobileData"] = _headersConfig.MobileData,
            ["UserSessionId"] = _headersConfig.UserSessionId,
            ["MessageSource"] = _headersConfig.MessageSource,
            ["Host"] = _headersConfig.Host,
            ["Connection"] = _headersConfig.Connection,
            ["User-Agent"] = _headersConfig.UserAgent
        };
    }

    private static int ParseIntWithDefault(string? value, int defaultValue)
    {
        if (string.IsNullOrEmpty(value))
            return defaultValue;
        
        try
        {
            return int.Parse(value);
        }
        catch (FormatException)
        {
            return defaultValue;
        }
        catch (OverflowException)
        {
            return defaultValue;
        }
    }

    private static bool ParseBoolWithDefault(string? value, bool defaultValue)
    {
        if (string.IsNullOrEmpty(value))
            return defaultValue;
        
        try
        {
            return bool.Parse(value);
        }
        catch (FormatException)
        {
            return defaultValue;
        }
    }

    public async Task<HttpResponseMessage> PutAsync(string endpoint, HttpContent content)
    {
        if (_retryService != null)
        {
            return await _retryService.ExecuteHttpRequestWithRetryAsync(
                async () => await ExecutePutRequestAsync(endpoint, content),
                $"PUT {endpoint}"
            );
        }
        else
        {
            return await ExecutePutRequestAsync(endpoint, content);
        }
    }

    private async Task<HttpResponseMessage> ExecutePutRequestAsync(string endpoint, HttpContent content)
    {
        try
        {
            _logger.LogInformation($"Making PUT request to: {_httpClient.BaseAddress}{endpoint}");
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var response = await _httpClient.PutAsync(endpoint, content);
            stopwatch.Stop();
            
            _logger.LogInformation($"Request completed in: {stopwatch.ElapsedMilliseconds}ms");
            
            // Log response details
            _logger.LogInformation($"Response Status: {response.StatusCode}");
            _logger.LogInformation($"Response Reason: {response.ReasonPhrase}");
            _logger.LogInformation($"Response Version: {response.Version}");
            
            // Log response headers
            _logger.LogInformation("=== RESPONSE HEADERS ===");
            foreach (var header in response.Headers)
            {
                _logger.LogInformation($"  {header.Key}: {string.Join(", ", header.Value)}");
            }
            
            // Log content headers
            if (response.Content != null)
            {
                _logger.LogInformation("=== CONTENT HEADERS ===");
                foreach (var header in response.Content.Headers)
                {
                    _logger.LogInformation($"  {header.Key}: {string.Join(", ", header.Value)}");
                }
                
                // Log response body
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("=== RESPONSE BODY ===");
                _logger.LogInformation($"Content Length: {responseContent.Length} characters");
                _logger.LogInformation($"Content Type: {response.Content.Headers.ContentType}");
                
                // Log first 2000 characters of response body
                var preview = responseContent.Length > 2000 ? responseContent.Substring(0, 2000) + "..." : responseContent;
                _logger.LogInformation($"Response Body Preview:\n{preview}");
                
                // Log full response body if it's small enough
                if (responseContent.Length <= 5000)
                {
                    _logger.LogInformation($"=== FULL RESPONSE BODY ===\n{responseContent}");
                }
                
                // Format and log JSON response if applicable
                if (response.Content.Headers.ContentType?.MediaType?.Contains("json") == true)
                {
                    try
                    {
                        var jsonObject = System.Text.Json.JsonSerializer.Deserialize<object>(responseContent);
                        var formattedJson = System.Text.Json.JsonSerializer.Serialize(jsonObject, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                        _logger.LogInformation("=== FORMATTED JSON RESPONSE ===");
                        _logger.LogInformation(formattedJson);
                    }
                    catch (System.Text.Json.JsonException jsonEx)
                    {
                        _logger.LogWarning($"Could not parse JSON response: {jsonEx.Message}");
                    }
                    catch (System.InvalidOperationException invalidOpEx)
                    {
                        _logger.LogWarning($"Invalid operation during JSON processing: {invalidOpEx.Message}");
                    }
                    catch (Exception jsonEx)
                    {
                        _logger.LogWarning($"Unexpected error during JSON processing: {jsonEx.Message}");
                    }
                }
            }
            
            return response;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed");
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request was cancelled or timed out");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred during PUT request");
            throw;
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
