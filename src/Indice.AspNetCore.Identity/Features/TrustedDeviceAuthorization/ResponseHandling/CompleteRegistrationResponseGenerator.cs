using System;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Services;
using Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Stores;
using Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Validation;
using Microsoft.AspNetCore.Authentication;

namespace Indice.AspNetCore.Identity.TrustedDeviceAuthorization.ResponseHandling
{
    internal class CompleteRegistrationResponseGenerator : IResponseGenerator<CompleteRegistrationRequestValidationResult, CompleteRegistrationResponse>
    {
        public CompleteRegistrationResponseGenerator(
            IDevicePasswordHasher devicePasswordHasher,
            ISystemClock systemClock,
            IUserDeviceStore userDeviceStore
        ) {
            UserDeviceStore = userDeviceStore ?? throw new ArgumentNullException(nameof(userDeviceStore));
            DevicePasswordHasher = devicePasswordHasher;
            SystemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
        }

        public IUserDeviceStore UserDeviceStore { get; }
        public IDevicePasswordHasher DevicePasswordHasher { get; }
        public ISystemClock SystemClock { get; }

        public async Task<CompleteRegistrationResponse> Generate(CompleteRegistrationRequestValidationResult validationResult) {
            var device = validationResult.Device ?? new UserDevice(Guid.NewGuid()) {
                DateCreated = SystemClock.UtcNow,
                DeviceId = validationResult.DeviceId,
                DeviceName = validationResult.DeviceName,
                DevicePlatform = validationResult.DevicePlatform,
                Enabled = true,
                IsPushNotificationsEnabled = false,
                PublicKey = validationResult.PublicKey,
                UserId = validationResult.UserId
            };
            switch (validationResult.InteractionMode) {
                case InteractionMode.Pin when validationResult.Device == null:
                    var password = DevicePasswordHasher.HashPassword(device, validationResult.Pin);
                    device.Password = password;
                    await UserDeviceStore.CreateDevice(device);
                    break;
                case InteractionMode.Pin when validationResult.Device != null:
                    password = DevicePasswordHasher.HashPassword(device, validationResult.Pin);
                    await UserDeviceStore.UpdateDevicePassword(device, password);
                    break;
                case InteractionMode.Fingerprint when validationResult.Device == null:
                    device.PublicKey = validationResult.PublicKey;
                    await UserDeviceStore.CreateDevice(device);
                    break;
                case InteractionMode.Fingerprint when validationResult.Device != null:
                    await UserDeviceStore.UpdateDevicePublicKey(device, validationResult.PublicKey);
                    break;
            }
            return new CompleteRegistrationResponse();
        }
    }
}
