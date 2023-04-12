export const environment = {
    api_url: '',
    api_docs: 'docs/index.html',
    auth_settings: {
        accessTokenExpiringNotificationTime: 60,
        authority: '',
        automaticSilentRenew: true,
        client_id: '',
        filterProtocolClaims: true,
        loadUserInfo: true,
        monitorSession: true,
        post_logout_redirect_uri: '',
        redirect_uri: 'auth-callback',
        response_type: 'code',
        revokeAccessTokenOnSignout: true,
        scope: 'openid profile email role offline_access identity identity:clients identity:users identity:logs',
        silent_redirect_uri: 'auth-renew'
    },
    culture: '',
    isTemplate: true,
    production: true
};
