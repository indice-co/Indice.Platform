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
        scope: '',
        silent_redirect_uri: 'auth-renew'
    },
    culture: '',
    i18n_assets: '/assets/i18n/',
    isTemplate: true,
    production: true
};
