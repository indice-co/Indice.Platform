import { IShellConfig, ShellLayoutType } from '@indice/ng-components';

export class ShellConfig implements IShellConfig {
    public appLogo = 'assets/images/branding/indice.png';
    public appLogoAlt = 'Indice';
    public fluid = false;
    public langs = ['EL', 'EN'];
    public showAlertsOnHeader = false;
    public showFooter = true;
    public showHeader = true;
    public showNotifications = false;
    public showUserNameOnHeader = false;
    public layout = ShellLayoutType.Stacked;
}
