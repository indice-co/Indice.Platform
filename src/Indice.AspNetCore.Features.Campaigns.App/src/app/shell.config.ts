import { IShellConfig } from '@indice/ng-components';

export class ShellConfig implements IShellConfig {
    public appLogo = '/assets/images/branding/indice.png';
    public appLogoAlt = 'Indice';
    public fluid = false;
    public showFooter = true;
    public showHeader = true;
    public showUserNameOnHeader = false;
}
