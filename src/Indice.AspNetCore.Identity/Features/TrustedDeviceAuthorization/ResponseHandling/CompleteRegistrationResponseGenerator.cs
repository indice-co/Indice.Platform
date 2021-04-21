using System;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Data.Models;
using Microsoft.AspNetCore.Authentication;

namespace Indice.AspNetCore.Identity.Features
{
    internal class CompleteRegistrationResponseGenerator : IResponseGenerator<CompleteRegistrationRequestValidationResult, CompleteRegistrationResponse>
    {
        public CompleteRegistrationResponseGenerator(
            IUserDeviceStore userDeviceStore,
            ISystemClock systemClock
        ) {
            UserDeviceStore = userDeviceStore ?? throw new ArgumentNullException(nameof(userDeviceStore));
            SystemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
        }

        public IUserDeviceStore UserDeviceStore { get; }
        public ISystemClock SystemClock { get; }

        public async Task<CompleteRegistrationResponse> Generate(CompleteRegistrationRequestValidationResult validationResult) {
            await UserDeviceStore.CreateDevice(new UserDevice { 
                IsPushNotificationsEnabled = false,
                DateCreated = SystemClock.UtcNow,
                DeviceId = validationResult.DeviceId,
                UserId = validationResult.UserId
            });
            return new CompleteRegistrationResponse();
        }
    }
}
