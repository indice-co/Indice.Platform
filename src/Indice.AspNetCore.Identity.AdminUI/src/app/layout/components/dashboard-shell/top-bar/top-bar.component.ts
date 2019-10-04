import { Component, OnInit } from '@angular/core';

import { AuthService } from 'src/app/core/services/auth.service';
import { MenuService } from 'src/app/layout/services/menu.service';

@Component({
    selector: 'app-top-bar',
    templateUrl: './top-bar.component.html'
})
export class TopBarComponent implements OnInit {
    constructor(private authService: AuthService, private menuService: MenuService) { }

    public displayName: string;

    public ngOnInit(): void {
        this.displayName = this.authService.getDisplayName();
    }

    public signOut(): void {
        this.authService.signoutRedirect();
    }

    public toggleMenu(): void {
        this.menuService.toggleSideMenu();
    }
}
