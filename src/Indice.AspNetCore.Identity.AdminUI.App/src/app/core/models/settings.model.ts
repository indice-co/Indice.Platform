export interface IAppSettings {
    api_url: string;
    api_docs: string;
    auth_settings: IAuthSettings;
    culture: string;
    isTemplate: boolean,
    production: boolean;
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
}
