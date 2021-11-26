export const environment = {
  auth_settings: {
    accessTokenExpiringNotificationTime: 60,
    authority: 'https://indice-idsrv.azurewebsites.net',
    automaticSilentRenew: true,
    client_id: 'backoffice-ui',
    filterProtocolClaims: true,
    loadUserInfo: true,
    monitorSession: true,
    post_logout_redirect_uri: 'http://localhost:4200/logged-out',
    redirect_uri: 'http://localhost:4200/auth-callback',
    response_type: 'code',
    revokeAccessTokenOnSignout: true,
    scope: 'openid profile role email phone backoffice backoffice:campaigns',
    silent_redirect_uri: 'http://localhost:4200/auth-renew',
    useRefreshToken: true
  },
  campaigns_api_url: 'https://localhost:2001',
  production: true
};
