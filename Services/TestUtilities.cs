using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using VaxCareApiTests.Models;

namespace VaxCareApiTests.Services;

public class TestUtilities
{
    private readonly IConfiguration _configuration;
    private readonly HeadersConfiguration _headersConfig;

    public TestUtilities(IConfiguration configuration)
    {
        _configuration = configuration;
        _headersConfig = new HeadersConfiguration();
        
        try
        {
            _configuration.GetSection("Headers").Bind(_headersConfig);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Warning: Could not bind Headers configuration: {ex.Message}");
            // Continue with default values
        }
    }

    public Dictionary<string, string> CreateTestHeaders(Dictionary<string, string>? overrides = null)
    {
        var defaultHeaders = new Dictionary<string, string>
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

        if (overrides != null)
        {
            foreach (var overrideItem in overrides)
            {
                defaultHeaders[overrideItem.Key] = overrideItem.Value;
            }
        }

        return defaultHeaders;
    }

    public VaxHubIdentifier DecodeVaxHubIdentifier(string base64Token)
    {
        try
        {
            var jsonBytes = Convert.FromBase64String(base64Token);
            var jsonString = Encoding.UTF8.GetString(jsonBytes);
            return JsonConvert.DeserializeObject<VaxHubIdentifier>(jsonString) ?? new VaxHubIdentifier();
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Failed to decode X-VaxHub-Identifier: {ex.Message}", ex);
        }
    }

    public bool ValidateBase64Token(string token)
    {
        if (string.IsNullOrEmpty(token))
            return false;

        // Check if it's valid base64
        try
        {
            Convert.FromBase64String(token);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool ValidateTraceparent(string traceparent)
    {
        if (string.IsNullOrEmpty(traceparent))
            return false;

        // Validate OpenTelemetry traceparent format: 00-{32-hex}-{16-hex}-{2-hex}
        var pattern = @"^[0-9a-f]{2}-[0-9a-f]{32}-[0-9a-f]{16}-[0-9a-f]{2}$";
        return Regex.IsMatch(traceparent, pattern, RegexOptions.IgnoreCase);
    }

    public void LogResponseDetails(HttpResponseMessage response, string? content = null)
    {
        Console.WriteLine($"Response Status: {response.StatusCode}");
        Console.WriteLine($"Response Headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"))}");
        
        if (!string.IsNullOrEmpty(content))
        {
            Console.WriteLine($"Response Data: {content}");
        }
    }

    public void LogErrorDetails(Exception ex, HttpResponseMessage? response = null)
    {
        Console.WriteLine($"Error: {ex.Message}");
        
        if (response != null)
        {
            Console.WriteLine($"Response Status: {response.StatusCode}");
            Console.WriteLine($"Response Headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"))}");
        }
    }
}
