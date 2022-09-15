import { Component, Inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';

import { AuthService } from '@indice/ng-auth';
import { IShellConfig, SHELL_CONFIG } from '@indice/ng-components';
import { map } from 'rxjs/operators';

@Component({
    selector: 'app-home',
    templateUrl: './home.component.html'
})
export class HomeComponent implements OnInit {
    constructor(
        @Inject(SHELL_CONFIG) public shellConfig: IShellConfig,
        @Inject(AuthService) private _authService: AuthService,
        private _router: Router
    ) { }

    public ngOnInit(): void {
        this._authService
            .isLoggedIn()
            .pipe(map((isLoggedIn: Boolean) => {
                if (isLoggedIn) {
                    this._router.navigate(['dashboard']);
                    return;
                }
                this._authService.signinRedirect({
                    location: '/dashboard'
                });
            }))
            .subscribe();
    }
}