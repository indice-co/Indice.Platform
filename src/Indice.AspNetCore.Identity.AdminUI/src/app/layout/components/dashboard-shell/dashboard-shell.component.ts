import { Component, OnInit } from '@angular/core';

import { MenuService } from '../../services/menu.service';

@Component({
    selector: 'app-dashboard-shell',
    templateUrl: './dashboard-shell.component.html',
    providers: [MenuService]
})
export class DashboardShellComponent implements OnInit {
    constructor(private _menuService: MenuService) { }

    public isMenuOpen = true;

    public ngOnInit(): void {
        this._menuService.menuToggled.subscribe(_ => {
            this.isMenuOpen = !this.isMenuOpen;
        });
    }
}
