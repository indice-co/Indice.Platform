using Indice.Services;

namespace Indice.Features.Media.AspNetCore;
/// <summary>Constant values for Media API.</summary>
public static class MediaLibraryApi
{
    /// <summary>Authentication scheme name used by Messages API.</summary>
    public const string AuthenticationScheme = "Bearer";
    /// <summary>Media API scope.</summary>
    public const string Scope = "media";

    /// <summary>Constant values for Media API Policies.</summary>
    public static class Policies
    {
        /// <summary>Policy name for access to media library.</summary>
        public const string BeMediaLibraryManager = nameof(BeMediaLibraryManager);
    }
}

/// <summary>Placeholder for prefixing Media API endpoints.</summary>
internal class ApiPrefixes
{
    /// <summary>Media API prefix placeholder.</summary>
    public const string MediaManagementEndpoints = "media";
}

/// <summary>Service keys for Media API.</summary>
public class KeyedServiceNames
{
    /// <summary>Key service name for <see cref="IFileService"/> implementation.</summary>
    public const string FileServiceKey = "Media:FileServiceKey";
}
