using IdentityModel.AspNetCore.AccessTokenManagement;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using ResourceOwnerPasswordFlow.Settings;

namespace ResourceOwnerPasswordFlow.Services
{
    public class CustomTokenClientConfigurationService : DefaultTokenClientConfigurationService
    {
        private readonly ClientSettings _clientSettings;
        private readonly GeneralSettings _generalSettings;

        public CustomTokenClientConfigurationService(
            UserAccessTokenManagementOptions userAccessTokenManagementOptions,
            ClientAccessTokenManagementOptions clientAccessTokenManagementOptions,
            IOptionsMonitor<OpenIdConnectOptions> oidcOptions,
            IAuthenticationSchemeProvider authenticationSchemeProvider,
            IOptions<ClientSettings> clientSettings,
            IOptions<GeneralSettings> generalSettings,
            ILogger<DefaultTokenClientConfigurationService> logger
        ) : base(userAccessTokenManagementOptions, clientAccessTokenManagementOptions, oidcOptions, authenticationSchemeProvider, logger) {
            _clientSettings = clientSettings?.Value ?? throw new ArgumentNullException(nameof(clientSettings));
            _generalSettings = generalSettings?.Value ?? throw new ArgumentNullException(nameof(generalSettings));
        }

        public override Task<RefreshTokenRequest> GetRefreshTokenRequestAsync(UserAccessTokenParameters parameters = null) {
            return Task.FromResult(new RefreshTokenRequest {
                Address = $"{_generalSettings.Authority}/connect/token",
                ClientId = _clientSettings.Id,
                ClientSecret = _clientSettings.Secret
            });
        }

        public override Task<TokenRevocationRequest> GetTokenRevocationRequestAsync(UserAccessTokenParameters parameters = null) {
            return Task.FromResult(new TokenRevocationRequest {
                Address = $"{_generalSettings.Authority}/connect/revocation",
                ClientId = _clientSettings.Id,
                ClientSecret = _clientSettings.Secret
            });
        }
    }
}
