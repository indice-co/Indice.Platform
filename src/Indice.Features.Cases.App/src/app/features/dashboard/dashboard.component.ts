import { Component, Inject, OnInit } from '@angular/core';
import { AuthService } from '@indice/ng-auth';
import { HeaderMetaItem } from '@indice/ng-components';
import { User } from 'oidc-client';
import { Subscription } from 'rxjs';
import { settings } from 'src/app/core/models/settings';
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
    public tiles = new Map<string, boolean>([
        [ReportTag.GroupedByCasetype, true],
        [ReportTag.AgentGroupedByCasetype, true],
        [ReportTag.CustomerGroupedByCasetype, true],
        [ReportTag.GroupedByStatus, true],
        [ReportTag.AgentGroupedByStatus, true],
        [ReportTag.CustomerGroupedByStatus, true],
        [ReportTag.GroupedByGroupId, true],
    ]);

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

        if (settings.dashboardTags !== '') {
            this.tiles.forEach((value, tag) => this.tiles.set(tag, false));
            settings.dashboardTags.split(',').forEach(tag => this.tiles.set(tag, true));
        }
    }
}
