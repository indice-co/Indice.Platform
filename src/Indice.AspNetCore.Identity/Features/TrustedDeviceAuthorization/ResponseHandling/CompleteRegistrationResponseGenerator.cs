using System;
using System.Linq;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Services;
using Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Stores;
using Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace Indice.AspNetCore.Identity.TrustedDeviceAuthorization.ResponseHandling
{
    internal class CompleteRegistrationResponseGenerator : IResponseGenerator<CompleteRegistrationRequestValidationResult, CompleteRegistrationResponse>
    {
        public CompleteRegistrationResponseGenerator(
            IDevicePasswordHasher devicePasswordHasher,
            ISystemClock systemClock,
            IUserDeviceStore userDeviceStore,
            ExtendedUserManager<User> userManager
        ) {
            UserDeviceStore = userDeviceStore ?? throw new ArgumentNullException(nameof(userDeviceStore));
            UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            DevicePasswordHasher = devicePasswordHasher ?? throw new ArgumentNullException(nameof(devicePasswordHasher));
            SystemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
        }

        public IUserDeviceStore UserDeviceStore { get; }
        public ExtendedUserManager<User> UserManager { get; }
        public IDevicePasswordHasher DevicePasswordHasher { get; }
        public ISystemClock SystemClock { get; }

        public async Task<CompleteRegistrationResponse> Generate(CompleteRegistrationRequestValidationResult validationResult) {
            var device = validationResult.Device ?? new UserDevice(Guid.NewGuid()) {
                DeviceId = validationResult.DeviceId,
                Name = validationResult.DeviceName,
                Platform = validationResult.DevicePlatform,
                IsPushNotificationsEnabled = false,
                PublicKey = validationResult.PublicKey,
                DateCreated = SystemClock.UtcNow,
                User = validationResult.User
            };
            var errors = Enumerable.Empty<IdentityError>();
            IdentityResult result;
            switch (validationResult.InteractionMode) {
                case InteractionMode.Pin when validationResult.Device == null:
                    var password = DevicePasswordHasher.HashPassword(device, validationResult.Pin);
                    device.Password = password;
                    result = await UserManager.CreateDeviceAsync(validationResult.User, device);
                    errors = result.Errors;
                    break;
                case InteractionMode.Pin when validationResult.Device != null:
                    password = DevicePasswordHasher.HashPassword(device, validationResult.Pin);
                    await UserDeviceStore.UpdatePassword(device, password);
                    break;
                case InteractionMode.Fingerprint when validationResult.Device == null:
                    device.PublicKey = validationResult.PublicKey;
                    result = await UserManager.CreateDeviceAsync(validationResult.User, device);
                    errors = result.Errors;
                    break;
                case InteractionMode.Fingerprint when validationResult.Device != null:
                    await UserDeviceStore.UpdatePublicKey(device, validationResult.PublicKey);
                    break;
            }
            return new CompleteRegistrationResponse(device.Id, errors);
        }
    }
}
