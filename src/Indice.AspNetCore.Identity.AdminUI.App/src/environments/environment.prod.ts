export const environment = {
  api_url: 'https://indice-idsrv.azurewebsites.net',
  api_docs: 'https://indice-idsrv.azurewebsites.net/docs/index.html',
  auth_settings: {
    authority: 'https://indice-idsrv.azurewebsites.net',
    client_id: 'idsrv-admin-ui',
    filterProtocolClaims: true,
    loadUserInfo: true,
    post_logout_redirect_uri: 'https://indice-admin-ui.azureedge.net/#/logged-out',
    redirect_uri: 'https://indice-admin-ui.azureedge.net/#/auth-callback',
    response_type: 'code',
    scope: 'openid profile email role offline_access identity identity:clients identity:users',
    silent_redirect_uri : 'https://indice-admin-ui.azureedge.net/#/auth-renew'
  },
  isTemplate: false,
  production: true
};
