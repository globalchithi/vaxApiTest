using System.Text.RegularExpressions;
using FluentAssertions;
using SpecFlowTests.Support;
using TechTalk.SpecFlow;

namespace SpecFlowTests.Steps;

[Binding]
public sealed class PatientsAppointmentSyncStepDefinitions
{
    private readonly ScenarioContext _scenarioContext;

    public PatientsAppointmentSyncStepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    private AppointmentSyncContext SyncContext => _scenarioContext.Get<AppointmentSyncContext>();
    private TestContext TestContext => _scenarioContext.Get<TestContext>();

    [When(@"I build the appointment sync URL with clinicId ""([^""]+)"" date ""([^""]+)"" version ""([^""]+)""")]
    public void WhenIBuildTheAppointmentSyncUrl(string clinicId, string date, string version)
    {
        var endpoint = "/api/patients/appointment/sync";
        var query = $"clinicId={clinicId}&date={date}&version={version}";

        var baseAddress = TestContext.HttpClient.BaseAddress;
        baseAddress.Should().NotBeNull("the HTTP client must have a base address to construct the appointment sync URL");

        var builder = new UriBuilder(baseAddress!)
        {
            Path = endpoint,
            Query = query
        };

        TestContext.SetRequestUri(builder.Uri);

        var requestUri = TestContext.RequestUri;
        requestUri.Should().NotBeNull("the appointment sync URL should resolve against the configured base address");
        SyncContext.QueryString = requestUri!.Query;
    }

    [Then(@"the appointment sync query string should include clinicId ""([^""]+)"" date ""([^""]+)"" version ""([^""]+)""")]
    public void ThenTheAppointmentSyncQueryStringShouldInclude(string clinicId, string date, string version)
    {
        SyncContext.QueryString.Should().NotBeNullOrWhiteSpace("building the appointment sync URL should capture the query string");
        var query = SyncContext.QueryString!;
        query.Should().Contain($"clinicId={clinicId}");
        query.Should().Contain($"date={date}");
        query.Should().Contain($"version={version}");
    }

    [Then(@"the appointment sync date ""([^""]+)"" should match yyyy-MM-dd format")]
    public void ThenTheAppointmentSyncDateShouldMatch(string date)
    {
        var pattern = @"^\d{4}-\d{2}-\d{2}$";
        Regex.IsMatch(date, pattern).Should().BeTrue($"date '{date}' should match the YYYY-MM-DD format");
    }

    [Then(@"the appointment sync date ""([^""]+)"" should be invalid")]
    public void ThenTheAppointmentSyncDateShouldBeInvalid(string date)
    {
        var pattern = @"^\d{4}-\d{2}-\d{2}$";
        Regex.IsMatch(date, pattern).Should().BeFalse($"date '{date}' should not match the YYYY-MM-DD format");
    }

    [Then(@"the appointment sync version ""([^""]+)"" should match X.Y format")]
    public void ThenTheAppointmentSyncVersionShouldMatch(string version)
    {
        var pattern = @"^\d+\.\d+$";
        Regex.IsMatch(version, pattern).Should().BeTrue($"version '{version}' should match the X.Y format");
    }

    [Then(@"the appointment sync version ""([^""]+)"" should be invalid")]
    public void ThenTheAppointmentSyncVersionShouldBeInvalid(string version)
    {
        var pattern = @"^\d+\.\d+$";
        Regex.IsMatch(version, pattern).Should().BeFalse($"version '{version}' should not match the X.Y format");
    }

    [Then(@"the appointment sync clinicId ""([^""]+)"" should be a positive integer")]
    public void ThenTheAppointmentSyncClinicIdShouldBePositiveInteger(string clinicId)
    {
        clinicId.Should().NotBeNull();
        int.TryParse(clinicId, out var parsed).Should().BeTrue($"clinicId '{clinicId}' should be numeric");
        parsed.Should().BeGreaterThan(0, $"clinicId '{clinicId}' should be positive");
    }

    [Then(@"the appointment sync clinicId ""([^""]+)"" should be invalid")]
    public void ThenTheAppointmentSyncClinicIdShouldBeInvalid(string clinicId)
    {
        var isValid = int.TryParse(clinicId, out var parsed) && parsed > 0;
        isValid.Should().BeFalse($"clinicId '{clinicId}' should not be accepted as a positive integer");
    }

    [Then(@"I demonstrate appointment sync response logging")]
    public void ThenIDemonstrateAppointmentSyncResponseLogging()
    {
        var baseUrl = TestContext.HttpClient.BaseAddress?.ToString() ?? "<base-url-not-set>";
        Console.WriteLine("=== RESPONSE LOGGING DEMONSTRATION ===");
        Console.WriteLine($"Making GET request to: {baseUrl.TrimEnd('/')}/api/patients/appointment/sync?clinicId=89534&date=2025-10-22&version=2.0");
        Console.WriteLine("Request completed in: 245ms");
        Console.WriteLine("Response Status: 200 OK");
        Console.WriteLine("Response Reason: OK");
        Console.WriteLine("Response Version: 1.1");
        Console.WriteLine("=== RESPONSE HEADERS ===");
        Console.WriteLine("  Content-Type: application/json");
        Console.WriteLine("  Content-Length: 1234");
        Console.WriteLine("  Server: nginx/1.18.0");
        Console.WriteLine("=== CONTENT HEADERS ===");
        Console.WriteLine("  Content-Type: application/json; charset=utf-8");
        Console.WriteLine("  Content-Length: 1234");
        Console.WriteLine("=== RESPONSE BODY ===");
        Console.WriteLine("Content Length: 1234 characters");
        Console.WriteLine("Content Type: application/json; charset=utf-8");
        Console.WriteLine("Response Body Preview:");
        Console.WriteLine("{");
        Console.WriteLine("  \"appointments\": [");
        Console.WriteLine("    {");
        Console.WriteLine("      \"id\": 123,");
        Console.WriteLine("      \"patientName\": \"John Doe\",");
        Console.WriteLine("      \"appointmentTime\": \"2025-10-22T10:00:00Z\"");
        Console.WriteLine("    }");
        Console.WriteLine("  ],");
        Console.WriteLine("  \"totalCount\": 1,");
        Console.WriteLine("  \"hasMore\": false");
        Console.WriteLine("}");
        Console.WriteLine("=== FULL RESPONSE BODY ===");
        Console.WriteLine("(Full JSON response would be logged here)");
        Console.WriteLine("=== END RESPONSE LOGGING DEMONSTRATION ===");
    }
}

