using System.Text;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;

namespace Indice.Services.Tests;

public sealed class SendGridWebhookAuthenticationTests
{
    private static readonly byte[] PrivateKey = CreatePrivateKey();
    private static readonly string PublicKey = CreatePublicKey(PrivateKey);

    private const string Timestamp = "1794315393";

    private const string Payload = """
[
  {
    "email":"example@test.com",
    "timestamp":1795424071,
    "smtp-id":"<pvtnugixSticlBD8NlXOiw@geopod-ismtpd-9>",
    "event":"bounce",
    "sg_event_id":"UUztscodZbFEc5h_uG1Xzg==",
    "sg_message_id":"pvtnugixSticlBD8NlXOiw.recvd-b4f458cf6-zmm4-1-6B03FF45-F.0",
    "response":"550 5.7.26 Unauthenticated email from indice.gr is not accepted due to domain's DMARC policy"
  },
  {
    "email":"example@test.com",
    "timestamp":1795424069,
    "smtp-id":"<pvtnugixSticlBD8NlXOiw@geopod-ismtpd-9>",
    "event":"processed",
    "sg_event_id":"ijmfhV44GX_kTxJh_ylK7g==",
    "sg_message_id":"pvtnugixSticlBD8NlXOiw.recvd-b4f458cf6-zmm4-1-6B03FF45-F.0"
  }
]
""";

    [Fact]
    public async Task MissingSignature_IsRejected_WhenVerificationKeyIsConfigured() {
        var result = await ParseAsync(new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase), PublicKey);

        Assert.False(result.Success);
        Assert.Equal(403, result.Status);
    }

    [Fact]
    public async Task MissingSignature_IsAccepted_WhenVerificationKeyIsNotConfigured() {
        var result = await ParseAsync(new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase), webhookVerificationKey: null);

        Assert.True(result.Success);
        Assert.Equal(2, result.Events.Count);
    }

    [Fact]
    public async Task ValidSignature_IsAccepted_AndEventsAreParsed() {
        var headers = CreateSignedHeaders(Payload, Timestamp);

        var result = await ParseAsync(headers, PublicKey);

        Assert.True(result.Success);
        Assert.Equal(2, result.Events.Count);
        Assert.All(result.Events, webhookEvent => Assert.Equal(EmailServiceSendGrid.ServiceName, webhookEvent.Provider));
        Assert.Equal("bounce", result.Events[0].EventType);
        Assert.Equal("UUztscodZbFEc5h_uG1Xzg==", result.Events[0].EventId);
        Assert.Equal("pvtnugixSticlBD8NlXOiw", result.Events[0].MessageId);
        Assert.Equal("example@test.com", result.Events[0].Recipient);
        Assert.Equal("processed", result.Events[1].EventType);
        Assert.Equal("ijmfhV44GX_kTxJh_ylK7g==", result.Events[1].EventId);
        Assert.Equal("pvtnugixSticlBD8NlXOiw", result.Events[1].MessageId);
    }

    [Fact]
    public async Task SmtpIdFallback_UsesNormalizedCorrelationKey() {
        const string payload = """
            [{"email":"example@test.com","timestamp":1795424071,"smtp-id":"<pvtnugixSticlBD8NlXOiw@geopod-ismtpd-9>","event":"bounce"}]
            """;

        var result = await ParseAsync(new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase), webhookVerificationKey: null, payload);

        Assert.True(result.Success);
        var webhookEvent = Assert.Single(result.Events);
        Assert.Equal("pvtnugixSticlBD8NlXOiw", webhookEvent.MessageId);
    }

    [Fact]
    public async Task InvalidSignature_IsRejected() {
        var headers = CreateSignedHeaders(Payload, Timestamp);
        headers["x-twilio-email-event-webhook-signature"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        var result = await ParseAsync(headers, PublicKey);

        Assert.False(result.Success);
        Assert.Equal(403, result.Status);
    }

    [Fact]
    public async Task WrongVerificationKey_IsRejected() {
        var headers = CreateSignedHeaders(Payload, Timestamp);
        var wrongPublicKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        var result = await ParseAsync(headers, wrongPublicKey);

        Assert.False(result.Success);
        Assert.Equal(403, result.Status);
    }

    private static Dictionary<string, StringValues> CreateSignedHeaders(string payload, string timestamp) {
        var signedData = Encoding.UTF8.GetBytes(timestamp + payload);
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportPkcs8PrivateKey(PrivateKey, out _);
        var signature = ecdsa.SignData(signedData, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);

        return new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase) {
            ["x-twilio-email-event-webhook-timestamp"] = timestamp,
            ["x-twilio-email-event-webhook-signature"] = Convert.ToBase64String(signature)
        };
    }

    private static byte[] CreatePrivateKey() {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return ecdsa.ExportPkcs8PrivateKey();
    }

    private static string CreatePublicKey(byte[] privateKey) {
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportPkcs8PrivateKey(privateKey, out _);
        return Convert.ToBase64String(ecdsa.ExportSubjectPublicKeyInfo());
    }

    private static async Task<WebhookHttpResult> ParseAsync(
        Dictionary<string, StringValues> requestHeaders,
        string? webhookVerificationKey,
        string payload = Payload) {
        var options = new Mock<IOptions<EmailServiceSendGridSettings>>();
        options.Setup(x => x.Value).Returns(new EmailServiceSendGridSettings {
            WebhookVerificationKey = webhookVerificationKey
        });

        var handler = new SendGridWebhookHttpHandler(options.Object);
        using var body = new MemoryStream(Encoding.UTF8.GetBytes(payload));

        return await handler.ParseAsync(requestHeaders, Enumerable.Empty<KeyValuePair<string, StringValues>>(), body, TestContext.Current.CancellationToken);
    }
}
