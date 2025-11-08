using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using NUnit.Framework;
using SpecFlowTests.Support;
using VaxCareApiTests.Models;
using TestContext = SpecFlowTests.Support.TestContext;

namespace SpecFlowTests.Drivers;

public class AppointmentCreationDriver
{
    private const string AppointmentEndpoint = "/api/patients/appointment";
    private readonly TestContext _testContext;
    private readonly AppointmentCreationContext _creationContext;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public AppointmentCreationDriver(TestContext testContext, AppointmentCreationContext creationContext)
    {
        _testContext = testContext;
        _creationContext = creationContext;
    }

    public void PrepareDefaultPayload(bool ensureUniqueLastName = true)
    {
        var patient = TestPatients.RiskFreePatientForCheckout.Create();
        var lastName = ensureUniqueLastName
            ? $"Patient{DateTime.UtcNow:yyyyMMddHHmmssfff}{Random.Shared.Next(100, 999)}"
            : patient.LastName;

        _creationContext.GeneratedLastName = lastName;

        var payload = new
        {
            newPatient = new
            {
                firstName = "Test",
                lastName,
                dob = "1990-07-07T00:00:00Z",
                gender = 0,
                phoneNumber = "5555555555",
                paymentInformation = new
                {
                    primaryInsuranceId = 12,
                    paymentMode = "InsurancePay",
                    primaryMemberId = "",
                    primaryGroupId = "",
                    relationshipToInsured = "Self",
                    insuranceName = "Cigna",
                    mbi = "",
                    stock = "Private"
                },
                ssn = ""
            },
            clinicId = 10808,
            date = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            providerId = 100001877,
            initialPaymentMode = "InsurancePay",
            visitType = "Well"
        };

        _creationContext.RequestBody = JsonSerializer.Serialize(payload, _serializerOptions);
    }

    public async Task SubmitAppointmentAsync()
    {
        var requestBody = _creationContext.RequestBody;
        requestBody.Should().NotBeNullOrWhiteSpace("appointment creation payload must be prepared before submitting");

        var endpoint = AppointmentEndpoint;
        _testContext.SetRequestUri(endpoint);

        try
        {
            using var content = new StringContent(requestBody!, Encoding.UTF8, "application/json");
            var response = await _testContext.HttpClient.PostAsync(endpoint, content).ConfigureAwait(false);
            await _testContext.CaptureResponseAsync(response).ConfigureAwait(false);

            _creationContext.ResponseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            _creationContext.AppointmentId = ExtractAppointmentId(_creationContext.ResponseBody);
        }
        catch (HttpRequestException ex) when (IsNetworkConnectivityIssue(ex))
        {
            Assert.Inconclusive($"Network connectivity issue during appointment creation: {ex.Message}");
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            Assert.Inconclusive($"Appointment creation request timed out: {ex.Message}");
        }
    }

    public string? GetAppointmentId() => _creationContext.AppointmentId;

    public string? GetResponseBody() => _creationContext.ResponseBody;

    private static string? ExtractAppointmentId(string? responseContent)
    {
        if (string.IsNullOrWhiteSpace(responseContent))
        {
            return null;
        }

        var trimmed = responseContent.Trim();
        if (long.TryParse(trimmed, out var numericId))
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

            if (json["appointmentId"] is JsonValue appointmentId && appointmentId.TryGetValue<string>(out var stringId))
            {
                return stringId;
            }

            if (json["appointment_id"] is JsonValue snakeCase && snakeCase.TryGetValue<string>(out var snakeString))
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
}

