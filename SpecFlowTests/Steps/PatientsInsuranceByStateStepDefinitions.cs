using System.Text.RegularExpressions;
using FluentAssertions;
using SpecFlowTests.Support;
using TechTalk.SpecFlow;

namespace SpecFlowTests.Steps;

[Binding]
public sealed class PatientsInsuranceByStateStepDefinitions
{
    private readonly ScenarioContext _scenarioContext;

    public PatientsInsuranceByStateStepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    private InsuranceByStateContext InsuranceContext => _scenarioContext.Get<InsuranceByStateContext>();
    private TestContext TestContext => _scenarioContext.Get<TestContext>();

    [When(@"I build the insurance by state URL for state ""([^""]+)"" with contractedOnly ""([^""]+)""")]
    public void WhenIBuildTheInsuranceByStateUrl(string state, string contractedOnly)
    {
        var endpoint = $"/api/patients/insurance/bystate/{state}";
        var query = $"contractedOnly={contractedOnly}";

        var baseAddress = TestContext.HttpClient.BaseAddress;
        baseAddress.Should().NotBeNull("the HTTP client must have a base address to build the insurance by state URL");

        var builder = new UriBuilder(baseAddress!)
        {
            Path = endpoint,
            Query = query
        };

        TestContext.SetRequestUri(builder.Uri);
        InsuranceContext.QueryString = builder.Uri.Query;
    }

    [Then(@"the insurance by state request should target state ""([^""]+)""")]
    public void ThenTheInsuranceByStateRequestShouldTargetState(string state)
    {
        var requestUri = TestContext.RequestUri;
        requestUri.Should().NotBeNull("the insurance URL should be set before validation");
        requestUri!.AbsolutePath.Should().Be($"/api/patients/insurance/bystate/{state}");
    }

    [Then(@"the insurance by state query should be ""([^""]+)""")]
    public void ThenTheInsuranceByStateQueryShouldBe(string expectedQuery)
    {
        InsuranceContext.QueryString.Should().Be(expectedQuery, "the insurance URL should include the expected query string");
    }

    [Then(@"the insurance by state state code ""([^""]+)"" should be valid")]
    public void ThenTheInsuranceByStateStateCodeShouldBeValid(string state)
    {
        Regex.IsMatch(state, @"^[A-Z]{2}$").Should().BeTrue($"state code '{state}' should be two uppercase letters");
    }
}

