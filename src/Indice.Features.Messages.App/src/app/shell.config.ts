import { IShellConfig, ShellLayoutType } from '@indice/ng-components';

export class ShellConfig implements IShellConfig {
    public appLogo = 'assets/images/logo.png';
    public appLogoAlt = 'Indice';
    public breadcrumb = true;
    public fluid = false;
    public langs = ['EL', 'EN'];
    public layout = ShellLayoutType.Sidebar;
    public showAlertsOnHeader = false;
    public showFooter = true;
    public showHeader = true;
    public showLangsOnHeader = true;
    public showNotifications = false;
    public showUserNameOnHeader = true;
    public showPictureOnHeader = true;
}

export const CommonAppShellConfig: IShellConfig = {
    appLogo: '',
    appLogoAlt: '',
    breadcrumb: true,
    fluid: true,
    layout: ShellLayoutType.Stacked,
    showAlertsOnHeader: false,
    showFooter: false,
    showHeader: false,
    showLangsOnHeader: true,
    showUserNameOnHeader: false,
    showPictureOnHeader: true
};
