import { Component, OnInit } from "@angular/core";

import { AuthService } from "src/app/core/services/auth.service";
import { MenuService } from "src/app/layout/services/menu.service";

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
    this.picture = profile.picture;
    this.displayName = this.authService.getDisplayName();
  }

  public signOut(): void {
    this.authService.signoutRedirect();
  }

  public toggleMenu(): void {
    this._menuService.toggleSideMenu();
  }
}
