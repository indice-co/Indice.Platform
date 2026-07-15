// Production environment. Swapped in for environment.ts by the production build.
// Adjust the host/redirect URIs for the deployed origin before going live.
export const environment = {
  production: true,
  isTemplate: false,
  culture: 'el',
  api_url: 'https://localhost:2002/api',
  auth_settings: {
    accessTokenExpiringNotificationTime: 60,
    authority: 'https://localhost:2000',
    automaticSilentRenew: true,
    client_id: 'dex-ui',
    filterProtocolClaims: true,
    loadUserInfo: true,
    monitorSession: true,
    post_logout_redirect_uri: 'http://localhost:4200/logged-out',
    redirect_uri: 'http://localhost:4200/auth-callback',
    response_type: 'code',
    revokeAccessTokenOnSignout: true,
    scope: 'openid profile role email dex dex:chat',
    silent_redirect_uri: 'http://localhost:4200/auth-renew',
    useRefreshToken: true,
    extraQueryParams: {},
  },
};
