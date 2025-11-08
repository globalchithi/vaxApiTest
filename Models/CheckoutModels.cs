using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace VaxCareApiTests.Models
{
    public class AppointmentCheckout
    {
        [JsonPropertyName("tabletId")]
        public string TabletId { get; set; } = "550e8400-e29b-41d4-a716-446655440001";

        [JsonPropertyName("administeredVaccines")]
        public List<CheckInVaccination> AdministeredVaccines { get; set; } = new List<CheckInVaccination>();

        [JsonPropertyName("administered")]
        public DateTime Administered { get; set; } = DateTime.Now;

        [JsonPropertyName("administeredBy")]
        public int AdministeredBy { get; set; } = 1;

        [JsonPropertyName("presentedRiskAssessmentId")]
        public int? PresentedRiskAssessmentId { get; set; } = null;

        [JsonPropertyName("forcedRiskType")]
        public int ForcedRiskType { get; set; } = 0;

        [JsonPropertyName("postShotVisitPaymentModeDisplayed")]
        public string PostShotVisitPaymentModeDisplayed { get; set; } = "InsurancePay";

        [JsonPropertyName("phoneNumberFlowPresented")]
        public bool PhoneNumberFlowPresented { get; set; } = false;

        [JsonPropertyName("phoneContactConsentStatus")]
        public string PhoneContactConsentStatus { get; set; } = "NOT_APPLICABLE";

        [JsonPropertyName("phoneContactReasons")]
        public string PhoneContactReasons { get; set; } = "";

        [JsonPropertyName("flags")]
        public List<string> Flags { get; set; } = new List<string>();

        [JsonPropertyName("pregnancyPrompt")]
        public bool PregnancyPrompt { get; set; } = false;

        [JsonPropertyName("weeksPregnant")]
        public int? WeeksPregnant { get; set; } = null;

        [JsonPropertyName("creditCardInformation")]
        public object? CreditCardInformation { get; set; } = null;

        [JsonPropertyName("activeFeatureFlags")]
        public List<string> ActiveFeatureFlags { get; set; } = new List<string>();

        [JsonPropertyName("attestHighRisk")]
        public bool AttestHighRisk { get; set; } = false;

        [JsonPropertyName("riskFactors")]
        public List<string> RiskFactors { get; set; } = new List<string>();
    }

    public class CheckInVaccination
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 1;

        [JsonPropertyName("productId")]
        public int ProductId { get; set; }

        [JsonPropertyName("ageIndicated")]
        public bool AgeIndicated { get; set; } = true;

        [JsonPropertyName("lotNumber")]
        public string LotNumber { get; set; } = "";

        [JsonPropertyName("method")]
        public string Method { get; set; } = "Intramuscular";

        [JsonPropertyName("site")]
        public string Site { get; set; } = "";

        [JsonPropertyName("doseSeries")]
        public int DoseSeries { get; set; } = 1;

        [JsonPropertyName("paymentMode")]
        public string PaymentMode { get; set; } = "InsurancePay";

        [JsonPropertyName("paymentModeReason")]
        public string? PaymentModeReason { get; set; } = null;
    }

    public class TestProduct
    {
        public int Id { get; set; }
        public string LotNumber { get; set; } = "";
        public string DisplayName { get; set; } = "";
    }

    public class TestSite
    {
        public string DisplayName { get; set; } = "Left Deltoid";
    }

    public class TestPatient
    {
        public string FirstName { get; set; } = "Test";
        public string LastName { get; set; } = "Patient";
        public string CompletePatientName => $"{FirstName} {LastName}";
        public DateTime DateOfBirth { get; set; } = DateTime.Now.AddYears(-30);
        public string Gender { get; set; } = "Male";
        public string Ssn { get; set; } = "123121234";
        public string PaymentMode { get; set; } = "InsurancePay";
        public int PrimaryInsuranceId { get; set; } = 1;
        public string PrimaryMemberId { get; set; } = "123456789";
        public string PrimaryGroupId { get; set; } = "GROUP123";
    }
}
