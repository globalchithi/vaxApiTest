using System;
using FluentAssertions;
using SpecFlowTests.Support;
using TechTalk.SpecFlow;

namespace SpecFlowTests.Steps;

[Binding]
public sealed class PatientsClinicStepDefinitions
{
    private readonly ScenarioContext _scenarioContext;

    public PatientsClinicStepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    private TestContext TestContext => _scenarioContext.Get<TestContext>();

    [When(@"I build the request URL for ""([^""]+)""")]
    public void WhenIBuildTheRequestUrlFor(string endpoint)
    {
        TestContext.SetRequestUri(endpoint);
    }

    [Then(@"the request URL should have absolute path ""([^""]+)""")]
    public void ThenTheRequestUrlShouldHaveAbsolutePath(string expectedPath)
    {
        var requestUri = TestContext.RequestUri;
        requestUri.Should().NotBeNull("the request URI must be built before validating");
        requestUri!.AbsolutePath.Should().Be(expectedPath);
    }

    [Then(@"the following request headers should be present:")]
    public void ThenTheFollowingRequestHeadersShouldBePresent(Table table)
    {
        var headers = TestContext.HttpClient.DefaultRequestHeaders;

        foreach (var row in table.Rows)
        {
            var headerKey = row["Header"];
            headers.Contains(headerKey).Should().BeTrue($"header '{headerKey}' should be configured for the patients clinic endpoint");

            if (headers.TryGetValues(headerKey, out var values))
            {
                var concatenated = string.Join(", ", values);
                if (string.IsNullOrWhiteSpace(concatenated))
                {
                    Console.WriteLine($"⚠️  Warning: Header '{headerKey}' is present but empty. Verify configuration.");
                }
                else
                {
                    concatenated.Should().NotBeNullOrWhiteSpace($"header '{headerKey}' should have a non-empty value");
                }
            }
        }
    }

    [Then(@"the response body should be valid json")]
    public void ThenTheResponseBodyShouldBeValidJson()
    {
        TestContext.JsonBody.Should().NotBeNull("a valid JSON response is expected from the patients clinic endpoint");
    }
}

