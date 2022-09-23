using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace Indice.AspNetCore.Identity.TrustedDeviceAuthorization.ResponseHandling
{
    internal class CompleteRegistrationResponse
    {
        public CompleteRegistrationResponse(IEnumerable<IdentityError> errors = null) {
            Errors = errors ?? new List<IdentityError>();
        }

        public IEnumerable<IdentityError> Errors { get; }
    }
}
