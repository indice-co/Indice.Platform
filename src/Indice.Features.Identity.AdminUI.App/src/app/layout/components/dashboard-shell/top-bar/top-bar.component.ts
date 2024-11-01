import { Component, OnInit } from "@angular/core";

import { AuthService } from "src/app/core/services/auth.service";
import { MenuService } from "src/app/layout/services/menu.service";
import * as app from "src/app/core/models/settings";

@Component({
  selector: "app-top-bar",
  templateUrl: "./top-bar.component.html",
})
export class TopBarComponent implements OnInit {
  constructor(
    public authService: AuthService,
    private _menuService: MenuService
  ) {}

  public picture: string;
  public displayName: string;

  public ngOnInit(): void {
    const profile = this.authService.getUserProfile();
    this.displayName = this.authService.getDisplayName();
    if (!profile.picture) {
      profile.picture = `${app.settings.auth_settings.authority}/api/my/account/picture/64`;
    }
    this.picture = `${profile.picture}?d=/avatar/${this.displayName}/64`;
  }

  public signOut(): void {
    this.authService.signoutRedirect();
  }

  public toggleMenu(): void {
    this._menuService.toggleSideMenu();
  }
}
