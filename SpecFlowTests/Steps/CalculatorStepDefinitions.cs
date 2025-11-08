using System.Text.Json.Nodes;
using FluentAssertions;
using SpecFlowTests.Drivers;
using SpecFlowTests.Support;
using TechTalk.SpecFlow;

namespace SpecFlowTests.Steps;

[Binding]
public sealed class ApiStepDefinitions
{
    private readonly ScenarioContext _scenarioContext;

    public ApiStepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    private TestContext TestContext => _scenarioContext.Get<TestContext>();

    [Given(@"the API base URL is configured")]
    public void GivenTheApiBaseUrlIsConfigured()
    {
        var baseUrl = TestContext.HttpClient.BaseAddress?.ToString();
        baseUrl.Should().NotBeNullOrWhiteSpace("an API base URL must be configured before requests are sent");
    }

    [Given(@"the following default headers are applied:")]
    public void GivenTheFollowingDefaultHeadersAreApplied(Table table)
    {
        foreach (var row in table.Rows)
        {
            var header = row["Header"];
            var value = row["Value"];

            if (TestContext.HttpClient.DefaultRequestHeaders.Contains(header))
            {
                TestContext.HttpClient.DefaultRequestHeaders.Remove(header);
            }

            TestContext.HttpClient.DefaultRequestHeaders.Add(header, value);
        }
    }

    [When(@"I send a GET request to ""([^""]+)""")]
    public async Task WhenISendAGetRequestToAsync(string endpoint)
    {
        var driver = new ApiDriver(TestContext);
        await driver.SendGetRequestAsync(NormalizeEndpoint(endpoint)).ConfigureAwait(false);
    }

    [When(@"I send a POST request to ""([^""]+)"" with body:")]
    public async Task WhenISendAPostRequestToWithBodyAsync(string endpoint, string body)
    {
        var driver = new ApiDriver(TestContext);
        await driver.SendPostRequestAsync(NormalizeEndpoint(endpoint), NormalizeBody(body)).ConfigureAwait(false);
    }

    [When(@"I send a PUT request to ""([^""]+)"" with body:")]
    public async Task WhenISendAPutRequestToWithBodyAsync(string endpoint, string body)
    {
        var driver = new ApiDriver(TestContext);
        await driver.SendPutRequestAsync(NormalizeEndpoint(endpoint), NormalizeBody(body)).ConfigureAwait(false);
    }

    [Then(@"the response status code should be (\d+)")]
    public void ThenTheResponseStatusCodeShouldBe(int expectedStatus)
    {
        TestContext.Response.Should().NotBeNull("a response should be captured before performing assertions");
        ((int)TestContext.Response!.StatusCode).Should().Be(expectedStatus);
    }

    [Then(@"the response header ""([^""]+)"" should contain ""([^""]+)""")]
    public void ThenTheResponseHeaderShouldContain(string headerName, string expectedValue)
    {
        TestContext.Response.Should().NotBeNull();
        TestContext.Response!.Headers.TryGetValues(headerName, out var values).Should().BeTrue($"header '{headerName}' should be present");
        values!.Should().Contain(v => v.Contains(expectedValue, StringComparison.OrdinalIgnoreCase));
    }

    [Then(@"the response body should not be empty")]
    public void ThenTheResponseBodyShouldNotBeEmpty()
    {
        TestContext.JsonBody.Should().NotBeNull("a JSON body is expected in the response");
    }

    [Then(@"the response json field ""([^""]+)"" should equal ""([^""]+)""")]
    public void ThenTheResponseJsonFieldShouldEqual(string jsonPath, string expectedValue)
    {
        TestContext.JsonBody.Should().NotBeNull("a JSON body is expected in the response");

        var node = ResolveJsonNode(TestContext.JsonBody!, jsonPath);
        node.Should().NotBeNull($"JSON field '{jsonPath}' should exist in the response");
        node!.ToString().Should().Be(expectedValue);
    }

    [Then(@"the response json field ""([^""]+)"" should be truthy")]
    public void ThenTheResponseJsonFieldShouldBeTruthy(string jsonPath)
    {
        TestContext.JsonBody.Should().NotBeNull("a JSON body is expected in the response");

        var node = ResolveJsonNode(TestContext.JsonBody!, jsonPath);
        node.Should().NotBeNull($"JSON field '{jsonPath}' should exist in the response");

        var value = node switch
        {
            JsonValue jsonValue when jsonValue.TryGetValue<bool>(out var boolResult) => boolResult,
            JsonValue jsonValue when jsonValue.TryGetValue<long>(out var longResult) => longResult != 0,
            JsonValue jsonValue when jsonValue.TryGetValue<string>(out var stringResult) => !string.IsNullOrWhiteSpace(stringResult),
            JsonArray array => array.Count > 0,
            JsonObject obj => obj.Count > 0,
            _ => node!.ToString() is { Length: > 0 }
        };

        value.Should().BeTrue($"JSON field '{jsonPath}' should resolve to a truthy value");
    }

    private static string NormalizeEndpoint(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new ArgumentException("Endpoint cannot be null or whitespace.", nameof(endpoint));
        }

        if (Uri.TryCreate(endpoint, UriKind.Absolute, out _))
        {
            return endpoint;
        }

        var trimmed = endpoint.Trim();
        return trimmed.StartsWith("/", StringComparison.Ordinal) ? trimmed : "/" + trimmed;
    }

    private static string NormalizeBody(string body)
    {
        return string.IsNullOrWhiteSpace(body) ? "{}" : body.Trim();
    }

    private static JsonNode? ResolveJsonNode(JsonNode? root, string path)
    {
        if (root is null || string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var current = root;
        foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            current = current switch
            {
                JsonObject obj when obj.TryGetPropertyValue(segment, out var result) => result,
                JsonArray array when int.TryParse(segment, out var index) && index >= 0 && index < array.Count => array[index],
                _ => null
            };

            if (current is null)
            {
                return null;
            }
        }

        return current;
    }
}
