import { Component, Inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '@indice/ng-auth';
import { HeaderMetaItem, Icons } from '@indice/ng-components';
import { User } from 'oidc-client';
import { of, Subscription } from 'rxjs';
import { take, catchError } from 'rxjs/operators';
import { CasesApiService } from 'src/app/core/services/cases-api.service';

@Component({
    selector: 'app-dashboard',
    templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit {
    public metaItems: HeaderMetaItem[] | null = [];
    public loaded = false;
    public cases: any;
    public user: User | null = null;
    public userSub$: Subscription | null = null;

    constructor(
        private router: Router,
        private _api: CasesApiService,
        @Inject(AuthService) private authService: AuthService
    ) { }

    ngOnInit(): void {
        this.authService.loadUser().subscribe((user) => {
            this.user = user;
        }, error => {
            console.error(error);
        });
        // Detect user changes and display / or not user info accordingly...
        this.userSub$ = this.authService.user$.subscribe((user: any) => {
            this.user = user;
        });
        this.metaItems = [];
        const cases$ = this._api
            .getCases(
                undefined,
                undefined,
                undefined,
                undefined,
                undefined,
                undefined,
                undefined,
                undefined,
                1,
                0,
                undefined,
                undefined,
                undefined
            )
            .pipe(
                take(1),
                catchError((err) => {
                    return of({ error: err, isError: true });
                }));

        cases$.subscribe(results => {
            this.cases = results;
            this.loaded = true;
        });
    }

    public navigate(path: string): void {
        this.router.navigateByUrl(path);
    }
}
