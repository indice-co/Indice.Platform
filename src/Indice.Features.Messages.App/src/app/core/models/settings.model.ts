export interface IAppSettings {
    api_url: string;
    auth_settings: IAuthSettings;
    culture: string;
    isTemplate: boolean,
    production: boolean;
    version: string;
    tenantId: string | undefined;
    enableMediaLibrary: boolean;
    maxFileSize: number;
    acceptableFileExtensions: string;
    /** Enables the personal inbox (bell & "my messages" page). */
    enableInbox: boolean;
    /** The absolute url of the SignalR hub used for real time inbox notifications. When empty the inbox falls back to polling. */
    inboxHubUrl: string | undefined;
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
