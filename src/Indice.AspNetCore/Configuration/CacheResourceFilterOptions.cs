using Indice.AspNetCore.Filters;

namespace Indice.Configuration;

/// <summary>Options for the <see cref="CacheResourceFilter"/>.</summary>
public class CacheResourceFilterOptions
{
    /// <summary>Configuration section name</summary>
    public const string Name = "IdentityServer:Endpoints";
    /// <summary>Disables the cache for all the endpoints in the IdentityServer API. Defaults to false.</summary>
    public bool DisableCache { get; set; }
}
