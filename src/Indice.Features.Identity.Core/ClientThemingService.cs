using System.Text.Json;
using Indice.Features.Identity.Core.Data;
using Indice.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Identity.Core;

/// <summary>Loads the theme configuration for the current client in the context.</summary>
public class ClientThemingService<TThemeConfig> : IClientThemingService<TThemeConfig> where TThemeConfig : class
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ExtendedConfigurationDbContext _configurationDbContext;

    /// <summary>Creates a new instance of <see cref="ClientThemingService{T}"/>.</summary>
    /// <param name="httpContextAccessor">Provides access to the current HTTP context.</param>
    /// <param name="configurationDbContext"><see cref="DbContext"/> for the IdentityServer configuration data.</param>
    public ClientThemingService(
        IHttpContextAccessor httpContextAccessor,
        ExtendedConfigurationDbContext configurationDbContext
    ) {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _configurationDbContext = configurationDbContext ?? throw new ArgumentNullException(nameof(configurationDbContext));
    }

    /// <inheritdoc />
    public async Task<TThemeConfig> GetClientTheme() {
        var clientId = _httpContextAccessor.HttpContext.GetClientIdFromReturnUrl();
        if (string.IsNullOrWhiteSpace(clientId)) {
            return default;
        }
        var client = await _configurationDbContext
            .Clients
            .Include(x => x.Properties)
            .Where(x => x.ClientId == clientId)
            .SingleOrDefaultAsync();
        if (client is null) {
            return default;
        }
        var themeConfig = client
            .Properties
            .Where(x => x.Key == ClientPropertyKeys.ThemeConfig)
            .FirstOrDefault();
        if (themeConfig is not null) {
            return JsonSerializer.Deserialize<TThemeConfig>(themeConfig.Value, JsonSerializerOptionDefaults.GetDefaultSettings());
        }
        return default;
    }
}

/// <summary>Contains operation to load the theme configuration for the current client in the context.</summary>
public interface IClientThemingService<TThemeConfig> where TThemeConfig : class
{
    /// <summary>The type that describes the theme configuration.</summary>
    public Type ThemeConfigType { get => typeof(TThemeConfig); }
    /// <summary>Gets the theme configuration for the current client.</summary>
    Task<TThemeConfig> GetClientTheme();
}
