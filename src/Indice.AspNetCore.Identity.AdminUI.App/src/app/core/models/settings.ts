import { environment } from 'src/environments/environment';
import { IAppSettings, IAuthSettings } from './settings.model';

function createAppSettings(): IAppSettings {
    const isTemplate = environment.isTemplate;
    let authority, clientId, host, baseHref, culture;
    if (isTemplate) {
        const appRoot = document.getElementsByTagName('app-root')[0];
        authority = appRoot.getAttribute('authority');
        clientId = appRoot.getAttribute('clientId');
        host = appRoot.getAttribute('host');
        baseHref = appRoot.getAttribute('baseHref').trim
        culture = appRoot.getAttribute('culture');
        if (!authority || !clientId || !host) {
            throw new Error('Please provide authority, clientId and baseAddress as properties of app-root element.');
        }
        appRoot.attributes.removeNamedItem('authority');
        appRoot.attributes.removeNamedItem('clientId');
        appRoot.attributes.removeNamedItem('host');
        appRoot.attributes.removeNamedItem('baseHref');
        appRoot.attributes.removeNamedItem('culture');
    }
    return {
        api_url: !isTemplate ? environment.api_url : authority,
        api_docs: !isTemplate ? environment.api_docs : `${authority}/${environment.api_docs}`,
        auth_settings: {
            authority: !isTemplate ? environment.auth_settings.authority : authority,
            client_id: !isTemplate ? environment.auth_settings.client_id : clientId,
            filterProtocolClaims: environment.auth_settings.filterProtocolClaims,
            loadUserInfo: environment.auth_settings.loadUserInfo,
            post_logout_redirect_uri: !isTemplate ? environment.auth_settings.post_logout_redirect_uri : `${host}/${baseHref}/${environment.auth_settings.post_logout_redirect_uri}`,
            redirect_uri: !isTemplate ? environment.auth_settings.redirect_uri : `${host}/${baseHref}/${environment.auth_settings.redirect_uri}`,
            response_type: environment.auth_settings.response_type,
            scope: environment.auth_settings.scope,
            silent_redirect_uri: !isTemplate ? environment.auth_settings.silent_redirect_uri : `${host}/${baseHref}/${environment.auth_settings.silent_redirect_uri}`
        } as IAuthSettings,
        culture: !isTemplate ? environment.culture : culture,
        isTemplate: environment.isTemplate,
        production: environment.production
    };
}

export const settings = createAppSettings();