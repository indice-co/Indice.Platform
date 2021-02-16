import { environment } from 'src/environments/environment';
import { IAppSettings, IAuthSettings } from './settings.model';

export function getAppSettings(): IAppSettings {
    const isTemplate = environment.isTemplate;
    let authority, clientId, appBaseAddress;
    if (isTemplate) {
        const appRoot = document.getElementsByTagName('app-root')[0];
        authority = appRoot.getAttribute('authority');
        clientId = appRoot.getAttribute('clientId');
        appBaseAddress = appRoot.getAttribute('baseAddress');
        if (!authority || !clientId || !appBaseAddress) {
            throw new Error('Please provide authority, clientId and baseAddress as properties of app-root element.');
        }
    }
    return {
        api_url: !isTemplate ? environment.api_url : authority,
        api_docs: !isTemplate ? environment.api_docs : `${authority}/${environment.api_docs}`,
        auth_settings: {
            authority: !isTemplate ? environment.auth_settings.authority : authority,
            client_id: !isTemplate ? environment.auth_settings.client_id : clientId,
            filterProtocolClaims: environment.auth_settings.filterProtocolClaims,
            loadUserInfo: environment.auth_settings.loadUserInfo,
            post_logout_redirect_uri: !isTemplate ? environment.auth_settings.post_logout_redirect_uri : appBaseAddress,
            redirect_uri: !isTemplate ? environment.auth_settings.redirect_uri : `${appBaseAddress}/${environment.auth_settings.redirect_uri}`,
            response_type: environment.auth_settings.response_type,
            scope: environment.auth_settings.scope,
            silent_redirect_uri: !isTemplate ? environment.auth_settings.silent_redirect_uri : `${appBaseAddress}/${environment.auth_settings.silent_redirect_uri}`
        } as IAuthSettings,
        isTemplate: environment.isTemplate,
        production: environment.production
    };
}
