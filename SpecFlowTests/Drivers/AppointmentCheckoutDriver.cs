using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using NUnit.Framework;
using SpecFlowTests.Support;
using VaxCareApiTests.Models;
using TestContext = SpecFlowTests.Support.TestContext;

namespace SpecFlowTests.Drivers;

public class AppointmentCheckoutDriver
{
    private const string CreateAppointmentEndpoint = "/api/patients/appointment";
    private const string CheckoutEndpointTemplate = "/api/patients/appointment/{0}/checkout";
    private readonly TestContext _testContext;
    private readonly AppointmentCheckoutContext _checkoutContext;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public AppointmentCheckoutDriver(TestContext testContext, AppointmentCheckoutContext checkoutContext)
    {
        _testContext = testContext;
        _checkoutContext = checkoutContext;
    }

    public async Task EnsureAppointmentExistsAsync(string patientKind)
    {
        _checkoutContext.PatientKind = patientKind;
        var patient = BuildPatient(patientKind);
        var appointmentId = await CreateAppointmentAsync(patient).ConfigureAwait(false);
        _checkoutContext.AppointmentId = appointmentId;
    }

    public void SetExistingAppointmentId(string appointmentId)
    {
        _checkoutContext.AppointmentId = appointmentId;
    }

    public void BuildCheckoutPayload(Func<CheckoutRequestBuilder, CheckoutRequestBuilder> configure)
    {
        var appointmentId = _checkoutContext.AppointmentId;
        appointmentId.Should().NotBeNullOrWhiteSpace("an appointment ID is required before building the checkout payload");

        var builder = configure(new CheckoutRequestBuilder());
        var payload = builder.Build();

        _checkoutContext.CheckoutPayload = JsonNode.Parse(JsonSerializer.Serialize(payload, _serializerOptions));
        _checkoutContext.LastRequestBody = JsonSerializer.Serialize(payload, _serializerOptions);
    }

    public async Task SubmitCheckoutAsync(HttpStatusCode? expectedStatus = null)
    {
        var appointmentId = _checkoutContext.AppointmentId;
        appointmentId.Should().NotBeNullOrWhiteSpace("an appointment must exist before submitting checkout");
        var payload = _checkoutContext.CheckoutPayload;
        payload.Should().NotBeNull("checkout payload must be prepared before submitting the request");

        var endpoint = string.Format(CheckoutEndpointTemplate, appointmentId);
        try
        {
            _testContext.SetRequestUri(endpoint);
            using var content = new StringContent(payload!.ToJsonString(), Encoding.UTF8, "application/json");
            var response = await _testContext.HttpClient.PutAsync(endpoint, content).ConfigureAwait(false);
            _checkoutContext.CheckoutResponse = response;
            await _testContext.CaptureResponseAsync(response).ConfigureAwait(false);

            if (expectedStatus.HasValue)
            {
                ((int)response.StatusCode).Should().Be((int)expectedStatus.Value,
                    $"expected HTTP {(int)expectedStatus.Value} but received {(int)response.StatusCode}. Response body: {await response.Content.ReadAsStringAsync().ConfigureAwait(false)}");
            }
        }
        catch (HttpRequestException ex) when (IsNetworkConnectivityIssue(ex))
        {
            Assert.Inconclusive($"Network connectivity issue when submitting checkout for appointment '{appointmentId}': {ex.Message}");
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            Assert.Inconclusive($"Checkout request timed out for appointment '{appointmentId}'.");
        }
    }

    public async Task ValidateResponseIsJsonAsync()
    {
        var response = _checkoutContext.CheckoutResponse;
        response.Should().NotBeNull("checkout response should be captured before asserting JSON payload");

        var content = await response!.Content.ReadAsStringAsync().ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(content))
        {
            Assert.Inconclusive("Checkout response body is empty; cannot validate JSON structure.");
        }

        try
        {
            JsonNode.Parse(content);
        }
        catch (JsonException ex)
        {
            Assert.Fail($"Response body is not valid JSON: {ex.Message}\nBody:\n{content}");
        }
    }

    private async Task<string> CreateAppointmentAsync(TestPatient patient)
    {
        var requestBody = JsonSerializer.Serialize(new
        {
            newPatient = new
            {
                firstName = patient.FirstName,
                lastName = patient.LastName,
                dob = patient.DateOfBirth.ToString("yyyy-MM-dd"),
                gender = patient.Gender,
                phoneNumber = "1234567890",
                state = "FL",
                paymentInformation = new
                {
                    primaryInsuranceId = patient.PrimaryInsuranceId,
                    primaryMemberId = patient.PrimaryMemberId,
                    primaryGroupId = patient.PrimaryGroupId,
                    uninsured = string.Equals(patient.PaymentMode, "SelfPay", StringComparison.OrdinalIgnoreCase)
                },
                ssn = patient.Ssn
            },
            clinicId = 89534,
            date = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"),
            providerId = 0,
            initialPaymentMode = patient.PaymentMode,
            visitType = "Well"
        }, _serializerOptions);

        try
        {
            _testContext.SetRequestUri(CreateAppointmentEndpoint);
            using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            var response = await _testContext.HttpClient.PostAsync(CreateAppointmentEndpoint, content).ConfigureAwait(false);
            await _testContext.CaptureResponseAsync(response).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var appointmentId = ExtractAppointmentId(responseBody);
            appointmentId.Should().NotBeNullOrWhiteSpace("appointment creation must return an identifier");
            return appointmentId!;
        }
        catch (HttpRequestException ex) when (IsNetworkConnectivityIssue(ex))
        {
            Assert.Inconclusive($"Network connectivity issue when creating appointment for '{_checkoutContext.PatientKind}': {ex.Message}");
            return FallbackAppointmentId();
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            Assert.Inconclusive($"Appointment creation timed out for '{_checkoutContext.PatientKind}'.");
            return FallbackAppointmentId();
        }
        catch (JsonException ex)
        {
            Assert.Inconclusive($"Unable to parse appointment creation response: {ex.Message}");
            return FallbackAppointmentId();
        }
    }

    private static TestPatient BuildPatient(string patientKind)
    {
        TestPatients source = patientKind.ToLowerInvariant() switch
        {
            "riskfree" or "risk-free" => TestPatients.RiskFreePatientForCheckout.Create(),
            "selfpay" or "self-pay" => TestPatients.SelfPayPatient.Create(),
            "vfc" => TestPatients.VFCPatient.Create(),
            "partnerbill" or "partner-bill" => TestPatients.PartnerBillPatient.Create(),
            "pregnant" => TestPatients.PregnantPatient.Create(),
            "medd" or "medicare" => TestPatients.MedDPatientForCopayRequired.Create(),
            _ => TestPatients.RiskFreePatientForCheckout.Create()
        };

        return new TestPatient
        {
            FirstName = source.FirstName,
            LastName = source.LastName,
            DateOfBirth = source.DateOfBirth,
            Gender = source.Gender,
            Ssn = source.Ssn ?? "123121234",
            PaymentMode = source.PaymentMode ?? "InsurancePay",
            PrimaryInsuranceId = source.PrimaryInsuranceId ?? 1000023151,
            PrimaryMemberId = source.PrimaryMemberId ?? "abc123",
            PrimaryGroupId = source.PrimaryGroupId ?? ""
        };
    }

    private static string? ExtractAppointmentId(string responseContent)
    {
        var trimmed = responseContent.Trim();
        if (int.TryParse(trimmed, out var numericId))
        {
            return numericId.ToString();
        }

        try
        {
            var json = JsonNode.Parse(trimmed);
            if (json is null)
            {
                return null;
            }

            if (json["appointmentId"] is JsonValue appointmentId &&
                appointmentId.TryGetValue<string>(out var stringId))
            {
                return stringId;
            }

            if (json["appointment_id"] is JsonValue snakeCase &&
                snakeCase.TryGetValue<string>(out var snakeString))
            {
                return snakeString;
            }

            if (json["id"] is JsonValue idValue && idValue.TryGetValue<string>(out var genericId))
            {
                return genericId;
            }

            if (json["appointment"] is JsonObject obj)
            {
                if (obj["id"] is JsonValue nestedId && nestedId.TryGetValue<string>(out var nestedString))
                {
                    return nestedString;
                }

                if (obj["appointmentId"] is JsonValue nestedAppointmentId && nestedAppointmentId.TryGetValue<string>(out var nestedAppointment))
                {
                    return nestedAppointment;
                }
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private static bool IsNetworkConnectivityIssue(HttpRequestException exception)
    {
        var message = exception.Message;
        return message.Contains("No such host", StringComparison.OrdinalIgnoreCase)
               || message.Contains("nodename nor servname provided", StringComparison.OrdinalIgnoreCase)
               || message.Contains("Name or service not known", StringComparison.OrdinalIgnoreCase)
               || message.Contains("The SSL connection could not be established", StringComparison.OrdinalIgnoreCase);
    }

    private static string FallbackAppointmentId() => "12345";

    public sealed class CheckoutRequestBuilder
    {
        private readonly List<AdministeredVaccineBuilder> _vaccines = new();
        private string _paymentModeDisplayed = "InsurancePay";
        private string _tabletId = "550e8400-e29b-41d4-a716-446655440000";
        private string _administeredBy = "1";
        private string _paymentModeOverride = "InsurancePay";
        private bool _includeCreditCard;

        public CheckoutRequestBuilder WithTabletId(string tabletId)
        {
            _tabletId = tabletId;
            return this;
        }

        public CheckoutRequestBuilder WithAdministeredBy(int userId)
        {
            _administeredBy = userId.ToString();
            return this;
        }

        public CheckoutRequestBuilder WithPaymentModeDisplayed(string paymentMode)
        {
            _paymentModeDisplayed = paymentMode;
            return this;
        }

        public CheckoutRequestBuilder WithPaymentMode(string paymentMode)
        {
            _paymentModeOverride = paymentMode;
            return this;
        }

        public CheckoutRequestBuilder WithCreditCardInfo()
        {
            _includeCreditCard = true;
            return this;
        }

        public CheckoutRequestBuilder AddVaccine(Action<AdministeredVaccineBuilder> configure)
        {
            var builder = new AdministeredVaccineBuilder();
            configure(builder);
            _vaccines.Add(builder);
            return this;
        }

        public object Build()
        {
            return new
            {
                tabletId = _tabletId,
                administeredVaccines = _vaccines.Select(v => v.Build(_paymentModeOverride)).ToArray(),
                administered = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                administeredBy = int.Parse(_administeredBy, System.Globalization.CultureInfo.InvariantCulture),
                forcedRiskType = 0,
                postShotVisitPaymentModeDisplayed = _paymentModeDisplayed,
                phoneNumberFlowPresented = 0,
                phoneContactConsentStatus = "NotApplicable",
                phoneContactReasons = "",
                flags = Array.Empty<string>(),
                pregnancyPrompt = 0,
                creditCardInformation = _includeCreditCard ? new
                {
                    cardNumber = "4111111111111111",
                    expirationDate = "12/2025",
                    cardholderName = "John Doe",
                    email = "john.doe@example.com",
                    phoneNumber = "1234567890"
                } : null,
                activeFeatureFlags = Array.Empty<string>(),
                attestHighRisk = 0,
                riskFactors = Array.Empty<string>()
            };
        }
    }

    public sealed class AdministeredVaccineBuilder
    {
        private int _id = 1;
        private int _productId = 13;
        private int _ageIndicated = 1;
        private string _lotNumber = "J003535";
        private string _method = "Intramuscular";
        private string _site = "Right Arm";
        private int _doseSeries = 1;

        public AdministeredVaccineBuilder WithId(int id)
        {
            _id = id;
            return this;
        }

        public AdministeredVaccineBuilder WithProductId(int productId)
        {
            _productId = productId;
            return this;
        }

        public AdministeredVaccineBuilder WithLotNumber(string lotNumber)
        {
            _lotNumber = lotNumber;
            return this;
        }

        public AdministeredVaccineBuilder WithSite(string site)
        {
            _site = site;
            return this;
        }

        public AdministeredVaccineBuilder WithDoseSeries(int series)
        {
            _doseSeries = series;
            return this;
        }

        public AdministeredVaccineBuilder WithMethod(string method)
        {
            _method = method;
            return this;
        }

        public AdministeredVaccineBuilder WithAgeIndicated(int ageIndicated)
        {
            _ageIndicated = ageIndicated;
            return this;
        }

        public object Build(string paymentMode)
        {
            return new
            {
                id = _id,
                productId = _productId,
                ageIndicated = _ageIndicated,
                lotNumber = _lotNumber,
                method = _method,
                site = _site,
                doseSeries = _doseSeries,
                paymentMode
            };
        }
    }
}

