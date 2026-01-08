// This file can be replaced during build by using the `fileReplacements` array.
// `ng build` replaces `environment.ts` with `environment.prod.ts`.
// The list of file replacements can be found in `angular.json`.

const title = document.title;
const api = 'https://localhost:2001';
const authority = 'https://my.indice.gr';
const self = 'http://localhost:4300';
export const environment = {
  api_url: api, // cases api url
  auth_settings: {
    accessTokenExpiringNotificationTime: 60,
    authority: authority, // identity api url
    automaticSilentRenew: true,
    client_id: 'cases-ui',
    filterProtocolClaims: true,
    loadUserInfo: true,
    monitorSession: true,
    post_logout_redirect_uri: self,
    redirect_uri: `${self}/auth-callback`,
    response_type: 'code',
    revokeAccessTokenOnSignout: true,
    scope: 'openid profile role email cases',
    silent_redirect_uri: `${self}/auth-renew`,
    useRefreshToken: true
  },
  culture: 'el-GR',
  i18n_assets: '/assets/i18n/',
  isTemplate: false,
  production: false,
  dashboardTags: '',
  caseListColumns: '',
  caseListFilters: ''
};

/*
 * For easier debugging in development mode, you can import the following file
 * to ignore zone related error stack frames such as `zone.run`, `zoneDelegate.invokeTask`.
 *
 * This import should be commented out in production mode because it will have a negative impact
 * on performance if an error is thrown.
 */
// import 'zone.js/plugins/zone-error';  // Included with Angular CLI.
