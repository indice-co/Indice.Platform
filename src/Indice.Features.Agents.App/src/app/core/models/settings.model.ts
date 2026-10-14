/** Resolved application settings, built once at startup by `createAppSettings()` in settings.ts. */
export interface IAppSettings {
  /** Display name of the assistant, shown wherever the UI names it. Set by the host (`AgentsUIOptions.AssistantName`); defaults to `Dex`. */
  assistant_name: string;
  /** Theme the app started with: session storage `theme` (fed by the `?uitheme=` query string argument), else the host's `<html data-theme>` (`AgentsUIOptions.Theme`), else `dex`. */
  theme: string;
  api_url: string;
  auth_settings: IAuthSettings;
  culture: string;
  isTemplate: boolean;
  production: boolean;
  version: string;
}

/** OIDC settings consumed by `@indice/ng-auth` via the `AUTH_SETTINGS` token. */
export interface IAuthSettings {
  accessTokenExpiringNotificationTime: number;
  authority: string;
  automaticSilentRenew: boolean;
  client_id: string;
  filterProtocolClaims: boolean;
  loadUserInfo: boolean;
  monitorSession: boolean;
  post_logout_redirect_uri: string;
  redirect_uri: string;
  response_type: string;
  revokeAccessTokenOnSignout: boolean;
  scope: string;
  silent_redirect_uri: string;
  useRefreshToken: boolean;
  extraQueryParams: Record<string, unknown>;
}
