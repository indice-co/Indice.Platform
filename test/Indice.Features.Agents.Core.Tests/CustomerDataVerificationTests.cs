using System.Text;
using System.Text.Json;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Workflows.Cards;
using Indice.Features.Agents.Core.Workflows.Steps.CustomerData;
using Indice.Features.Agents.Core.Workflows.Verification;

namespace Indice.Features.Agents.Core.Tests;

/// <summary>Covers the deterministic pieces of the customer data / strong verification sub-workflow.</summary>
public class CustomerDataVerificationTests
{
    private const string ServicePickupJson = """
        {
          "caseNumber": "SP-100234",
          "customer": {
            "fullName": "Maria Papadopoulou",
            "customerCode": "C-88231",
            "mobilePhone": "+30 694 1234567",
            "email": "maria@example.com"
          },
          "vehicle": { "licensePlate": "ΙΚΑ-4521", "model": "Model X" },
          "address": { "street": "Leoforos Kifisias 24", "city": "Athens", "postalCode": "11526" },
          "amountDue": 149.9
        }
        """;

    private static JsonElement Payload(string json = ServicePickupJson) => JsonDocument.Parse(json).RootElement.Clone();

    private static CustomerDataRecord Record(string referenceId = "SP-100234") =>
        new() { Reference = new ExternalReference(referenceId, "ServicePickup"), DataType = "ServicePickup", Data = Payload() };

    [Fact]
    public void ExtractsVerifiableFieldsWithTheirKinds() {
        var fields = VerifiableDataExtractor.Extract(Payload());

        var byPath = fields.ToDictionary(field => field.Path, field => field.Kind);
        Assert.Equal(VerifiableFieldKind.CaseNumber, byPath["caseNumber"]);
        Assert.Equal(VerifiableFieldKind.CustomerCode, byPath["customer.customerCode"]);
        Assert.Equal(VerifiableFieldKind.Phone, byPath["customer.mobilePhone"]);
        Assert.Equal(VerifiableFieldKind.Email, byPath["customer.email"]);
        Assert.Equal(VerifiableFieldKind.LicensePlate, byPath["vehicle.licensePlate"]);
        // Names, models and amounts are not identity evidence and must stay out of the challenge pool.
        Assert.DoesNotContain(fields, field => field.Path == "customer.fullName");
        Assert.DoesNotContain(fields, field => field.Path == "vehicle.model");
        Assert.DoesNotContain(fields, field => field.Path == "amountDue");
    }

    [Fact]
    public void ChannelsAreOnlyPhonesAndEmails() {
        var channels = VerifiableDataExtractor.Extract(Payload()).Where(field => field.IsChannel).Select(field => field.Path);

        Assert.Equal(["customer.mobilePhone", "customer.email"], channels.Order().Reverse());
    }

    [Theory]
    [InlineData(VerifiableFieldKind.Phone, "+30 694 1234567", "6941234567", true)]
    [InlineData(VerifiableFieldKind.Phone, "+30 694 1234567", "0030-694-1234567", true)]
    [InlineData(VerifiableFieldKind.Phone, "+30 694 1234567", "6941234568", false)]
    [InlineData(VerifiableFieldKind.LicensePlate, "ΙΚΑ-4521", "ika 4521", false)]
    [InlineData(VerifiableFieldKind.LicensePlate, "ABC-4521", "abc 4521", true)]
    [InlineData(VerifiableFieldKind.Email, "maria@example.com", "MARIA@example.com", true)]
    [InlineData(VerifiableFieldKind.PostalAddress, "Leoforos Kifisias 24", "kifisias 24", true)]
    [InlineData(VerifiableFieldKind.PostalAddress, "Leoforos Kifisias 24", "patision 24", false)]
    [InlineData(VerifiableFieldKind.CustomerCode, "C-88231", "my code is c88231", true)]
    [InlineData(VerifiableFieldKind.CustomerCode, "C-88231", "88232", false)]
    public void MatchesAnswersTolerantlyPerKind(VerifiableFieldKind kind, string stored, string answer, bool expected) {
        var field = new VerifiableField("field", kind, stored);

        Assert.Equal(expected, VerificationMatcher.Matches(field, answer));
    }

    [Fact]
    public void MatchReturnsTheFieldTheAnswerSatisfies() {
        var fields = VerifiableDataExtractor.Extract(Payload());

        Assert.Equal("customer.mobilePhone", VerificationMatcher.Match(fields, "you can call me at 694 1234567")?.Path);
        Assert.Null(VerificationMatcher.Match(fields, "no idea"));
        Assert.Null(VerificationMatcher.Match(fields, null));
    }

    [Fact]
    public void ChallengeExcludesWhatTheUserAlreadyProved() {
        var fields = IdentityVerifier.GetChallengeFields(Record());

        // The case number opened the conversation, so answering it back proves nothing.
        Assert.DoesNotContain(fields, field => field.Kind == VerifiableFieldKind.CaseNumber);
        Assert.Contains(fields, field => field.Path == "customer.customerCode");
    }

    [Fact]
    public void ChallengeExcludesAnyFieldEqualToTheReference() {
        var fields = IdentityVerifier.GetChallengeFields(Record(referenceId: "C-88231"));

        Assert.DoesNotContain(fields, field => field.Path == "customer.customerCode");
        Assert.Contains(fields, field => field.Path == "vehicle.licensePlate");
    }

    [Fact]
    public void MasksChannelsBeforeShowingThem() {
        var phone = ChannelMasker.MaskPhone("+30 694 1234567");
        Assert.EndsWith("4567", phone);
        Assert.DoesNotContain("694", phone);
        var email = ChannelMasker.MaskEmail("maria@example.com");
        Assert.StartsWith("m", email);
        Assert.EndsWith(".com", email);
        Assert.DoesNotContain("maria", email);
        Assert.DoesNotContain("example", email);
    }

    [Theory]
    [InlineData("my case is SP-100234 please", "SP-100234")]
    [InlineData("SP/100234", "SP/100234")]
    [InlineData("hello there", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void ExtractsTheReferenceOutOfFreeText(string? text, string? expected) =>
        Assert.Equal(expected, ReferenceCollector.TryExtractReference(text));

    [Theory]
    [InlineData("my code is 123456", "123456")]
    [InlineData("4821", "4821")]
    [InlineData("I did not get one", null)]
    [InlineData(null, null)]
    public void ExtractsTheOneTimeCodeOutOfFreeText(string? text, string? expected) =>
        Assert.Equal(expected, OtpVerifier.TryExtractCode(text));

    [Fact]
    public void RendersTheRecordAsAnHtmlCard() {
        var renderer = new HandlebarsCustomerDataCardRenderer(Path.Join(Path.GetTempPath(), $"cards-{Guid.NewGuid():N}"));

        var content = renderer.Render(Record());

        Assert.Equal("text/html", content.MediaType);
        var html = Encoding.UTF8.GetString(content.Data.Span);
        Assert.Contains("SP-100234", html);
        Assert.Contains("Maria Papadopoulou", html);
        Assert.Contains("License plate", html);
    }

    [Fact]
    public void CardEscapesPayloadMarkup() {
        var record = new CustomerDataRecord {
            Reference = new ExternalReference("SP-1", "ServicePickup"),
            DataType = "ServicePickup",
            Data = Payload("""{ "note": "<script>alert(1)</script>" }""")
        };
        var renderer = new HandlebarsCustomerDataCardRenderer(Path.Join(Path.GetTempPath(), $"cards-{Guid.NewGuid():N}"));

        var html = Encoding.UTF8.GetString(renderer.Render(record).Data.Span);

        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }
}
