import { Component } from '@angular/core';

import { environment } from 'src/environments/environment';
import { MenuService } from 'src/app/layout/services/menu.service';
import { MenuItem } from 'src/app/layout/services/models/menu-item';

@Component({
    selector: 'app-side-menu',
    templateUrl: './side-menu.component.html'
})
export class SideMenuComponent {
    constructor(private _menuService: MenuService) {
        this.menuItems = this._menuService.getMenuItems();
    }

    public menuItems: MenuItem[];
    public apiDocsUrl = environment.api_docs;

    public toggleMenuItem(menuItem: MenuItem): void {
        this._menuService.toggleMenuItem(menuItem);
    }

    public toggleMenu(): void {
        this._menuService.toggleSideMenu();
    }
}
