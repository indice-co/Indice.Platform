export const environment = {
    api_url: '',
    api_docs: 'docs/index.html',
    auth_settings: {
        authority: '',
        client_id: '',
        filterProtocolClaims: true,
        loadUserInfo: true,
        post_logout_redirect_uri: '',
        redirect_uri: '#/auth-callback',
        response_type: 'code',
        scope: 'openid profile email role offline_access identity identity:clients identity:users',
        silent_redirect_uri: '#/auth-renew'
    },
    isTemplate: true,
    production: true
};
