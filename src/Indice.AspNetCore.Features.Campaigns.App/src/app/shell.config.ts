import { IShellConfig } from "@indice/ng-components";

export class ShellConfig implements IShellConfig {
    public appLogo = 'https://tailwindui.com/img/logos/workflow-mark.svg?color=white';
    public appLogoAlt = 'Indice';
    public fluid = false;
    public showFooter = true;
    public showHeader = true;
    public showUserNameOnHeader = false;
}
