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
