using Indice.Features.Agents.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Payments;

/// <summary>Chat payments settings shared by all providers. Bound from the <c>Dex:Payments</c> configuration section.</summary>
public class PaymentsOptions
{
    /// <summary>The configuration section name.</summary>
    public const string Name = "Dex:Payments";
    /// <summary>The payment providers the user may pick (see <see cref="PaymentProviders"/>). When empty, <c>Stripe</c> is assumed.</summary>
    /// <remarks>Left empty by default because the configuration binder appends to (rather than replaces) a pre-populated list.</remarks>
    public List<string> AllowedProviders { get; set; } = [];

    /// <summary>The allowed providers, falling back to <c>Stripe</c> when none is configured.</summary>
    public IReadOnlyList<string> EffectiveProviders => AllowedProviders.Count == 0
        ? [PaymentProviders.Stripe]
        : [.. AllowedProviders.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase)];

    /// <summary>True when <paramref name="provider"/> is one of the <see cref="EffectiveProviders"/>.</summary>
    public bool IsAllowed(string? provider) =>
        !string.IsNullOrWhiteSpace(provider) && EffectiveProviders.Contains(provider.Trim(), StringComparer.OrdinalIgnoreCase);
}

/// <summary>Provider-agnostic access to the payment attempts of a case.</summary>
public interface IPaymentAttemptsService
{
    /// <summary>Resolves an allowed provider by name; <c>null</c> when it is unknown or not allowed.</summary>
    IPaymentProvider? GetProvider(string? name);
    /// <summary>Creates (or reuses) the case's attempt with the given provider.</summary>
    /// <exception cref="InvalidOperationException">The provider is unknown or not allowed.</exception>
    Task<PaymentSessionModel?> CreateOrReuseAttemptAsync(string provider, string userId, string caseId, string tenantId, decimal amount, Guid conversationId, CancellationToken cancellationToken = default);
    /// <summary>The latest payment attempt of the case.</summary>
    Task<PaymentSessionModel?> GetCurrentAttemptAsync(string userId, string caseId, string tenantId, CancellationToken cancellationToken = default);
    /// <summary>Replaces a failed or cancelled attempt with a new one of the same provider; returns the live attempt if one exists; null if the user has no attempt for the case.</summary>
    Task<PaymentSessionModel?> RetryAttemptAsync(string userId, string caseId, string tenantId, CancellationToken cancellationToken = default);
}

/// <summary>Default <see cref="IPaymentAttemptsService"/>: routes to the <see cref="IPaymentProvider"/> that owns the attempt.</summary>
public class PaymentAttemptsService(AgentsDbContext db, IEnumerable<IPaymentProvider> providers, IOptions<PaymentsOptions> options) : IPaymentAttemptsService
{
    private readonly PaymentsOptions _options = options.Value;

    ///<inheritdoc/>
    public IPaymentProvider? GetProvider(string? name) =>
        _options.IsAllowed(name) ? providers.FirstOrDefault(x => string.Equals(x.Name, name!.Trim(), StringComparison.OrdinalIgnoreCase)) : null;

    ///<inheritdoc/>
    public Task<PaymentSessionModel?> CreateOrReuseAttemptAsync(string provider, string userId, string caseId, string tenantId, decimal amount, Guid conversationId, CancellationToken cancellationToken = default) {
        var paymentProvider = GetProvider(provider) ?? throw new InvalidOperationException($"Payment provider '{provider}' is not registered or not allowed.");
        return paymentProvider.CreateOrReuseAttemptAsync(userId, caseId, tenantId, amount, conversationId, cancellationToken);
    }

    ///<inheritdoc/>
    public async Task<PaymentSessionModel?> GetCurrentAttemptAsync(string userId, string caseId, string tenantId, CancellationToken cancellationToken = default) {
        var session = await db.PaymentSessions.Where(x => x.CaseId == caseId && x.TenantId == tenantId)
                                              .OrderByDescending(x => x.CreatedAt)
                                              .FirstOrDefaultAsync(cancellationToken);
        return session?.ToModel();
    }

    ///<inheritdoc/>
    public async Task<PaymentSessionModel?> RetryAttemptAsync(string userId, string caseId, string tenantId, CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(caseId);
        var latest = await db.PaymentSessions.Where(x => x.CaseId == caseId && x.TenantId == tenantId)
                                             .OrderByDescending(x => x.CreatedAt)
                                             .FirstOrDefaultAsync(cancellationToken);
        if (latest is null) {
            return null;
        }
        var provider = GetProvider(latest.Provider);
        if (provider is null) {
            return null;
        }
        return await provider.CreateOrReuseAttemptAsync(userId, caseId, tenantId, latest.Amount, latest.ConversationId, cancellationToken);
    }
}

internal static class PaymentSessionMappingExtensions
{
    public static PaymentSessionModel ToModel(this DbPaymentSession session) => new() {
        Id = session.Id,
        CaseId = session.CaseId,
        ConversationId = session.ConversationId,
        Provider = session.Provider,
        // Only a confirmable attempt needs its secret on the wire.
        PaymentSessionSecret = session.Status == PaymentAuthorizationStatus.Created ? session.ClientSecret : null,
        PaymentAuthorizationStatus = session.Status,
        Amount = session.Amount,
        Currency = session.Currency
    };
}
