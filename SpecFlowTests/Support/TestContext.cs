using System.Text.Json.Nodes;

namespace SpecFlowTests.Support;

public class TestContext
{
    public HttpClient HttpClient { get; }
    public HttpResponseMessage? Response { get; private set; }
    public JsonNode? JsonBody { get; private set; }
    public Uri? RequestUri { get; private set; }

    public TestContext(HttpClient httpClient)
    {
        HttpClient = httpClient;
    }

    public async Task CaptureResponseAsync(HttpResponseMessage response)
    {
        Response = response;
        JsonBody = null;

        if (response.Content == null)
        {
            JsonBody = null;
            return;
        }

        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        JsonBody = string.IsNullOrWhiteSpace(content) ? null : JsonNode.Parse(content);
    }

    public void SetRequestUri(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new ArgumentException("Endpoint cannot be null or whitespace.", nameof(endpoint));
        }

        if (Uri.TryCreate(endpoint, UriKind.Absolute, out var absoluteUri))
        {
            RequestUri = absoluteUri;
            return;
        }

        if (HttpClient.BaseAddress is null)
        {
            throw new InvalidOperationException("Cannot resolve relative endpoint because HttpClient.BaseAddress is not configured.");
        }

        RequestUri = new Uri(HttpClient.BaseAddress, endpoint);
    }

    public void SetRequestUri(Uri requestUri)
    {
        RequestUri = requestUri ?? throw new ArgumentNullException(nameof(requestUri));
    }
}

