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
            var device = new UserDevice(Guid.NewGuid()) {
                DateCreated = SystemClock.UtcNow,
                DeviceId = validationResult.DeviceId,
                DeviceName = validationResult.DeviceName,
                DevicePlatform = validationResult.DevicePlatform,
                Enabled = true,
                IsPushNotificationsEnabled = false,
                PublicKey = validationResult.PublicKey,
                UserId = validationResult.UserId
            };
            if (validationResult.InteractionMode == InteractionMode.Pin) {
                device.Password = DevicePasswordHasher.HashPassword(device, validationResult.Pin);
            }
            await UserDeviceStore.CreateDevice(device);
            return new CompleteRegistrationResponse();
        }
    }
}
