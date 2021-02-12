import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';

import { AuthService } from '../../services/auth.service';
import { User } from 'oidc-client';

@Component({
    selector: 'app-auth-callback',
    templateUrl: './auth-callback.component.html'
})
export class AuthCallbackComponent implements OnInit {
    constructor(private authService: AuthService, private router: Router) { }

    public ngOnInit(): void {
        this.authService.signinRedirectCallback().then((user: User) => {
            this.authService.isLoggedIn().subscribe((isLoggedIn: boolean) => {
                if (!isLoggedIn) {
                    this.router.navigate(['/unauthorized']);
                    return;
                }
                const target = 'app/dashboard';
                this.router.navigateByUrl(user.state.url || target);
            });
        });
    }
}
