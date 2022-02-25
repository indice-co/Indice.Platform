import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';

import { User } from 'oidc-client';
import { AuthService } from '../../services/auth.service';

@Component({
    selector: 'app-auth-callback',
    templateUrl: './auth-callback.component.html'
})
export class AuthCallbackComponent implements OnInit {
    constructor(private _authService: AuthService, private _router: Router) { }

    public ngOnInit(): void {
        this._authService.signinRedirectCallback().subscribe((user: User) => {
            if (user && this._authService.userHasAccess()) {
                const target = 'app/dashboard';
                this._router.navigateByUrl(user.state.url || target);
            }
        });
    }
}
