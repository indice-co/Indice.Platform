import { MenuOption } from "@indice/ng-components";

export class ExtendedMenuOption extends MenuOption {
    constructor(text: string, value: any, description?: string, data?: any, icon?: string, mode?: MenuOptionMode) {
        super(text, value, description, data, icon);
        this.mode = mode;
    }

    public mode?: MenuOptionMode = MenuOptionMode.View;
}

export enum MenuOptionMode {
    View = 'View',
    Upsert = 'Upsert'
}
