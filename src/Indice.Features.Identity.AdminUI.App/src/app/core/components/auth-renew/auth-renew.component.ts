import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';

import { AuthService } from '../../services/auth.service';

@Component({
    selector: 'app-auth-renew',
    template: ``,
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class AuthRenewComponent implements OnInit {
    constructor(private _authService: AuthService) { }

    public ngOnInit(): void {
        this._authService.signinSilentCallback().subscribe(_ => {
            this._authService.userHasAccess();
        });
    }
}
