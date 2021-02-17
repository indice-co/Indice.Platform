import { Component } from '@angular/core';

import { MenuService } from 'src/app/layout/services/menu.service';
import { MenuItem } from 'src/app/layout/services/models/menu-item';
import * as app from 'src/app/core/models/settings';

@Component({
    selector: 'app-side-menu',
    templateUrl: './side-menu.component.html'
})
export class SideMenuComponent {
    constructor(private menuService: MenuService) {
        this.menuItems = this.menuService.getMenuItems();
    }

    public menuItems: MenuItem[];
    public apiDocsUrl = app.settings.api_docs;
    public authority = app.settings.auth_settings.authority;

    public toggleMenuItem(menuItem: MenuItem): void {
        this.menuService.toggleMenuItem(menuItem);
    }

    public toggleMenu(): void {
        this.menuService.toggleSideMenu();
    }
}
