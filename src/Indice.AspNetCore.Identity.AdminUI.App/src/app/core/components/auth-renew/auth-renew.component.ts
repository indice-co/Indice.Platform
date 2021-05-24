import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';

import { AuthService } from '../../services/auth.service';

@Component({
    selector: 'app-auth-renew',
    template: ``
})
export class AuthRenewComponent implements OnInit {
    constructor(private _authService: AuthService, private _router: Router) { }

    public ngOnInit(): void {
        this._authService.signinSilentCallback().subscribe(_ => {
            this._authService.checkUserAccess();
        });
    }
}
