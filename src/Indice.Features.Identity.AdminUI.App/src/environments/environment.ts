// This file can be replaced during build by using the `fileReplacements` array.
// `ng build --prod` replaces `environment.ts` with `environment.prod.ts`.
// The list of file replacements can be found in `angular.json`.

const authority = 'https://localhost:2000'; // '';https://my.indice.gr
export const environment = {
    api_url: authority,
    api_docs: `${authority}/docs/index.html`,
    auth_settings: {
        accessTokenExpiringNotificationTime: 60,
        authority: authority,
        automaticSilentRenew: true,
        client_id: 'idsrv-admin-ui',
        filterProtocolClaims: true,
        loadUserInfo: true,
        monitorSession: true,
        post_logout_redirect_uri: 'http://localhost:4200/admin',
        redirect_uri: 'http://localhost:4200/auth-callback',
        response_type: 'code',
        revokeAccessTokenOnSignout: true,
        scope: 'openid profile email role offline_access identity identity:clients identity:users identity:logs',
        silent_redirect_uri: 'http://localhost:4200/auth-renew'
    },
    culture: 'en-GB',
    isTemplate: false,
    production: false
}
/*
 * For easier debugging in development mode, you can import the following file
 * to ignore zone related error stack frames such as `zone.run`, `zoneDelegate.invokeTask`.
 *
 * This import should be commented out in production mode because it will have a negative impact
 * on performance if an error is thrown.
 */
// import 'zone.js/plugins/zone-error';  // Included with Angular CLI.
