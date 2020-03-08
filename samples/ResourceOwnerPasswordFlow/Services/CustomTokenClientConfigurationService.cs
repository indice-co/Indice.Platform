using System;
using System.Threading.Tasks;
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

        public CustomTokenClientConfigurationService(IOptions<AccessTokenManagementOptions> accessTokenManagementOptions, IOptionsMonitor<OpenIdConnectOptions> oidcOptions,
            IAuthenticationSchemeProvider schemeProvider, IOptions<ClientSettings> clientSettings, IOptions<GeneralSettings> generalSettings)
            : base(accessTokenManagementOptions, oidcOptions, schemeProvider) {
            _clientSettings = clientSettings?.Value ?? throw new ArgumentNullException(nameof(clientSettings));
            _generalSettings = generalSettings?.Value ?? throw new ArgumentNullException(nameof(generalSettings));
        }

        public override Task<RefreshTokenRequest> GetRefreshTokenRequestAsync() {
            return Task.FromResult(new RefreshTokenRequest {
                Address = $"{_generalSettings.Authority}/connect/token",
                ClientId = _clientSettings.Id,
                ClientSecret = _clientSettings.Secret
            });
        }

        public override Task<TokenRevocationRequest> GetTokenRevocationRequestAsync() {
            return Task.FromResult(new TokenRevocationRequest {
                Address = $"{_generalSettings.Authority}/connect/revocation",
                ClientId = _clientSettings.Id,
                ClientSecret = _clientSettings.Secret
            });
        }
    }
}
