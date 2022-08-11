import { Component, Inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { environment } from 'src/environments/environment';
import { AuthService } from '@indice/ng-auth';
import { IShellConfig, SHELL_CONFIG } from '@indice/ng-components';
import { map } from 'rxjs/operators';

@Component({
    selector: 'app-home',
    templateUrl: './home.component.html'
})
export class HomeComponent implements OnInit {
    env = environment;
    constructor(
        @Inject(SHELL_CONFIG) public shellConfig: IShellConfig,
        @Inject(AuthService) private authService: AuthService,
        private router: Router
    ) { }

    public ngOnInit(): void {
        this.authService
            .isLoggedIn()
            .pipe(map((isLoggedIn: Boolean) => {
                if (isLoggedIn) {
                    this.router.navigate(['/dashboard']);
                    return;
                }
                this.authService.signinRedirect('/dashboard');
            }))
            .subscribe();
    }
}