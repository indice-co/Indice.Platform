using Indice.Features.Agents.Core.Payments;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Tests;

public class VivaPaymentsTests
{
    [Theory]
    [InlineData("viva", true)]
    [InlineData(" Stripe ", true)]
    [InlineData("Paypal", false)]
    [InlineData(null, false)]
    public void PaymentsOptions_IsAllowed_IsCaseInsensitive(string? provider, bool expected) {
        var options = new PaymentsOptions { AllowedProviders = [PaymentProviders.Stripe, PaymentProviders.Viva] };

        Assert.Equal(expected, options.IsAllowed(provider));
    }

    [Theory]
    [InlineData("F", PaymentAuthorizationStatus.Succeeded)]
    [InlineData("E", PaymentAuthorizationStatus.Failed)]
    [InlineData("X", PaymentAuthorizationStatus.Cancelled)]
    [InlineData("A", null)]
    public void MapStatus_MapsVivaStatuses(string statusId, PaymentAuthorizationStatus? expected) {
        Assert.Equal(expected, VivaPaymentsService.MapStatus(new VivaTransaction { StatusId = statusId }));
    }

    [Fact]
    public void TryReadEvent_ReadsTransactionPayload() {
        const string json = """{ "EventTypeId": 1796, "EventData": { "TransactionId": "abc-123", "MerchantTrns": "ref" } }""";

        Assert.True(VivaPaymentsService.TryReadEvent(json, out var type, out var transactionId, out var merchantTrns));
        Assert.Equal(1796, type);
        Assert.Equal("abc-123", transactionId);
        Assert.Equal("ref", merchantTrns);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{ "EventTypeId": 1796 }""")]
    public void TryReadEvent_RejectsMalformedPayload(string json) {
        Assert.False(VivaPaymentsService.TryReadEvent(json, out _, out _, out _));
    }
}
