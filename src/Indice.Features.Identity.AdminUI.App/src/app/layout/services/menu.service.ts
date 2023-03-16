import { Injectable } from '@angular/core';
import { Router } from '@angular/router';

import { Subject } from 'rxjs';
import { MenuItem } from './models/menu-item';
import { AuthService } from 'src/app/core/services/auth.service';

@Injectable()
export class MenuService {
    private _currentUrl: string;
    private _menuItems: MenuItem[] = [];
    private _menuToggled = new Subject();

    constructor(private _router: Router, private _authService: AuthService) {
        this._currentUrl = this._router.url;
        const isAdmin = this._authService.isAdmin();
        const canReadUsers = this._authService.isAdminUIUsersReader();
        const canWriteUsers = this._authService.isAdminUIUsersWriter();
        const canReadClients = this._authService.isAdminUIClientsReader();
        const canWriteClients = this._authService.isAdminUIClientsWriter();
        this._menuItems.push(...[
            new MenuItem('Dashboard', '/app/dashboard', canReadUsers || canReadClients, 'home'),
            new MenuItem('Users', undefined, canReadUsers, 'group', this.isActiveMenuItem('/app/users'), [
                new MenuItem('Users List', '/app/users', canReadUsers),
                new MenuItem('Add User', '/app/users/add', canWriteUsers)
            ]),
            new MenuItem('Roles', undefined, canReadUsers, 'security', this.isActiveMenuItem('/app/roles'), [
                new MenuItem('Roles List', '/app/roles', canReadUsers),
                new MenuItem('Add Role', '/app/roles/add', canWriteUsers)
            ]),
            new MenuItem('Clients', undefined, canReadClients, 'apps', this.isActiveMenuItem('/app/clients'), [
                new MenuItem('Clients List', '/app/clients', canReadClients),
                new MenuItem('Add Client', '/app/clients/add', canWriteClients)
            ]),
            new MenuItem('Resources', undefined, canReadClients, 'web', this.isActiveMenuItem('/app/resources'), [
                new MenuItem('Identity Resources List', '/app/resources/identity', canReadClients),
                new MenuItem('API Resources List', '/app/resources/api', canReadClients),
                new MenuItem('Add Resource', '/app/resources/add', canWriteClients)
            ]),
            new MenuItem('Claim Types', undefined, canReadUsers || canReadClients, 'perm_identity', this.isActiveMenuItem('/app/claim-types'), [
                new MenuItem('Claims List', '/app/claim-types', canReadUsers || canReadClients),
                new MenuItem('Add Claim', '/app/claim-types/add', canWriteUsers || canWriteClients)
            ]),
            new MenuItem('App Settings', undefined, isAdmin, 'settings_system_daydream', this.isActiveMenuItem('/app/settings'), [
                new MenuItem('App Settings List', '/app/settings', isAdmin),
                new MenuItem('Add App Setting', '/app/settings/add', isAdmin)
            ]),
            //new MenuItem('Sign in Logs', '/app/sign-in-logs', isAdmin, 'book'),
        ]);
    }

    public menuToggled = this._menuToggled.asObservable();

    public getMenuItems(): MenuItem[] {
        return this._menuItems;
    }

    public toggleSideMenu(): void {
        this._menuToggled.next(undefined);
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
