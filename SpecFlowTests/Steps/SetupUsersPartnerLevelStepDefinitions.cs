using FluentAssertions;
using SpecFlowTests.Support;
using TechTalk.SpecFlow;

namespace SpecFlowTests.Steps;

[Binding]
public sealed class SetupUsersPartnerLevelStepDefinitions
{
    private readonly ScenarioContext _scenarioContext;

    public SetupUsersPartnerLevelStepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    private UsersPartnerLevelContext UsersContext => _scenarioContext.Get<UsersPartnerLevelContext>();
    private TestContext TestContext => _scenarioContext.Get<TestContext>();

    [When(@"I build the users partner level URL for partner ""([^""]+)""")]
    public void WhenIBuildTheUsersPartnerLevelUrl(string partnerId)
    {
        var endpoint = "/api/setup/usersPartnerLevel";
        var query = $"partnerId={partnerId}";

        var baseAddress = TestContext.HttpClient.BaseAddress;
        baseAddress.Should().NotBeNull("the HTTP client must have a base address to build the users partner level URL");

        var builder = new UriBuilder(baseAddress!)
        {
            Path = endpoint,
            Query = query
        };

        TestContext.SetRequestUri(builder.Uri);
        UsersContext.QueryString = builder.Uri.Query;
    }

    [Then(@"the users partner level request should target partner ""([^""]+)""")]
    public void ThenTheUsersPartnerLevelRequestShouldTargetPartner(string partnerId)
    {
        var requestUri = TestContext.RequestUri;
        requestUri.Should().NotBeNull("the users partner level URL should be set before validation");
        requestUri!.AbsolutePath.Should().Be("/api/setup/usersPartnerLevel");
        requestUri.Query.Should().Contain($"partnerId={partnerId}");
    }
}

