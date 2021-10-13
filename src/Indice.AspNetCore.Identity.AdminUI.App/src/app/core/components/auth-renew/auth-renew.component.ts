import { Component, OnInit } from '@angular/core';

import { AuthService } from '../../services/auth.service';

@Component({
    selector: 'app-auth-renew',
    template: ``
})
export class AuthRenewComponent implements OnInit {
    constructor(private _authService: AuthService) { }

    public ngOnInit(): void {
        this._authService.signinSilentCallback().subscribe(_ => {
            this._authService.userHasAccess();
        });
    }
}
