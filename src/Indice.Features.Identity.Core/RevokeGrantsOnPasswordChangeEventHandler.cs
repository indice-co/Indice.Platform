#if NET9_0_OR_GREATER
using Duende.IdentityServer.Services;
#else
using IdentityServer4.Services;
#endif
using Indice.Events;
using Indice.Features.Identity.Core.Events;
using Indice.Features.Identity.Core.Events.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Identity.Core;

/// <summary>
/// Revokes all persisted grants (refresh tokens, reference tokens, consents) of a user whenever the password changes, 
/// so that credentials issued before the change cannot be used any more.
/// Enabled via <c>IdentityOptions:Password:RevokeGrantsOnPasswordChange</c>.
/// </summary>
internal sealed class RevokeGrantsOnPasswordChangeEventHandler : IPlatformEventHandler<PasswordChangedEvent>, IPlatformEventHandler<PasswordSetEvent>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly bool _enabled;

    public RevokeGrantsOnPasswordChangeEventHandler(IServiceProvider serviceProvider, IConfiguration configuration) {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _enabled = configuration.GetIdentityOption<bool?>(nameof(IdentityOptions.Password), "RevokeGrantsOnPasswordChange") ?? false;
    }

    Task IPlatformEventHandler<PasswordChangedEvent>.Handle(PasswordChangedEvent @event, PlatformEventArgs args) => RevokeAsync(@event.User);

    Task IPlatformEventHandler<PasswordSetEvent>.Handle(PasswordSetEvent @event, PlatformEventArgs args) => RevokeAsync(@event.User);

    private async Task RevokeAsync(UserEventContext user) {
        if (!_enabled) {
            return;
        }
        // Resolved lazily so that hosts without the feature enabled do not need IdentityServer services registered.
        await _serviceProvider.GetRequiredService<IPersistedGrantService>().RemoveAllGrantsAsync(user.Id);
    }
}
