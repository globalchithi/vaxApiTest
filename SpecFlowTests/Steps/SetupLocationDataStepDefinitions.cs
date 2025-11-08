using FluentAssertions;
using SpecFlowTests.Support;
using TechTalk.SpecFlow;

namespace SpecFlowTests.Steps;

[Binding]
public sealed class SetupLocationDataStepDefinitions
{
    private readonly ScenarioContext _scenarioContext;

    public SetupLocationDataStepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    private LocationDataContext LocationContext => _scenarioContext.Get<LocationDataContext>();
    private TestContext TestContext => _scenarioContext.Get<TestContext>();

    [When(@"I build the location data URL for clinic ""([^""]+)""")]
    public void WhenIBuildTheLocationDataUrl(string clinicId)
    {
        var endpoint = "/api/setup/LocationData";
        var query = $"clinicId={clinicId}";

        var baseAddress = TestContext.HttpClient.BaseAddress;
        baseAddress.Should().NotBeNull("the HTTP client must have a base address to build the location data URL");

        var builder = new UriBuilder(baseAddress!)
        {
            Path = endpoint,
            Query = query
        };

        TestContext.SetRequestUri(builder.Uri);
        LocationContext.QueryString = builder.Uri.Query;
    }

    [Then(@"the location data request should target clinic ""([^""]+)""")]
    public void ThenTheLocationDataRequestShouldTargetClinic(string clinicId)
    {
        var requestUri = TestContext.RequestUri;
        requestUri.Should().NotBeNull("the location data URL should be set before validation");
        requestUri!.AbsolutePath.Should().Be("/api/setup/LocationData");
        requestUri.Query.Should().Contain($"clinicId={clinicId}");
    }
}

