import { Component, Inject, OnInit } from '@angular/core';

import { AuthService } from '@indice/ng-auth';
import { IShellConfig, SHELL_CONFIG } from '@indice/ng-components';

@Component({
    selector: 'app-logout',
    templateUrl: './logout.component.html'
})
export class LogOutComponent implements OnInit {
    constructor(
        @Inject(SHELL_CONFIG) public shellConfig: IShellConfig,
        @Inject(AuthService) private authService: AuthService
    ) { }

    public ngOnInit(): void {
        this.authService.signoutRedirect();
    }
}