using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Events;
using Indice.Services;

namespace Indice.Features.Identity.Core.DeviceAuthentication.Services;

internal class UserNameOrPasswordChangedEventHandler : IPlatformEventHandler<UserNameChangedEvent>, IPlatformEventHandler<PasswordChangedEvent>
{
    private readonly ExtendedUserManager<DbUser> _userManager;

    public UserNameOrPasswordChangedEventHandler(ExtendedUserManager<DbUser> extendedUserManager) {
        _userManager = extendedUserManager ?? throw new ArgumentNullException(nameof(extendedUserManager));
    }

    public Task Handle(UserNameChangedEvent @event) => _userManager.SetNativeDevicesRequirePasswordAsync(@event.User, requiresPassword: true);

    public Task Handle(PasswordChangedEvent @event) => _userManager.SetNativeDevicesRequirePasswordAsync(@event.User, requiresPassword: true);
}
