using Indice.Events;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Events;

namespace Indice.Features.Identity.Core.DeviceAuthentication.Services;

internal class UserNameOrPasswordChangedEventHandler(ExtendedUserManager<User> extendedUserManager) : IPlatformEventHandler<UserNameChangedEvent>, IPlatformEventHandler<PasswordChangedEvent>
{
    private readonly ExtendedUserManager<User> _userManager = extendedUserManager ?? throw new ArgumentNullException(nameof(extendedUserManager));

    public async Task Handle(UserNameChangedEvent @event, PlatformEventArgs args) {
        var user = await _userManager.FindByIdAsync(@event.User.Id);
        await _userManager.SetNativeDevicesRequirePasswordAsync(user, requiresPassword: true);
    }

    public async Task Handle(PasswordChangedEvent @event, PlatformEventArgs args) {
        var user = await _userManager.FindByIdAsync(@event.User.Id);
        await _userManager.SetNativeDevicesRequirePasswordAsync(user, requiresPassword: true);
    }
}
