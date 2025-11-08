using System.Net;
using FluentAssertions;
using SpecFlowTests.Drivers;
using SpecFlowTests.Support;
using TechTalk.SpecFlow;

namespace SpecFlowTests.Steps;

[Binding]
public sealed class PatientsAppointmentCheckoutStepDefinitions
{
    private readonly ScenarioContext _scenarioContext;

    public PatientsAppointmentCheckoutStepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    private AppointmentCheckoutContext CheckoutContext => _scenarioContext.Get<AppointmentCheckoutContext>();
    private AppointmentCheckoutDriver Driver => _scenarioContext.Get<AppointmentCheckoutDriver>();

    [Given(@"a ""([^""]+)"" patient appointment exists")]
    public async Task GivenAPatientAppointmentExistsAsync(string patientKind)
    {
        await Driver.EnsureAppointmentExistsAsync(patientKind).ConfigureAwait(false);
        CheckoutContext.AppointmentId.Should().NotBeNullOrWhiteSpace("appointment creation should provide an identifier");
    }

    [Given(@"an appointment id of ""([^""]+)""")]
    public void GivenAnAppointmentIdOf(string appointmentId)
    {
        Driver.SetExistingAppointmentId(appointmentId);
    }

    [Given(@"a checkout payload for single vaccine with payment mode ""([^""]+)""")]
    public void GivenACheckoutPayloadForSingleVaccine(string paymentMode)
    {
        Driver.BuildCheckoutPayload(builder => builder
            .WithTabletId("550e8400-e29b-41d4-a716-446655440001")
            .WithPaymentModeDisplayed(paymentMode)
            .WithPaymentMode(paymentMode)
            .AddVaccine(v => v
                .WithId(1)
                .WithProductId(13)
                .WithLotNumber("J003535")
                .WithSite("Arm - Right")
                .WithDoseSeries(1)));
    }

    [Given(@"a checkout payload with multiple vaccines")]
    public void GivenACheckoutPayloadWithMultipleVaccines()
    {
        Driver.BuildCheckoutPayload(builder => builder
            .WithTabletId("550e8400-e29b-41d4-a716-446655440002")
            .WithPaymentMode("InsurancePay")
            .AddVaccine(v => v.WithId(1).WithProductId(13).WithLotNumber("J003535").WithSite("Right Arm"))
            .AddVaccine(v => v.WithId(2).WithProductId(14).WithLotNumber("ADACEL001").WithSite("Left Arm"))
            .AddVaccine(v => v.WithId(3).WithProductId(15).WithLotNumber("PPSV23001").WithSite("Right Arm")));
    }

    [Given(@"a checkout payload with mixed dose series")]
    public void GivenACheckoutPayloadWithMixedDoseSeries()
    {
        Driver.BuildCheckoutPayload(builder => builder
            .WithTabletId("550e8400-e29b-41d4-a716-446655440009")
            .AddVaccine(v => v.WithId(1).WithDoseSeries(1))
            .AddVaccine(v => v.WithId(2).WithDoseSeries(2)));
    }

    [Given(@"a checkout payload for self-pay with credit card details")]
    public void GivenACheckoutPayloadForSelfPayWithCreditCardDetails()
    {
        Driver.BuildCheckoutPayload(builder => builder
            .WithTabletId("550e8400-e29b-41d4-a716-446655440003")
            .WithPaymentModeDisplayed("SelfPay")
            .WithPaymentMode("SelfPay")
            .WithCreditCardInfo()
            .AddVaccine(v => v.WithId(1).WithLotNumber("SELF001").WithSite("Arm - Right")));
    }

    [Given(@"a checkout payload for VFC patient")]
    public void GivenACheckoutPayloadForVfcPatient()
    {
        Driver.BuildCheckoutPayload(builder => builder
            .WithTabletId("550e8400-e29b-41d4-a716-446655440004")
            .WithPaymentModeDisplayed("NoPay")
            .WithPaymentMode("NoPay")
            .AddVaccine(v => v.WithId(1).WithProductId(13).WithLotNumber("VFC001").WithSite("Arm - Right")));
    }

    [Given(@"a checkout payload with no vaccines")]
    public void GivenACheckoutPayloadWithNoVaccines()
    {
        Driver.BuildCheckoutPayload(builder => builder
            .WithTabletId("550e8400-e29b-41d4-a716-446655440012"));
    }

    [Given(@"a checkout payload missing administered vaccines")]
    public void GivenACheckoutPayloadMissingAdministeredVaccines()
    {
        Driver.BuildCheckoutPayload(builder => builder
            .WithTabletId("550e8400-e29b-41d4-a716-446655440010")
            .WithPaymentMode("InsurancePay"));
    }

    [When(@"I submit the checkout request expecting success")]
    public async Task WhenISubmitTheCheckoutRequestExpectingSuccessAsync()
    {
        await Driver.SubmitCheckoutAsync(HttpStatusCode.OK).ConfigureAwait(false);
    }

    [When(@"I submit the checkout request")]
    public async Task WhenISubmitTheCheckoutRequestAsync()
    {
        await Driver.SubmitCheckoutAsync().ConfigureAwait(false);
    }

    [Then(@"the checkout response should be valid json")]
    public async Task ThenTheCheckoutResponseShouldBeValidJsonAsync()
    {
        await Driver.ValidateResponseIsJsonAsync().ConfigureAwait(false);
    }

    [Then(@"the checkout request should succeed")]
    public void ThenTheCheckoutRequestShouldSucceed()
    {
        var response = CheckoutContext.CheckoutResponse;
        response.Should().NotBeNull("checkout response should exist");
        response!.IsSuccessStatusCode.Should().BeTrue("checkout request is expected to succeed");
    }

    [Then(@"the checkout request status should be ""([^""]+)""")]
    public void ThenTheCheckoutRequestStatusShouldBe(string expectedStatus)
    {
        var response = CheckoutContext.CheckoutResponse;
        response.Should().NotBeNull("checkout response should exist");
        response!.StatusCode.ToString().Should().Be(expectedStatus);
    }
}

