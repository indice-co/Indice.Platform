export const environment = {
  api_url: 'https://localhost:2001',
  auth_settings: {
    accessTokenExpiringNotificationTime: 60,
    authority: 'https://my.indice.gr',
    automaticSilentRenew: true,
    client_id: 'risk-ui',
    filterProtocolClaims: true,
    loadUserInfo: true,
    monitorSession: true,
    post_logout_redirect_uri: 'http://localhost:4200',
    redirect_uri: 'http://localhost:4200/auth-callback',
    response_type: 'code',
    revokeAccessTokenOnSignout: true,
    scope: 'openid profile role email risk',
    silent_redirect_uri: 'http://localhost:4200/auth-renew',
    useRefreshToken: true
  },
  culture: 'el-GR',
  isTemplate: false,
  production: false
};
