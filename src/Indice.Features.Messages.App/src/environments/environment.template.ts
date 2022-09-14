export const environment = {
    api_url: '',
    auth_settings: {
        accessTokenExpiringNotificationTime: 60,
        authority: '',
        automaticSilentRenew: true,
        client_id: '',
        filterProtocolClaims: true,
        loadUserInfo: true,
        monitorSession: true,
        post_logout_redirect_uri: 'logged-out',
        redirect_uri: 'auth-callback',
        response_type: 'code',
        revokeAccessTokenOnSignout: true,
        scope: 'openid profile role email phone',
        silent_redirect_uri: 'auth-renew'
    },
    culture: '',
    isTemplate: true,
    multitenancy: false,
    production: true
};
