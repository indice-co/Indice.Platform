using Indice.Events;
using Indice.Features.Cases.Core.Events;
using Indice.Features.Cases.Core.Models;
using Microsoft.Extensions.Caching.Distributed;

namespace Indice.Features.Cases.Server.Authorization;

/// <summary>
/// Removes what <see cref="CasesAccessOwnerHandler"/> cached for a customer, when the back-office changes what that customer can do on a case.
/// Without it a customer that was refused stays refused, and one that was removed keeps access, until the cached answer expires.
/// </summary>
internal class CasesAccessOwnerCacheInvalidationHandler :
    IPlatformEventHandler<CaseCoOwnerAddedEvent>,
    IPlatformEventHandler<CaseCoOwnerRemovedEvent>,
    IPlatformEventHandler<CaseContactPermissionChangedEvent>
{
    private readonly IDistributedCache _cache;

    public CasesAccessOwnerCacheInvalidationHandler(IDistributedCache cache) {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    /// <inheritdoc/>
    public Task Handle(CaseCoOwnerAddedEvent @event, PlatformEventArgs args) => Remove(@event.UserId, @event.CaseId);

    /// <inheritdoc/>
    public Task Handle(CaseCoOwnerRemovedEvent @event, PlatformEventArgs args) => Remove(@event.UserId, @event.CaseId);

    /// <inheritdoc/>
    public Task Handle(CaseContactPermissionChangedEvent @event, PlatformEventArgs args) => Remove(@event.UserId, @event.CaseId);

    private async Task Remove(string? userId, Guid caseId) {
        if (string.IsNullOrEmpty(userId)) {
            return;
        }
        // The answer is cached once for every permission an endpoint can ask for.
        foreach (var permission in Enum.GetValues<ContactPermission>()) {
            await _cache.RemoveAsync(string.Format(CasesAccessOwnerHandler.CacheKeyFormat, userId, caseId, permission));
        }
    }
}
