import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';

import { AuthService } from '../../services/auth.service';

@Component({
    selector: 'app-auth-renew',
    template: ``
})
export class AuthRenewComponent implements OnInit {
    constructor(private authService: AuthService, private router: Router) { }

    public ngOnInit(): void {
        this.authService.signinSilentCallback().catch(_ => {
            this.router.navigate(['/unauthorized']);
        });
    }
}
