import { IShellConfig } from '@indice/ng-components';

export class ShellConfig implements IShellConfig {
    public appLogo = 'assets/images/branding/logo.svg';
    public appLogoAlt = document.title ?? 'ChaniaBank Risk UI';
    public fluid = true;
    public showFooter = false;
    public showHeader = true;
    public showUserNameOnHeader = true;
    public breadcrumb = true;
}
