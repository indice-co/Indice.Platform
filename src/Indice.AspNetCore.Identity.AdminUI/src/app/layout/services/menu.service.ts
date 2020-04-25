import { Injectable } from '@angular/core';
import { Router } from '@angular/router';

import { MenuItem } from './models/menu-item';
import { Subject } from 'rxjs';

@Injectable()
export class MenuService {
    private _currentUrl: string;
    private _menuItems: MenuItem[] = [];
    private _menuToggled = new Subject();

    constructor(private router: Router) {
        this._currentUrl = this.router.url;
        this._menuItems.push(...[
            new MenuItem('Dashboard', '/app/dashboard', 'home'),
            new MenuItem('Users', undefined, 'group', this.isActiveMenuItem('/app/users'), [
                new MenuItem('Users List', '/app/users'),
                new MenuItem('Add User', '/app/users/add')
            ]),
            new MenuItem('Roles', undefined, 'security', this.isActiveMenuItem('/app/roles'), [
                new MenuItem('Roles List', '/app/roles'),
                new MenuItem('Add Role', '/app/roles/add')
            ]),
            new MenuItem('Clients', undefined, 'apps', this.isActiveMenuItem('/app/clients'), [
                new MenuItem('Clients List', '/app/clients'),
                new MenuItem('Add Client', '/app/clients/add')
            ]),
            new MenuItem('Resources', undefined, 'web', this.isActiveMenuItem('/app/resources'), [
                new MenuItem('Identity Resources List', '/app/resources/identity'),
                new MenuItem('API Resources List', '/app/resources/api'),
                new MenuItem('Add Resource', '/app/resources/add')
            ]),
            new MenuItem('Claim Types', undefined, 'perm_identity', this.isActiveMenuItem('/app/claim-types'), [
                new MenuItem('Claims List', '/app/claim-types'),
                new MenuItem('Add Claim', '/app/claim-types/add')
            ]),
            new MenuItem('App Settings', undefined, 'settings_system_daydream', this.isActiveMenuItem('/app/settings'), [
                new MenuItem('App Settings List', '/app/settings'),
                new MenuItem('Add App Setting', '/app/settings/add')
            ])
        ]);
    }

    public menuToggled = this._menuToggled.asObservable();

    public getMenuItems(): MenuItem[] {
        return this._menuItems;
    }

    public toggleSideMenu(): void {
        this._menuToggled.next();
    }

    public toggleMenuItem(menuItem: MenuItem): void {
        this._menuItems.forEach((item: MenuItem) => {
            if (item !== menuItem) {
                item.isOpen = false;
            }
        });
        menuItem.toggle();
    }

    private isActiveMenuItem(menuItemPath: string): boolean {
        return this._currentUrl.indexOf(menuItemPath) >= 0;
    }
}
