// This file can be replaced during build by using the `fileReplacements` array.
// `ng build` replaces `environment.ts` with `environment.prod.ts`.
// The list of file replacements can be found in `angular.json`.

const title = document.title;

export const environment = {
  api_url: 'https://localhost:44378', // cases api url
  auth_settings: {
    accessTokenExpiringNotificationTime: 60,
    authority: 'https://localhost:44359', // identity api url
    automaticSilentRenew: true,
    client_id: 'cases-backoffice-app',
    filterProtocolClaims: true,
    loadUserInfo: true,
    monitorSession: true,
    post_logout_redirect_uri: 'http://localhost:4300',
    redirect_uri: 'http://localhost:4300/auth-callback',
    response_type: 'code',
    revokeAccessTokenOnSignout: true,
    scope: 'openid profile role email cases-api',
    silent_redirect_uri: 'http://localhost:4300/auth-renew',
    useRefreshToken: true
  },
  culture: 'el-GR',
  isTemplate: false,
  production: false
};

/*
 * For easier debugging in development mode, you can import the following file
 * to ignore zone related error stack frames such as `zone.run`, `zoneDelegate.invokeTask`.
 *
 * This import should be commented out in production mode because it will have a negative impact
 * on performance if an error is thrown.
 */
// import 'zone.js/plugins/zone-error';  // Included with Angular CLI.
