import { environment } from 'src/environments/environment';
import { IAppSettings, IAuthSettings } from './settings.model';

function createAppSettings(): IAppSettings {
    const isTemplate = environment.isTemplate;
    let authority = '', clientId = '', host = '', culture = '', version = '', scopes = '', tenantId: string | undefined = '', path = '', enableMediaLibrary = undefined, enableRichTextEditor = undefined;
    if (isTemplate) {
        const appRoot = document.getElementsByTagName('app-root')[0];
        authority = appRoot.getAttribute('authority') || '';
        clientId = appRoot.getAttribute('clientId') || '';
        host = appRoot.getAttribute('host') || '';
        path = appRoot.getAttribute('path') || '';
        culture = appRoot.getAttribute('culture') || '';
        version = appRoot.getAttribute('version') || '';
        scopes = appRoot.getAttribute('scopes') || '';
        tenantId = appRoot.getAttribute('tenantId') || undefined;
        enableMediaLibrary = appRoot.getAttribute('enableMediaLibrary') || undefined;
        enableRichTextEditor = appRoot.getAttribute('enableRichTextEditor') || undefined;
        if (!authority || !clientId || !host) {
            throw new Error('Please provide authority, clientId and baseAddress as properties of app-root element.');
        }
        appRoot.attributes.removeNamedItem('authority');
        appRoot.attributes.removeNamedItem('clientId');
        appRoot.attributes.removeNamedItem('host');
        appRoot.attributes.removeNamedItem('path');
        appRoot.attributes.removeNamedItem('culture');
        appRoot.attributes.removeNamedItem('version');
        appRoot.attributes.removeNamedItem('scopes');
        appRoot.attributes.removeNamedItem('tenantId');
        appRoot.attributes.removeNamedItem('enableMediaLibrary');
        appRoot.attributes.removeNamedItem('enableRichTextEditor');
    }
    return {
        api_url: !isTemplate ? environment.api_url : host,
        auth_settings: {
            accessTokenExpiringNotificationTime: environment.auth_settings.accessTokenExpiringNotificationTime,
            authority: !isTemplate ? environment.auth_settings.authority : authority,
            automaticSilentRenew: environment.auth_settings.automaticSilentRenew,
            client_id: !isTemplate ? environment.auth_settings.client_id : clientId,
            filterProtocolClaims: environment.auth_settings.filterProtocolClaims,
            loadUserInfo: environment.auth_settings.loadUserInfo,
            monitorSession: environment.auth_settings.monitorSession,
            post_logout_redirect_uri: !isTemplate ? environment.auth_settings.post_logout_redirect_uri : [host.replace(/\/$/su, ""), path.replace(/(^\/)|(\/$)/sug, ""), environment.auth_settings.post_logout_redirect_uri].filter(x => x?.length > 0).join('/'),
            redirect_uri: !isTemplate ? environment.auth_settings.redirect_uri : [host.replace(/\/$/su, ""), path.replace(/(^\/)|(\/$)/sug, ""), environment.auth_settings.redirect_uri].filter(x => x?.length > 0).join('/'),
            response_type: environment.auth_settings.response_type,
            revokeAccessTokenOnSignout: environment.auth_settings.revokeAccessTokenOnSignout,
            scope: `${environment.auth_settings.scope} ${scopes}`,
            silent_redirect_uri: !isTemplate ? environment.auth_settings.silent_redirect_uri : [ host.replace(/\/$/su, ""), 
                                                                                                 path.replace(/(^\/)|(\/$)/sug, ""), 
                                                                                                 environment.auth_settings.silent_redirect_uri ]
                                                                                                 .filter(x => x?.length > 0)
                                                                                                 .join('/')
        } as IAuthSettings,
        culture: !isTemplate ? environment.culture : culture,
        isTemplate: environment.isTemplate,
        production: environment.production,
        version: version || '1.0.0',
        tenantId: tenantId,
        enableMediaLibrary: enableMediaLibrary ? enableMediaLibrary === 'True' : environment.enableMediaLibrary,
        enableRichTextEditor: enableRichTextEditor ? enableRichTextEditor === 'True': environment.enableRichTextEditor,
    };
}

export const settings = createAppSettings();