// Development environment. `ng serve` uses this file as-is.
// The production build swaps it for environment.prod.ts (see angular.json fileReplacements).
export const environment = {
  production: false,
  // Embedded-host flag — false for `ng serve` / production. When true (template build) the real
  // config is read from the <app-root> attributes at runtime (see core/models/settings.ts).
  isTemplate: false,
  // UI culture (two-letter). Unused today; reserved for future i18n.
  culture: 'el',
  // Dex REST API base URL (no trailing slash — the generated client appends `/api/...`).
  api_url: 'https://agents.indice.gr/api',// 'https://agents.indice.gr/api',
  // OAuth2 authorization-code + PKCE against the Indice identity server.
  // NOTE: the `dex-ui` SPA client must be registered with the redirect URIs below
  // and allowed the `dex`/`dex:chat` scopes, or login will fail.
  auth_settings: {
    accessTokenExpiringNotificationTime: 60,
    authority: 'https://my.indice.gr',
    automaticSilentRenew: true,
    client_id: 'dex-ui',
    filterProtocolClaims: true,
    loadUserInfo: true,
    monitorSession: true,
    post_logout_redirect_uri: 'http://localhost:4200/logged-out',
    redirect_uri: 'http://localhost:4200/auth-callback',
    response_type: 'code',
    revokeAccessTokenOnSignout: true,
    scope: 'openid profile role email agents chat',
    silent_redirect_uri: 'http://localhost:4200/auth-renew',
    useRefreshToken: true,
    extraQueryParams: {},
  },
};
