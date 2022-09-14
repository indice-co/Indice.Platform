import { IShellConfig, ShellLayoutType } from '@indice/ng-components';

export class ShellConfig implements IShellConfig {
    public appLogo = 'assets/images/branding/indice.png';
    public appLogoAlt = 'Indice';
    public breadcrumb = false;
    public fluid = false;
    public langs = ['EL', 'EN'];
    public layout = ShellLayoutType.Sidebar;
    public showAlertsOnHeader = false;
    public showFooter = true;
    public showHeader = true;
    public showLangsOnHeader = true;
    public showNotifications = false;
    public showUserNameOnHeader = true;
}

export const CommonAppShellConfig: IShellConfig = {
    appLogo: '/assets/images/branding/evpulse-dark-white.svg',
    appLogoAlt: 'EVPulse',
    breadcrumb: false,
    fluid: true,
    layout: ShellLayoutType.Stacked,
    showAlertsOnHeader: false,
    showFooter: false,
    showHeader: false,
    showLangsOnHeader: true,
    showUserNameOnHeader: false
};
