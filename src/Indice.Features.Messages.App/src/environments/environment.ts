// This file can be replaced during build by using the `fileReplacements` array.
// `ng build` replaces `environment.ts` with `environment.prod.ts`.
// The list of file replacements can be found in `angular.json`.

export const environment = {
  api_url: 'https://messaging.indice.gr/api', //'https://localhost:2001/api',
  auth_settings: {
    accessTokenExpiringNotificationTime: 60,
    authority: 'https://my.indice.gr',
    automaticSilentRenew: true,
    client_id: 'messaging-ui',
    filterProtocolClaims: true,
    loadUserInfo: true,
    monitorSession: true,
    post_logout_redirect_uri: 'http://localhost:4200/logged-out',
    redirect_uri: 'http://localhost:4200/auth-callback',
    response_type: 'code',
    revokeAccessTokenOnSignout: true,
    scope: 'openid profile role email phone messages media',
    silent_redirect_uri: 'http://localhost:4200/auth-renew',
    useRefreshToken: true,
    prompt: 'login'
  },
  culture: 'el-GR',
  isTemplate: false,
  production: false,
  enableMediaLibrary: true,
  enableRichTextEditor: false
};

/*
 * For easier debugging in development mode, you can import the following file
 * to ignore zone related error stack frames such as `zone.run`, `zoneDelegate.invokeTask`.
 *
 * This import should be commented out in production mode because it will have a negative impact
 * on performance if an error is thrown.
 */
// import 'zone.js/plugins/zone-error';  // Included with Angular CLI.
