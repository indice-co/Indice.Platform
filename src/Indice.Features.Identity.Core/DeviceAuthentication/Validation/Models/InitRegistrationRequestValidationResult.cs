using System.Security.Claims;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Services;

namespace Indice.Features.Identity.Core.DeviceAuthentication.Validation;

internal class InitRegistrationRequestValidationResult : ValidationResult
{
    public ClaimsPrincipal? Principal { get; set; }
    public Client? Client { get; set; }
    public IList<string> RequestedScopes { get; set; } = [];
    public InteractionMode InteractionMode { get; set; }
    public string? CodeChallenge { get; set; }
    public string? DeviceId { get; set; }
    public string? UserId { get; set; }
    public TotpDeliveryChannel DeliveryChannel { get; set; }
}
