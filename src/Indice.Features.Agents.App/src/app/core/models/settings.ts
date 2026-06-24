import { environment } from '../../../environments/environment';
import { IAppSettings, IAuthSettings } from './settings.model';

// Builds the app's runtime settings. When the build is embedded in the .NET host
// (`isTemplate` === true) the real config is read from the <app-root> element attributes injected by
// UseAgentsUI(); otherwise the hardcoded `environment` is used (ng serve / production build).
function createAppSettings(): IAppSettings {
  const isTemplate = environment.isTemplate;
  let authority = '', clientId = '', host = '', culture = '', version = '', scopes = '', path = '';
  if (isTemplate) {
    const appRoot = document.getElementsByTagName('app-root')[0];
    authority = appRoot.getAttribute('authority') || '';
    clientId = appRoot.getAttribute('clientId') || '';
    host = appRoot.getAttribute('host') || '';
    path = appRoot.getAttribute('path') || '';
    culture = appRoot.getAttribute('culture') || '';
    version = appRoot.getAttribute('version') || '';
    scopes = appRoot.getAttribute('scopes') || '';
    if (!authority || !clientId || !host) {
      throw new Error('Please provide authority, clientId and host as attributes of the app-root element.');
    }
    appRoot.attributes.removeNamedItem('authority');
    appRoot.attributes.removeNamedItem('clientId');
    appRoot.attributes.removeNamedItem('host');
    appRoot.attributes.removeNamedItem('path');
    appRoot.attributes.removeNamedItem('culture');
    appRoot.attributes.removeNamedItem('version');
    appRoot.attributes.removeNamedItem('scopes');
  }
  return {
    // The generated DexApiService / ChatStreamService append "/api/..." to this base, so it must be the
    // origin (no "/api" suffix) — same shape as the dev environment. Embedded = same origin as the host.
    api_url: !isTemplate ? environment.api_url : host.replace(/\/$/su, ''),
    auth_settings: {
      accessTokenExpiringNotificationTime: environment.auth_settings.accessTokenExpiringNotificationTime,
      authority: !isTemplate ? environment.auth_settings.authority : authority,
      automaticSilentRenew: environment.auth_settings.automaticSilentRenew,
      client_id: !isTemplate ? environment.auth_settings.client_id : clientId,
      filterProtocolClaims: environment.auth_settings.filterProtocolClaims,
      loadUserInfo: environment.auth_settings.loadUserInfo,
      monitorSession: environment.auth_settings.monitorSession,
      post_logout_redirect_uri: !isTemplate
        ? environment.auth_settings.post_logout_redirect_uri
        : [host.replace(/\/$/su, ''), path.replace(/(^\/)|(\/$)/sug, ''), environment.auth_settings.post_logout_redirect_uri].filter(x => x?.length > 0).join('/'),
      redirect_uri: !isTemplate
        ? environment.auth_settings.redirect_uri
        : [host.replace(/\/$/su, ''), path.replace(/(^\/)|(\/$)/sug, ''), environment.auth_settings.redirect_uri].filter(x => x?.length > 0).join('/'),
      response_type: environment.auth_settings.response_type,
      revokeAccessTokenOnSignout: environment.auth_settings.revokeAccessTokenOnSignout,
      scope: `${environment.auth_settings.scope} ${scopes}`.trim(),
      silent_redirect_uri: !isTemplate
        ? environment.auth_settings.silent_redirect_uri
        : [host.replace(/\/$/su, ''), path.replace(/(^\/)|(\/$)/sug, ''), environment.auth_settings.silent_redirect_uri].filter(x => x?.length > 0).join('/'),
      useRefreshToken: environment.auth_settings.useRefreshToken,
      extraQueryParams: environment.auth_settings.extraQueryParams,
    } as IAuthSettings,
    culture: !isTemplate ? environment.culture : culture,
    isTemplate: environment.isTemplate,
    production: environment.production,
    version: version || '1.0.0',
  };
}

/** Singleton, resolved once at module load (the <app-root> element exists before the bottom-of-body scripts run). */
export const settings = createAppSettings();
