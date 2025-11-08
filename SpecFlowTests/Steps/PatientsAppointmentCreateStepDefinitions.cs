using System.Text.RegularExpressions;
using FluentAssertions;
using SpecFlowTests.Drivers;
using SpecFlowTests.Support;
using TechTalk.SpecFlow;

namespace SpecFlowTests.Steps;

[Binding]
public sealed class PatientsAppointmentCreateStepDefinitions
{
    private readonly ScenarioContext _scenarioContext;

    public PatientsAppointmentCreateStepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    private AppointmentCreationDriver Driver => _scenarioContext.Get<AppointmentCreationDriver>();
    private AppointmentCreationContext CreationContext => _scenarioContext.Get<AppointmentCreationContext>();
    private TestContext TestContext => _scenarioContext.Get<TestContext>();

    [Given(@"a unique appointment creation payload")]
    public void GivenAUniqueAppointmentCreationPayload()
    {
        Driver.PrepareDefaultPayload(ensureUniqueLastName: true);
    }

    [When(@"I submit the appointment creation request")]
    public async Task WhenISubmitTheAppointmentCreationRequestAsync()
    {
        await Driver.SubmitAppointmentAsync().ConfigureAwait(false);
    }

    [Then(@"an appointment id should be returned")]
    public void ThenAnAppointmentIdShouldBeReturned()
    {
        var appointmentId = Driver.GetAppointmentId();
        appointmentId.Should().NotBeNullOrWhiteSpace("appointment creation should return an identifier");
        Regex.IsMatch(appointmentId!, @"^\d+$").Should().BeTrue("appointment ID should be numeric");
    }

    [Then(@"the appointment creation response should not contain ""([^""]+)""")]
    public async Task ThenTheAppointmentCreationResponseShouldNotContainAsync(string word)
    {
        var response = TestContext.Response;
        response.Should().NotBeNull("appointment creation response should be captured");
        var content = await response!.Content.ReadAsStringAsync().ConfigureAwait(false);
        content.Should().NotContain(word, $"response should not mention '{word}'");
    }

    [Then(@"the generated patient last name should be unique")]
    public void ThenTheGeneratedPatientLastNameShouldBeUnique()
    {
        CreationContext.GeneratedLastName.Should().NotBeNullOrWhiteSpace("a last name should be generated for the payload");
    }
}

