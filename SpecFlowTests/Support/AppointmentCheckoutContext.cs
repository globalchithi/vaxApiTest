using System.Text.Json.Nodes;

namespace SpecFlowTests.Support;

public class AppointmentCheckoutContext
{
    public string? AppointmentId { get; set; }
    public JsonNode? CheckoutPayload { get; set; }
    public HttpResponseMessage? CheckoutResponse { get; set; }
    public string? LastRequestBody { get; set; }
    public string? PatientKind { get; set; }
}

