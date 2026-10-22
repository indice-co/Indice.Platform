using Indice.Features.Agents.Core.Data;
using Indice.Features.Agents.Core.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Stripe;

namespace Indice.Features.Agents.Core.Tests;

public class StripePaymentsServiceTests : IAsyncLifetime
{
    private static readonly string _connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database=AgentsDb.Payments.Test_{Environment.Version.Major}_{Guid.NewGuid():N};Trusted_Connection=True;MultipleActiveResultSets=true";
    private AgentsDbContext? _db;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync() {
        if (_db is not null) {
            await _db.Database.EnsureDeletedAsync();
            await _db.DisposeAsync();
        }
    }

    private const string UserId = "user-1";
    private const string TenantId = "indice";
    private static readonly Guid ConversationId = Guid.NewGuid();

    [Fact]
    public async Task CreateOrReuseAttempt_CreatesIntentAndStoresSession() {
        var (service, db, stripe, _) = CreateService();
        string CaseId = Guid.NewGuid().ToString();
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
        string CaseId = Guid.NewGuid().ToString();
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
        string CaseId = Guid.NewGuid().ToString();
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
        string CaseId = Guid.NewGuid().ToString();
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
        string CaseId = Guid.NewGuid().ToString();
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
        string CaseId = Guid.NewGuid().ToString();
        await service.CreateOrReuseAttemptAsync(UserId, CaseId, TenantId, 11.15m, ConversationId);
        stripe.IntentStatus = stripeStatus;

        var session = await service.GetCurrentAttemptAsync(UserId, CaseId, TenantId);

        Assert.NotNull(session);
        Assert.Equal(PaymentAuthorizationStatus.Created, session.PaymentAuthorizationStatus);
        Assert.Equal("pi_1_secret", session.PaymentSessionSecret);
        Assert.Empty(handler.Changes);
    }

    [Fact]
    public async Task GetCurrentAttempt_MapsDeclinedIntentToFailed_WithReason() {
        var (service, db, stripe, handler) = CreateService();
        string CaseId = Guid.NewGuid().ToString();
        await service.CreateOrReuseAttemptAsync(UserId, CaseId, TenantId, 11.15m, ConversationId);
        stripe.IntentStatus = "requires_payment_method";
        stripe.LastPaymentError = "Your card was declined.";

        var session = await service.GetCurrentAttemptAsync(UserId, CaseId, TenantId);

        Assert.NotNull(session);
        Assert.Equal(PaymentAuthorizationStatus.Failed, session.PaymentAuthorizationStatus);
        Assert.Null(session.PaymentSessionSecret);
        var stored = Assert.Single(db.PaymentSessions);
        Assert.Equal(PaymentAuthorizationStatus.Failed, stored.Status);
        Assert.Equal("Your card was declined.", stored.FailureReason);
        Assert.Equal(PaymentAuthorizationStatus.Failed, Assert.Single(handler.Changes).PaymentAuthorizationStatus);
    }

    [Fact]
    public async Task GetCurrentAttempt_ThrottlesStripeReconciliation() {
        var interval = TimeSpan.FromMilliseconds(300);
        string CaseId = Guid.NewGuid().ToString();
        var (service, _, stripe, _) = CreateService(interval);
        await service.CreateOrReuseAttemptAsync(UserId, CaseId, TenantId, 11.15m, ConversationId);
        await service.GetCurrentAttemptAsync(UserId, CaseId, TenantId);
        var callsAfterFirstSync = stripe.Requests.Count(x => x.Method == HttpMethod.Get);

        await service.GetCurrentAttemptAsync(UserId, CaseId, TenantId);
        await service.GetCurrentAttemptAsync(UserId, CaseId, TenantId);
        Assert.Equal(callsAfterFirstSync, stripe.Requests.Count(x => x.Method == HttpMethod.Get));

        await Task.Delay(interval * 2);
        await service.GetCurrentAttemptAsync(UserId, CaseId, TenantId);
        Assert.Equal(callsAfterFirstSync + 1, stripe.Requests.Count(x => x.Method == HttpMethod.Get));
    }

    [Fact]
    public async Task GetCurrentAttempt_DoesNotCallStripe_OnceTerminal() {
        var (service, _, stripe, _) = CreateService();
        string CaseId = Guid.NewGuid().ToString();
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
        string CaseId = Guid.NewGuid().ToString();

        var session = await service.GetCurrentAttemptAsync(UserId, CaseId, TenantId);

        Assert.Null(session);
    }

    // Reconciliation is not throttled by default so every read hits the fake Stripe client; the throttle test opts in.
    private static (StripePaymentsService Service, AgentsDbContext Db, FakeStripeClient Stripe, RecordingPaymentEventHandler Handler) CreateService(TimeSpan? reconciliationInterval = null) {
        // A relational provider is required: the EF InMemory provider does not support ExecuteUpdate (atomic status transitions).
        var options = new DbContextOptionsBuilder<AgentsDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
        var db = new AgentsDbContext(options);
        db.Database.EnsureCreated();
        var stripe = new FakeStripeClient();
        var handler = new RecordingPaymentEventHandler();
        var service = new StripePaymentsService(
            db,
            stripe,
            Options.Create(new StripeOptions { PublishableKey = "pk_test", SecretKey = "sk_test", WebhookSecret = "whsec_test", ReconciliationInterval = reconciliationInterval ?? TimeSpan.Zero }),
            handler,
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
        /// <summary>When set, Stripe reports this as the intent's <c>last_payment_error</c> (a declined confirmation).</summary>
        public string? LastPaymentError { get; set; }

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
                intent = new PaymentIntent {
                    Id = path["/v1/payment_intents/".Length..],
                    Status = IntentStatus,
                    LastPaymentError = LastPaymentError is null ? null : new StripeError { Message = LastPaymentError }
                };
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
