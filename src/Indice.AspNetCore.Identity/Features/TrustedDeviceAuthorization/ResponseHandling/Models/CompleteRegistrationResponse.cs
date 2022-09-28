using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace Indice.AspNetCore.Identity.TrustedDeviceAuthorization.ResponseHandling
{
    internal class CompleteRegistrationResponse
    {
        public CompleteRegistrationResponse(Guid registrationId, IEnumerable<IdentityError> errors) {
            RegistrationId = registrationId;
            Errors = errors ?? new List<IdentityError>();
        }

        public CompleteRegistrationResponse(Guid registrationId) : this(registrationId, null) { }

        public IEnumerable<IdentityError> Errors { get; }
        public Guid RegistrationId { get; }
    }
}
