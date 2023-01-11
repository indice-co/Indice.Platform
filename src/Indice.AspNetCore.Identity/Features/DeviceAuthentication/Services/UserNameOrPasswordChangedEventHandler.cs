using System;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.AspNetCore.Identity.Events;
using Indice.Services;

namespace Indice.AspNetCore.Identity.DeviceAuthentication.Services
{
    internal class UserNameOrPasswordChangedEventHandler : IPlatformEventHandler<UserNameChangedEvent>, IPlatformEventHandler<PasswordChangedEvent>
    {
        private readonly ExtendedUserManager<User> _userManager;

        public UserNameOrPasswordChangedEventHandler(ExtendedUserManager<User> extendedUserManager) {
            _userManager = extendedUserManager ?? throw new ArgumentNullException(nameof(extendedUserManager));
        }

        public Task Handle(UserNameChangedEvent @event) => _userManager.SetNativeDevicesRequirePasswordAsync(@event.User, requiresPassword: true);

        public Task Handle(PasswordChangedEvent @event) => _userManager.SetNativeDevicesRequirePasswordAsync(@event.User, requiresPassword: true);
    }
}
