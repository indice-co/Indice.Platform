using System.Text.Json;
using Indice.Features.Agents.Core.Workflows.Steps.Operator;

namespace Indice.Features.Agents.Core.Tests;

public class StripePaymentPayloadTests
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Payload_IncludesPaymentMethodOrder() {
        var payload = new PaymentCompletedStep.StripePaymentPayload("Stripe", "C-1", "pk_test", null, ["apple_pay", "google_pay", "card"]);

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(payload, WebOptions));

        Assert.Equal("C-1", json.RootElement.GetProperty("caseId").GetString());
        Assert.Equal(["apple_pay", "google_pay", "card"], json.RootElement.GetProperty("paymentMethodOrder").EnumerateArray().Select(x => x.GetString()));
    }

    [Fact]
    public void Payload_PaymentMethodOrderIsOptional() {
        var payload = new PaymentCompletedStep.StripePaymentPayload("Stripe", "C-1", "pk_test", null);

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(payload, WebOptions));

        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("paymentMethodOrder").ValueKind);
    }
}
