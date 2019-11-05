export const environment = {
  api_url: 'https://idsrv-admin-ui.azurewebsites.net',
  api_docs: 'https://idsrv-admin-ui.azurewebsites.net/docs/index.html',
  auth_settings: {
    authority: 'https://idsrv-admin-ui.azurewebsites.net',
    client_id: 'idsrv-admin-ui',
    filterProtocolClaims: true,
    loadUserInfo: true,
    post_logout_redirect_uri: 'https://idsrv-admin-ui.azurewebsites.net',
    redirect_uri: 'https://idsrv-admin-ui.azurewebsites.net/auth-callback',
    response_type: 'code',
    scope: 'openid profile email role offline_access identity identity:clients identity:users',
    silent_redirect_uri : 'https://idsrv-admin-ui.azurewebsites.net/auth-renew'
  },
  production: true
};
