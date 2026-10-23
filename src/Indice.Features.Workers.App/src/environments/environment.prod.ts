// Production environment. Swapped in for environment.ts by the production build.
// Adjust the host/redirect URIs for the deployed origin before going live.
export const environment = {
  production: true,
  isTemplate: false,
  culture: 'en',
  api_url: 'https://localhost:5001/api/workers',
  auth_settings: {
    accessTokenExpiringNotificationTime: 60,
    authority: 'https://localhost:5000',
    automaticSilentRenew: true,
    client_id: 'workers-ui',
    filterProtocolClaims: true,
    loadUserInfo: true,
    monitorSession: true,
    post_logout_redirect_uri: 'http://localhost:4200/logged-out',
    redirect_uri: 'http://localhost:4200/auth-callback',
    response_type: 'code',
    revokeAccessTokenOnSignout: true,
    scope: 'openid profile role email workers',
    silent_redirect_uri: 'http://localhost:4200/auth-renew',
    useRefreshToken: true,
    extraQueryParams: {},
  },
};
