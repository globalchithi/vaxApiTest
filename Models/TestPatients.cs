using System;

namespace VaxCareApiTests.Models
{
    public class TestPatients
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string EligibilityMessage { get; set; } = "";
        public int Age { get; set; }
        public int? PrimaryInsuranceId { get; set; }
        public string Gender { get; set; } = "";
        public string? PrimaryMemberId { get; set; }
        public int? ClinicId { get; set; }
        public string? PrimaryGroupId { get; set; }
        public string? Stock { get; set; }
        public string? PaymentMode { get; set; }
        public string? Ssn { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string CompletePatientName { get; set; } = "";

        public TestPatients(string firstName, string lastName, string eligibilityMessage, int age, 
            int? primaryInsuranceId = null, string gender = "Male", string? primaryMemberId = null, 
            int? clinicId = null, string? primaryGroupId = null, string? stock = null, 
            string? paymentMode = null, string? ssn = null)
        {
            FirstName = firstName;
            LastName = lastName + "_" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString().Substring(4, 6);
            EligibilityMessage = eligibilityMessage;
            Age = age;
            PrimaryInsuranceId = primaryInsuranceId;
            Gender = gender;
            PrimaryMemberId = primaryMemberId;
            ClinicId = clinicId;
            PrimaryGroupId = primaryGroupId;
            Stock = stock;
            PaymentMode = paymentMode;
            Ssn = ssn;
            DateOfBirth = DateTime.Now.AddYears(-age);
            CompletePatientName = $"{LastName}, {FirstName}";
        }

        public static class RiskFreePatientForCheckout
        {
            public static TestPatients Create()
            {
                return new TestPatients(
                    firstName: "Tammy",
                    lastName: "RiskFree",
                    eligibilityMessage: "Guaranteed Payment",
                    gender: "Female",
                    age: 40,
                    primaryInsuranceId: 1000023151,
                    primaryMemberId: "abc123"
                );
            }
        }

        public static class RiskFreePatientForEditCheckout
        {
            public static TestPatients Create()
            {
                return new TestPatients(
                    firstName: "Sharon",
                    lastName: "RiskFree",
                    eligibilityMessage: "Guaranteed Payment",
                    age: 40,
                    gender: "Female",
                    primaryInsuranceId: 1000023151,
                    primaryMemberId: "abc123"
                );
            }
        }

        public static class MedDPatientForCopayRequired
        {
            public static TestPatients Create()
            {
                return new TestPatients(
                    firstName: "Test",
                    lastName: "TestStatus1",
                    eligibilityMessage: "Guaranteed Payment",
                    age: 65,
                    gender: "Male",
                    primaryInsuranceId: 7,
                    primaryMemberId: "EG4TE5MK73"
                );
            }
        }

        public static class MedDWithSsnPatientForCopayRequired
        {
            public static TestPatients Create()
            {
                return new TestPatients(
                    firstName: "MedD",
                    lastName: "Eligible",
                    eligibilityMessage: "Guaranteed Payment",
                    age: 65,
                    gender: "Male",
                    primaryInsuranceId: 7,
                    primaryMemberId: "EG4TE5MK73",
                    ssn: "123121234"
                );
            }
        }

        public static class RiskFreePatientForCreatePatient
        {
            public static TestPatients Create()
            {
                return new TestPatients(
                    firstName: "Tammy",
                    lastName: "RiskFree",
                    eligibilityMessage: "Guaranteed Payment",
                    age: 10,
                    gender: "Female",
                    primaryInsuranceId: 1000023151,
                    primaryMemberId: "abc123"
                );
            }
        }

        public static class QaRobotPatient
        {
            public static TestPatients Create()
            {
                return new TestPatients(
                    firstName: "Mayah",
                    lastName: "Miller",
                    eligibilityMessage: "Guaranteed Payment",
                    age: 10,
                    gender: "Female",
                    primaryInsuranceId: 1000023151,
                    primaryMemberId: "abc123"
                );
            }
        }

        public static class PregnantPatient
        {
            public static TestPatients Create()
            {
                return new TestPatients(
                    firstName: "Mayah",
                    lastName: "Miller",
                    eligibilityMessage: "Guaranteed Payment",
                    age: 20,
                    gender: "Female",
                    primaryInsuranceId: 1000023151,
                    primaryMemberId: "abc123"
                );
            }
        }

        public static class MissingPatientWithPayerInfo
        {
            public static TestPatients Create()
            {
                return new TestPatients(
                    firstName: "Invalid-AutoTest",
                    lastName: "Payername",
                    eligibilityMessage: "New Payer Info Required",
                    age: 40,
                    gender: "Male",
                    primaryInsuranceId: 1000023151,
                    primaryMemberId: "abc123"
                );
            }
        }

        public static class MissingPatientWithAllPayerInfo
        {
            public static TestPatients Create()
            {
                return new TestPatients(
                    firstName: "Invalid",
                    lastName: "Payername",
                    eligibilityMessage: "New Payer Info Required",
                    age: 40,
                    gender: "Male",
                    primaryInsuranceId: 1000023151,
                    primaryMemberId: "abc123",
                    primaryGroupId: "abc"
                );
            }
        }

        public static class MissingPatientWithDemoInfo
        {
            public static TestPatients Create()
            {
                return new TestPatients(
                    firstName: "Invalid",
                    lastName: "Patientinfo",
                    eligibilityMessage: "Patient Info Required",
                    age: 40,
                    gender: "Male",
                    primaryInsuranceId: 2,
                    primaryMemberId: "10742845GBHZ"
                );
            }
        }

        public static class SelfPayPatient
        {
            public static TestPatients Create()
            {
                return new TestPatients(
                    firstName: "SelfPay",
                    lastName: "Patient",
                    eligibilityMessage: "Payment Required",
                    age: 40,
                    gender: "Male",
                    paymentMode: "SelfPay"
                );
            }
        }

        public static class SelfPayPatient2
        {
            public static TestPatients Create()
            {
                return new TestPatients(
                    firstName: "SelfPay",
                    lastName: "Patient",
                    eligibilityMessage: "Payment Required",
                    age: 30,
                    gender: "Male",
                    paymentMode: "SelfPay",
                    stock: "Private"
                );
            }
        }

        public static class PartnerBillPatient
        {
            public static TestPatients Create()
            {
                return new TestPatients(
                    firstName: "PB",
                    lastName: "Patient",
                    eligibilityMessage: "Ready to Vaccinate",
                    age: 40,
                    gender: "Male",
                    primaryInsuranceId: 2,
                    primaryMemberId: "10742845GBHZ",
                    paymentMode: "InsurancePay"
                );
            }
        }

        public static class VFCPatient
        {
            public static TestPatients Create()
            {
                return new TestPatients(
                    firstName: "VFC",
                    lastName: "Eligible",
                    eligibilityMessage: "Ready to Vaccinate",
                    age: 10,
                    gender: "Male",
                    primaryInsuranceId: 2,
                    primaryMemberId: "10742845GBHZ",
                    paymentMode: "NoPay"
                );
            }
        }
    }
}
