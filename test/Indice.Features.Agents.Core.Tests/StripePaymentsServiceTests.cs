using Indice.Features.Agents.Core.Data;
using Indice.Features.Agents.Core.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Stripe;

namespace Indice.Features.Agents.Core.Tests;

public class StripePaymentsServiceTests
{
    private const string UserId = "user-1";
    private const string CaseId = "C-1";
    private const string TenantId = "indice";
    private static readonly Guid ConversationId = Guid.NewGuid();

    [Fact]
    public async Task CreateOrReuseAttempt_CreatesIntentAndStoresSession() {
        var (service, db, stripe, _) = CreateService();

        var session = await service.CreateOrReuseAttemptAsync(UserId, CaseId, TenantId, 11.15m, ConversationId);

        Assert.NotNull(session);
        Assert.Equal(PaymentAuthorizationStatus.Created, session.PaymentAuthorizationStatus);
        Assert.Equal("pi_1_secret", session.PaymentSessionSecret);
        Assert.Equal(11.15m, session.Amount);
        var create = Assert.Single(stripe.Requests, x => x.Method == HttpMethod.Post && x.Path == "/v1/payment_intents");
        var createOptions = Assert.IsType<PaymentIntentCreateOptions>(create.Options);
        Assert.Equal(1115, createOptions.Amount);
        Assert.Equal("eur", createOptions.Currency);
        Assert.Equal(CaseId, createOptions.Metadata["caseId"]);
        Assert.Equal($"dex-{TenantId}-attempts:{CaseId}:1", create.RequestOptions?.IdempotencyKey);
        var stored = Assert.Single(db.PaymentSessions);
        Assert.Equal("pi_1", stored.PaymentIntentId);
        Assert.Equal(UserId, stored.UserId);
    }

    [Fact]
    public async Task CreateOrReuseAttempt_ReusesLiveAttempt_WithoutCreatingANewIntent() {
        var (service, _, stripe, _) = CreateService();
        var first = await service.CreateOrReuseAttemptAsync(UserId, CaseId, TenantId, 11.15m, ConversationId);

        var second = await service.CreateOrReuseAttemptAsync(UserId, CaseId, TenantId, 11.15m, ConversationId);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first.Id, second.Id);
        Assert.Single(stripe.Requests, x => x.Method == HttpMethod.Post && x.Path == "/v1/payment_intents");
        Assert.Single(stripe.Requests, x => x.Method == HttpMethod.Get && x.Path == "/v1/payment_intents/pi_1");
    }

    [Fact]
    public async Task CreateOrReuseAttempt_ReturnsPaidAttempt_WithoutClientSecret() {
        var (service, _, stripe, handler) = CreateService();
        await service.CreateOrReuseAttemptAsync(UserId, CaseId, TenantId, 11.15m, ConversationId);
        stripe.IntentStatus = "succeeded";

        var session = await service.CreateOrReuseAttemptAsync(UserId, CaseId, TenantId, 11.15m, ConversationId);

        Assert.NotNull(session);
        Assert.Equal(PaymentAuthorizationStatus.Succeeded, session.PaymentAuthorizationStatus);
        Assert.Null(session.PaymentSessionSecret);
        Assert.Single(stripe.Requests, x => x.Method == HttpMethod.Post && x.Path == "/v1/payment_intents");
        var change = Assert.Single(handler.Changes);
        Assert.Equal(PaymentAuthorizationStatus.Succeeded, change.PaymentAuthorizationStatus);
    }

    [Fact]
    public async Task CreateOrReuseAttempt_StartsNewAttempt_WhenPreviousWasCancelled() {
        var (service, db, stripe, _) = CreateService();
        await service.CreateOrReuseAttemptAsync(UserId, CaseId, TenantId, 11.15m, ConversationId);
        stripe.IntentStatus = "canceled";

        var session = await service.CreateOrReuseAttemptAsync(UserId, CaseId, TenantId, 11.15m, ConversationId);

        Assert.NotNull(session);
        Assert.Equal(PaymentAuthorizationStatus.Created, session.PaymentAuthorizationStatus);
        Assert.Equal("pi_2_secret", session.PaymentSessionSecret);
        var creates = stripe.Requests.Where(x => x.Method == HttpMethod.Post && x.Path == "/v1/payment_intents").ToList();
        Assert.Equal(2, creates.Count);
        Assert.Equal($"dex-{TenantId}-attempts:{CaseId}:2", creates[1].RequestOptions?.IdempotencyKey);
        Assert.Equal(2, db.PaymentSessions.Count());
    }

    [Theory]
    [InlineData("succeeded", PaymentAuthorizationStatus.Succeeded)]
    [InlineData("canceled", PaymentAuthorizationStatus.Cancelled)]
    public async Task GetCurrentAttempt_ReconcilesStatusWithStripe(string stripeStatus, PaymentAuthorizationStatus expected) {
        var (service, db, stripe, handler) = CreateService();
        await service.CreateOrReuseAttemptAsync(UserId, CaseId, TenantId, 11.15m, ConversationId);
        stripe.IntentStatus = stripeStatus;

        var session = await service.GetCurrentAttemptAsync(UserId, CaseId, TenantId);

        Assert.NotNull(session);
        Assert.Equal(expected, session.PaymentAuthorizationStatus);
        Assert.Equal(expected, Assert.Single(db.PaymentSessions).Status);
        Assert.Equal(expected, Assert.Single(handler.Changes).PaymentAuthorizationStatus);
    }

    [Theory]
    [InlineData("requires_payment_method")]
    [InlineData("processing")]
    public async Task GetCurrentAttempt_KeepsCreatedStatus_WhileIntentIsPending(string stripeStatus) {
        var (service, _, stripe, handler) = CreateService();
        await service.CreateOrReuseAttemptAsync(UserId, CaseId, TenantId, 11.15m, ConversationId);
        stripe.IntentStatus = stripeStatus;

        var session = await service.GetCurrentAttemptAsync(UserId, CaseId, TenantId);

        Assert.NotNull(session);
        Assert.Equal(PaymentAuthorizationStatus.Created, session.PaymentAuthorizationStatus);
        Assert.Equal("pi_1_secret", session.PaymentSessionSecret);
        Assert.Empty(handler.Changes);
    }

    [Fact]
    public async Task GetCurrentAttempt_DoesNotCallStripe_OnceTerminal() {
        var (service, _, stripe, _) = CreateService();
        await service.CreateOrReuseAttemptAsync(UserId, CaseId, TenantId, 11.15m, ConversationId);
        stripe.IntentStatus = "succeeded";
        await service.GetCurrentAttemptAsync(UserId, CaseId, TenantId);
        var callsBefore = stripe.Requests.Count;

        await service.GetCurrentAttemptAsync(UserId, CaseId, TenantId);

        Assert.Equal(callsBefore, stripe.Requests.Count);
    }

    [Fact]
    public async Task GetCurrentAttempt_ReturnsNull_WhenNoAttemptExists() {
        var (service, _, _, _) = CreateService();

        var session = await service.GetCurrentAttemptAsync(UserId, CaseId, TenantId);

        Assert.Null(session);
    }

    [Fact]
    public async Task Cancel_CancelsIntentAndMarksSession() {
        var (service, db, stripe, _) = CreateService();
        await service.CreateOrReuseAttemptAsync(UserId, CaseId, TenantId, 11.15m, ConversationId);

        var session = await service.CancelAsync(CaseId, TenantId);

        Assert.NotNull(session);
        Assert.Equal(PaymentAuthorizationStatus.Cancelled, session.PaymentAuthorizationStatus);
        Assert.Null(session.PaymentSessionSecret);
        Assert.Single(stripe.Requests, x => x.Method == HttpMethod.Post && x.Path == "/v1/payment_intents/pi_1/cancel");
        Assert.Equal(PaymentAuthorizationStatus.Cancelled, Assert.Single(db.PaymentSessions).Status);
    }

    [Fact]
    public async Task Cancel_ReturnsNull_WhenNoLiveAttemptExists() {
        var (service, _, stripe, _) = CreateService();

        var session = await service.CancelAsync(CaseId, TenantId);

        Assert.Null(session);
        Assert.Empty(stripe.Requests);
    }

    private static (StripePaymentsService Service, AgentsDbContext Db, FakeStripeClient Stripe, RecordingPaymentEventHandler Handler) CreateService() {
        var options = new DbContextOptionsBuilder<AgentsDbContext>()
            .UseInMemoryDatabase(databaseName: $"StripePayments-{Guid.NewGuid()}")
            .Options;
        var db = new AgentsDbContext(options);
        var stripe = new FakeStripeClient();
        var handler = new RecordingPaymentEventHandler();
        var service = new StripePaymentsService(
            db,
            stripe,
            Options.Create(new StripeOptions { PublishableKey = "pk_test", SecretKey = "sk_test", WebhookSecret = "whsec_test" }),
            handler,
            TimeProvider.System,
            NullLogger<StripePaymentsService>.Instance);
        return (service, db, stripe, handler);
    }

    private sealed class RecordingPaymentEventHandler : IPaymentEventHandler
    {
        public List<PaymentSessionModel> Changes { get; } = [];

        public Task OnStatusChangedAsync(PaymentSessionModel session, CancellationToken cancellationToken = default) {
            Changes.Add(session);
            return Task.CompletedTask;
        }
    }

    /// <summary>In-memory <see cref="IStripeClient"/> that answers PaymentIntent create/get/cancel calls and records every request.</summary>
    private sealed class FakeStripeClient : IStripeClient
    {
        private int _sequence;

        public record Request(HttpMethod Method, string Path, BaseOptions? Options, RequestOptions? RequestOptions);

        public List<Request> Requests { get; } = [];
        /// <summary>The status Stripe reports for every existing PaymentIntent.</summary>
        public string IntentStatus { get; set; } = "requires_payment_method";

        public string ApiBase => "https://api.stripe.com";
        public string ApiKey => "sk_test";
        public string ClientId => string.Empty;
        public string ConnectBase => "https://connect.stripe.com";
        public string FilesBase => "https://files.stripe.com";
        public string MeterEventsBase => "https://meter-events.stripe.com";

        public Task<T> RequestAsync<T>(HttpMethod method, string path, BaseOptions options, RequestOptions requestOptions, CancellationToken cancellationToken = default) where T : IStripeEntity {
            Requests.Add(new Request(method, path, options, requestOptions));
            PaymentIntent intent;
            if (method == HttpMethod.Post && path == "/v1/payment_intents") {
                var id = $"pi_{++_sequence}";
                var create = (PaymentIntentCreateOptions)options;
                intent = new PaymentIntent { Id = id, ClientSecret = $"{id}_secret", Status = "requires_payment_method", Amount = create.Amount ?? 0, Currency = create.Currency };
            } else if (method == HttpMethod.Get && path.StartsWith("/v1/payment_intents/", StringComparison.Ordinal)) {
                intent = new PaymentIntent { Id = path["/v1/payment_intents/".Length..], Status = IntentStatus };
            } else if (method == HttpMethod.Post && path.EndsWith("/cancel", StringComparison.Ordinal)) {
                IntentStatus = "canceled";
                intent = new PaymentIntent { Id = path["/v1/payment_intents/".Length..^"/cancel".Length], Status = IntentStatus };
            } else {
                throw new NotSupportedException($"Unexpected Stripe request {method} {path}.");
            }
            return Task.FromResult((T)(object)intent);
        }

        public Task<Stream> RequestStreamingAsync(HttpMethod method, string path, BaseOptions options, RequestOptions requestOptions, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
