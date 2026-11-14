using System.Security.Claims;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Validation;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Types;

namespace Indice.Features.Identity.Core.DeviceAuthentication.Validation;

internal class CompleteRegistrationRequestValidationResult : ValidationResult
{
    public ClaimsPrincipal Principal { get; set; } = new ClaimsPrincipal();
    public Client? Client { get; set; }
    public DevicePlatform DevicePlatform { get; set; }
    public IList<string> RequestedScopes { get; set; } = [];
    public InteractionMode InteractionMode { get; set; }
    public UserDevice? Device { get; set; }
    public User? User { get; set; }
    public string? DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public string? Pin { get; set; }
    public string? PublicKey { get; set; }
}
