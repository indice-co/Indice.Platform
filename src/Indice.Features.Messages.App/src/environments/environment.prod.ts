export const environment = {
  api_url: 'https://indice-notifications.azurewebsites.net',
  auth_settings: {
    accessTokenExpiringNotificationTime: 60,
    authority: 'https://identity.indice.gr',
    automaticSilentRenew: true,
    client_id: 'backoffice-ui',
    filterProtocolClaims: true,
    loadUserInfo: true,
    monitorSession: true,
    post_logout_redirect_uri: 'https://indice-notifications.azurewebsites.net/messages/logged-out',
    redirect_uri: 'http://localhost:4200/auth-callback',
    response_type: 'code',
    revokeAccessTokenOnSignout: true,
    scope: 'openid profile role email phone backoffice backoffice:campaigns',
    silent_redirect_uri: 'https://indice-notifications.azurewebsites.net/messages/auth-renew',
    useRefreshToken: true
  },
  culture: 'el-GR',
  isTemplate: false,
  multitenancy: false,
  production: true
};
