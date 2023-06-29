export interface IAppSettings {
    api_url: string;
    auth_settings: IAuthSettings;
    culture: string;
    i18n_assets: string;
    isTemplate: boolean,
    production: boolean;
    version: string;
}

export interface IAuthSettings {
    authority: string;
    client_id: string;
    filterProtocolClaims: boolean;
    loadUserInfo: boolean;
    post_logout_redirect_uri: string;
    redirect_uri: string;
    response_type: string;
    scope: string;
    silent_redirect_uri: string;
    revokeAccessTokenOnSignout: boolean;
    accessTokenExpiringNotificationTime: number;
    monitorSession: boolean;
    automaticSilentRenew: boolean;
}
