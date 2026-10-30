import { environment } from 'src/environments/environment';
import { IAppSettings, IAuthSettings } from './settings.model';

function createAppSettings(): IAppSettings {
    const isTemplate = environment.isTemplate;
    let authority: string, clientId: string, host: string, baseHref: string, culture: string, lang: string, i18nAssets: string, apiDocs: string, version: string, postLogoutRedirectUri: string, scopes: string;
    if (isTemplate) {
        const appRoot = document.getElementsByTagName('app-root')[0];
        authority = appRoot.getAttribute('authority');
        clientId = appRoot.getAttribute('clientId');
        host = appRoot.getAttribute('host');
        baseHref = appRoot.getAttribute('baseHref');
        culture = appRoot.getAttribute('culture');
        lang = appRoot.getAttribute('lang') ?? '';
        i18nAssets = appRoot.getAttribute('i18n-assets') ?? '';
        // left as the raw `%(ApiDocsUrl)` placeholder when the host replaces ConfigureIndexParameters
        apiDocs = appRoot.getAttribute('api-docs')?.replace(/^%\(.*\)$/u, '') ?? '';
        version = appRoot.getAttribute('version');
        postLogoutRedirectUri = appRoot.getAttribute('postLogoutRedirectUri');
        scopes = appRoot.getAttribute('scopes');
        if (!authority || !clientId || !host) {
            throw new Error('Please provide authority, clientId and baseAddress as properties of app-root element.');
        }
        appRoot.attributes.removeNamedItem('authority');
        appRoot.attributes.removeNamedItem('clientId');
        appRoot.attributes.removeNamedItem('host');
        appRoot.attributes.removeNamedItem('baseHref');
        appRoot.attributes.removeNamedItem('culture');
        appRoot.attributes.removeNamedItem('lang');
        appRoot.attributes.removeNamedItem('i18n-assets');
        appRoot.removeAttribute('api-docs');
        appRoot.attributes.removeNamedItem('version');
        appRoot.attributes.removeNamedItem('postLogoutRedirectUri');
        appRoot.attributes.removeNamedItem('scopes');
    }
    var settings = {
        api_url: !isTemplate ? environment.api_url : authority,
        api_docs: !isTemplate ? environment.api_docs : resolveUrl(authority, apiDocs || environment.api_docs),
        auth_settings: {
            authority: !isTemplate ? environment.auth_settings.authority : authority,
            client_id: !isTemplate ? environment.auth_settings.client_id : clientId,
            filterProtocolClaims: environment.auth_settings.filterProtocolClaims,
            loadUserInfo: environment.auth_settings.loadUserInfo,
            post_logout_redirect_uri: !isTemplate 
                ? environment.auth_settings.post_logout_redirect_uri 
                : [
                    host.replace(/\/$/su, ""), 
                    baseHref.replace(/(^\/)|(\/$)/sug, ""), 
                    environment.auth_settings.post_logout_redirect_uri
                ].filter(x => x?.length > 0).join('/'),
            redirect_uri: !isTemplate 
                ? environment.auth_settings.redirect_uri 
                : [
                    host.replace(/\/$/su, ""), 
                    baseHref.replace(/(^\/)|(\/$)/sug, ""), 
                    environment.auth_settings.redirect_uri
                ].filter(x => x?.length > 0).join('/'),
            response_type: environment.auth_settings.response_type,
            scope: `${environment.auth_settings.scope || ''} ${scopes || ''}`,
            silent_redirect_uri: !isTemplate 
                ? environment.auth_settings.silent_redirect_uri 
                : [
                    host.replace(/\/$/su, ""), 
                    baseHref.replace(/(^\/)|(\/$)/sug, ""), 
                    environment.auth_settings.silent_redirect_uri 
                ].filter(x => x?.length > 0).join('/'),
            revokeAccessTokenOnSignout: environment.auth_settings.revokeAccessTokenOnSignout,
            accessTokenExpiringNotificationTime: environment.auth_settings.accessTokenExpiringNotificationTime,
            monitorSession: environment.auth_settings.monitorSession,
            automaticSilentRenew: environment.auth_settings.automaticSilentRenew
        } as IAuthSettings,
        culture: !isTemplate ? environment.culture : culture,
        lang: !isTemplate ? 'en' : lang,
        i18n_assets: !isTemplate ? '' : i18nAssets,
        isTemplate: environment.isTemplate,
        production: environment.production,
        version: version || '1.0.0'
    };
    return settings;
}

/** Absolute urls (`https://…`, `//…`) are kept as is; relative ones (`docs`, `/scalar`) are resolved against the base url. */
function resolveUrl(base: string, url: string): string {
    if (/^([a-z][a-z\d+.-]*:)?\/\//iu.test(url)) {
        return url;
    }
    return `${(base ?? '').replace(/\/+$/u, '')}/${url.replace(/^\/+/u, '')}`;
}

export const settings = createAppSettings();
