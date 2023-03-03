import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { AuthService } from '../../services/auth.service';

@Component({
    selector: 'app-error',
    templateUrl: './error.component.html'
})
export class ErrorComponent implements OnInit {
    constructor(private _authService: AuthService, private _route: ActivatedRoute) { }

    public statusCode: number;

    public ngOnInit(): void {
        this.statusCode = +this._route.snapshot.data['statusCode'];
    }

    public signOut(): void {
        this._authService.signoutRedirect();
    }
}
