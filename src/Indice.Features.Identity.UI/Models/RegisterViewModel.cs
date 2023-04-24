namespace Indice.Features.Identity.UI.Models;

/// <summary>The view model for registration page.</summary>
public class RegisterViewModel : RegisterInputModel
{
    /// <summary>List of external providers.</summary>
    public IEnumerable<ExternalProviderModel> ExternalProviders { get; set; } = new List<ExternalProviderModel>();
    /// <summary>Visible external providers are those given a <see cref="ExternalProviderModel.DisplayName"/></summary>
    public IEnumerable<ExternalProviderModel> VisibleExternalProviders => ExternalProviders.Where(x => !string.IsNullOrWhiteSpace(x.DisplayName));
    /// <summary>Optional flag that should hide the local user registration form and keep only the <see cref="ExternalProviders"/> options.</summary>
    public bool IsExternalRegistrationOnly { get; set; }
    /// <summary>The authentication scheme of the external registration.</summary>
    public string? ExternalRegistrationScheme => ExternalProviders?.SingleOrDefault()?.AuthenticationScheme;
}
