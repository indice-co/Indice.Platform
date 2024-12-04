using IdentityServer4.Models;
using IdentityServer4.Validation;
using Indice.Features.Identity.Core.Data.Models;

namespace Indice.Features.Identity.Core.DeviceAuthentication.Validation;

internal class DeviceAuthenticationRequestValidationResult : ValidationResult
{
    public bool IsOpenIdRequest { get; set; }
    public Client? Client { get; set; } = null!;
    public IList<string> RequestedScopes { get; set; } = [];
    public InteractionMode InteractionMode { get; set; }
    public string? CodeChallenge { get; set; }
    public string? UserId { get; set; }
    public UserDevice? Device { get; set; }
}
