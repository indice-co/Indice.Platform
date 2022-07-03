using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Api.Security;
using Indice.AspNetCore.Identity.Data;
using Indice.AspNetCore.Identity.Models;
using Indice.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Indice.AspNetCore.Identity
{
    /// <summary>Loads the theme configuration for the current client in the context.</summary>
    public class ClientThemingService : IClientThemingService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ExtendedConfigurationDbContext _configurationDbContext;

        /// <summary>Creates a new instance of <see cref="ClientThemingService"/>.</summary>
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
        public async Task<ClientThemeConfig> GetClientTheme() {
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
                .Where(x => x.Key == IdentityServerApi.PropertyKeys.ThemeConfig)
                .FirstOrDefault();
            if (themeConfig is not null) {
                return JsonSerializer.Deserialize<ClientThemeConfig>(themeConfig.Value, JsonSerializerOptionDefaults.GetDefaultSettings());
            }
            return default;
        }
    }

    /// <summary>Contains operation to load the theme configuration for the current client in the context.</summary>
    public interface IClientThemingService
    {
        /// <summary>Gets the theme configuration for the current client.</summary>
        Task<ClientThemeConfig> GetClientTheme();
    }
}
