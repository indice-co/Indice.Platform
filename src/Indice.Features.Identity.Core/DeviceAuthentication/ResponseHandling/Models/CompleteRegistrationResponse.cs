using Microsoft.AspNetCore.Identity;

namespace Indice.Features.Identity.Core.DeviceAuthentication.ResponseHandling;

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
