// Template (embedded) environment. Swapped in by `npm run build:template` (angular.json `template` config).
// Real config (authority, clientId, api, host, culture, scopes) is injected at runtime from the
// <app-root> element attributes by the host's UseAgentsUI() middleware — see core/models/settings.ts.
// Only auth *behaviour* flags and the relative redirect paths are kept here.
export const environment = {
  production: true,
  isTemplate: true,
  api_url: '',
  culture: '',
  auth_settings: {
    accessTokenExpiringNotificationTime: 60,
    authority: '',
    automaticSilentRenew: true,
    client_id: '',
    filterProtocolClaims: true,
    loadUserInfo: true,
    monitorSession: true,
    // Relative paths — composed with host + base path at runtime in settings.ts.
    post_logout_redirect_uri: 'logged-out',
    redirect_uri: 'auth-callback',
    response_type: 'code',
    revokeAccessTokenOnSignout: true,
    scope: '',
    silent_redirect_uri: 'auth-renew',
    useRefreshToken: true,
    extraQueryParams: {},
  },
};
