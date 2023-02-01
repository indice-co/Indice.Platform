using System;
using System.Threading.Tasks;
using Indice.Features.Identity.Core.Models;
using Indice.Features.Identity.Core.Events;
using Indice.Services;

namespace Indice.Features.Identity.Core.DeviceAuthentication.Services
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
