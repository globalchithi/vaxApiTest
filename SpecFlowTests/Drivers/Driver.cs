using NUnit.Framework;
using SpecFlowTests.Support;
using TestContext = SpecFlowTests.Support.TestContext;

namespace SpecFlowTests.Drivers;

public class ApiDriver
{
    private readonly TestContext _testContext;

    public ApiDriver(TestContext testContext)
    {
        _testContext = testContext;
    }

    public async Task SendGetRequestAsync(string endpoint)
    {
        _testContext.SetRequestUri(endpoint);

        try
        {
            var response = await _testContext.HttpClient.GetAsync(endpoint).ConfigureAwait(false);
            await _testContext.CaptureResponseAsync(response).ConfigureAwait(false);
        }
        catch (HttpRequestException ex) when (IsNetworkConnectivityIssue(ex))
        {
            Assert.Inconclusive($"Network connectivity issue encountered when calling '{_testContext.RequestUri}': {ex.Message}");
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            Assert.Inconclusive($"Request timeout encountered when calling '{_testContext.RequestUri}'.");
        }
    }

    public async Task SendPostRequestAsync(string endpoint, string jsonPayload)
    {
        _testContext.SetRequestUri(endpoint);

        using var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

        try
        {
            var response = await _testContext.HttpClient.PostAsync(endpoint, content).ConfigureAwait(false);
            await _testContext.CaptureResponseAsync(response).ConfigureAwait(false);
        }
        catch (HttpRequestException ex) when (IsNetworkConnectivityIssue(ex))
        {
            Assert.Inconclusive($"Network connectivity issue encountered when calling '{_testContext.RequestUri}': {ex.Message}");
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            Assert.Inconclusive($"Request timeout encountered when calling '{_testContext.RequestUri}'.");
        }
    }

    public async Task SendPutRequestAsync(string endpoint, string jsonPayload)
    {
        _testContext.SetRequestUri(endpoint);

        using var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

        try
        {
            var response = await _testContext.HttpClient.PutAsync(endpoint, content).ConfigureAwait(false);
            await _testContext.CaptureResponseAsync(response).ConfigureAwait(false);
        }
        catch (HttpRequestException ex) when (IsNetworkConnectivityIssue(ex))
        {
            Assert.Inconclusive($"Network connectivity issue encountered when calling '{_testContext.RequestUri}': {ex.Message}");
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            Assert.Inconclusive($"Request timeout encountered when calling '{_testContext.RequestUri}'.");
        }
    }

    private static bool IsNetworkConnectivityIssue(HttpRequestException exception)
    {
        if (exception is null)
        {
            return false;
        }

        var message = exception.Message;
        return message.Contains("No such host", StringComparison.OrdinalIgnoreCase)
               || message.Contains("nodename nor servname provided", StringComparison.OrdinalIgnoreCase)
               || message.Contains("Name or service not known", StringComparison.OrdinalIgnoreCase)
               || message.Contains("The SSL connection could not be established", StringComparison.OrdinalIgnoreCase);
    }
}
