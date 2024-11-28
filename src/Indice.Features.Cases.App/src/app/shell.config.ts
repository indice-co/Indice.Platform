import { IShellConfig, ShellLayoutType } from '@indice/ng-components';

export class ShellConfig implements IShellConfig {
    public appLogo = 'assets/images/branding/logo.svg';
    public appLogoAlt = document.title ?? 'Case Management';
    public fluid = true;
    public showFooter = false;
    public showHeader = true;
    public showUserNameOnHeader = true;
    public breadcrumb = true;
    public layout = ShellLayoutType.Sidebar;
    public showPictureOnHeader = true;
}
