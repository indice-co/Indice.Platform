import { Component, Inject, OnInit } from '@angular/core';
import { AuthService } from '@indice/ng-auth';
import { HeaderMetaItem } from '@indice/ng-components';
import { User } from 'oidc-client';
import { Subscription } from 'rxjs';
import { ReportTag } from 'src/app/core/services/cases-api.service';

@Component({
    selector: 'app-dashboard',
    templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit {
    public metaItems: HeaderMetaItem[] | null = [];
    public user: User | null = null;
    public isAdmin: boolean | undefined;
    public userSub$: Subscription | null = null;
    public reportTag = ReportTag;

    constructor(@Inject(AuthService) private authService: AuthService) { }

    ngOnInit(): void {
        this.authService.loadUser().subscribe((user) => {
            this.user = user;
            this.isAdmin = this.authService.isAdmin();
        }, error => {
            console.error(error);
        });
        // Detect user changes and display / or not user info accordingly...
        this.userSub$ = this.authService.user$.subscribe((user: any) => {
            this.user = user;
            this.isAdmin = this.authService.isAdmin();
        });
        this.metaItems = [];
    }

}
