using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq.Protected;
using Moq;
using System.Text;

namespace Indice.Services.Tests;

[Trait("Services", "Brevo")]
public class BrevoServiceTests
{
    [Fact]
    public async Task SendAsync_Succeeds() {
        // Arrange
        var mockSettings = new Mock<IOptionsSnapshot<EmailServiceBrevoSettings>>();
        var mockHtmlRenderingEngine = new Mock<IHtmlRenderingEngine>();

        mockSettings.Setup(x => x.Value)
            .Returns(new EmailServiceBrevoSettings {
                Sender = "noreply@indice.gr",
                SenderName = "INDICE",
                ApiKey = Guid.NewGuid().ToString()
            });


        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        httpMessageHandlerMock
            .Protected() // <= here is the trick to set up protected!!!
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage());

        var service = new EmailServiceBrevo(
            mockSettings.Object,
            new HttpClient(httpMessageHandlerMock.Object),
            mockHtmlRenderingEngine.Object
        );

        // Act
        await service.SendAsync([ "user@indice.gr" ], "Email Subject", "This is the body");

        // Assert
        Assert.True(true); // actually, no exception means that the test has passed
    }

    [Fact]
    public async Task WebhookHandler_Parses_BrevoEmailLifecyclePayloads() {
        // Arrange
        var settings = Options.Create(new EmailServiceBrevoSettings {
            WebhookUsername = "user",
            WebhookPassword = "pass"
        });
        var sut = new BrevoWebhookHttpHandler(settings);

        var payload = """
        [
          {
            "id": 2636078,
            "email": "angelosvraimakis@gmail.com",
            "message-id": "<202610261127.96771796084@smtp-relay.mailin.fr>",
            "date": "2026-10-26 13:27:36",
            "event": "request",
            "subject": "Subject Line",
            "sending_ip": "77.32.148.25",
            "ts_event": 1793014056,
            "ts": 1793014056,
            "reason": "sent",
            "ts_epoch": 1793014056520,
            "uuid": "24f35486-8384-4129-88c5-0de983e3b841",
            "source": "API"
          },
          {
            "id": 2636078,
            "email": "angelosvraimakis@gmail.com",
            "message-id": "<202610261127.96771796084@smtp-relay.mailin.fr>",
            "date": "2026-10-26 13:27:37",
            "event": "delivered",
            "subject": "Subject Line",
            "sending_ip": "77.32.148.25",
            "ts_event": 1793014057,
            "ts": 1793014057,
            "reason": "sent",
            "ts_epoch": 1793014057000,
            "uuid": "24f35486-8384-4129-88c5-0de983e3b841",
            "source": "API"
          },
          {
            "id": 2636078,
            "email": "angelosvraimakis@gmail.com",
            "message-id": "<202610261127.96771796084@smtp-relay.mailin.fr>",
            "date": "2026-10-26 13:29:16",
            "event": "unique_opened",
            "subject": "Subject Line",
            "sending_ip": "66.249.81.75",
            "ts": 1793014156,
            "ts_epoch": 1793014156083,
            "ts_event": 1793014156,
            "link": "",
            "user_agent": "Mozilla/5.0 (Windows NT 5.1; rv:11.0) Gecko Firefox/11.0 (via ggpht.com GoogleImageProxy)",
            "device_used": "DESKTOP",
            "uuid": "24f35486-8384-4129-88c5-0de983e3b841",
            "source": "API"
          }
        ]
        """;

        using var body = new MemoryStream(Encoding.UTF8.GetBytes(payload));

        // Act
        var result = await sut.ParseAsync(
            CreateHeaders("user", "pass"),
            Enumerable.Empty<KeyValuePair<string, StringValues>>(),
            body,
            CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3, result.Events.Count);

        Assert.Collection(result.Events,
            first => {
                Assert.Equal("request", first.EventType);
                Assert.Equal("<202610261127.96771796084@smtp-relay.mailin.fr>", first.MessageId);
                Assert.Equal("angelosvraimakis@gmail.com", first.Recipient);
                Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1793014056520), first.Timestamp);
            },
            second => {
                Assert.Equal("delivered", second.EventType);
                Assert.Equal("<202610261127.96771796084@smtp-relay.mailin.fr>", second.MessageId);
                Assert.Equal("angelosvraimakis@gmail.com", second.Recipient);
                Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1793014057000), second.Timestamp);
            },
            third => {
                Assert.Equal("unique_opened", third.EventType);
                Assert.Equal("<202610261127.96771796084@smtp-relay.mailin.fr>", third.MessageId);
                Assert.Equal("angelosvraimakis@gmail.com", third.Recipient);
                Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1793014156083), third.Timestamp);
            });

        Assert.Equal(3, result.Events.Select(x => x.EventId).Distinct().Count());
    }

    private static IDictionary<string, StringValues> CreateHeaders(string username, string password) {
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        return new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase) {
            ["Authorization"] = $"Basic {token}"
        };
    }
}
