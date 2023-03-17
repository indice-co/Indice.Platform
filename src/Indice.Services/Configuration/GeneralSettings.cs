namespace Indice.Configuration;

/// <summary>General settings for an ASP.NET Core application.</summary>
public class GeneralSettings
{
    /// <summary>The name is used to mark the section found inside a configuration file.</summary>
    public static readonly string Name = "General";
    /// <summary>Url that the app is hosted under.</summary>
    public string Host { get; set; }
    /// <summary>The URL for the IdentityServer.</summary>
    public string Authority { get; set; }
    /// <summary>The base address URL for the IdentityServer when accessed internally on a private network.</summary>
    public string AuthorityInternal { get; set; }
    /// <summary>The name of the app. Usually used for the Layout page Title inside an HTML header. </summary>
    public string ApplicationName { get; set; } = "My App name";
    /// <summary>The name of the organization or brand.</summary>
    public string Organization { get; set; } = "Indice OE";
    /// <summary>The address of the organization or office location. Used in emails and website footers.</summary>
    public string OrganizationAddress { get; set; }
    /// <summary>A description for the app.</summary>
    public string ApplicationDescription { get; set; } = "My App description.";
    /// <summary>API settings if API is present.</summary>
    public ApiSettings Api { get; set; }
    /// <summary>A flag that indicates whether to enable the Swagger UI.</summary>
    public bool EnableSwagger { get; set; }
    /// <summary>A flag that indicates whether to register mock implementations of services in the DI.</summary>
    public bool MockServices { get; set; }
    /// <summary>A flag that indicates whether to redirect http to https.</summary>
    public bool UseHttpsRedirection { get; set; }
    /// <summary>A flag that indicates whether to redirect the setting that is definded in <see cref="Host"/>.</summary>
    public bool UseRedirectToHost { get; set; }
    /// <summary>A list of endpoints used throughout the application.</summary>
    public Dictionary<string, string> Endpoints { get; set; }
    /// <summary>A flag that indicates whether to enable HSTS (HTTP Strict Transport Security).</summary>
    public bool HstsEnabled { get; set; }
}
