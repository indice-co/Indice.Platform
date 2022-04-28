import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';

import { forkJoin } from 'rxjs';
import { HeaderMetaItem, Icons } from '@indice/ng-components';
import { CampaignResultSet, MessagesApiClient } from 'src/app/core/services/messages-api.service';

@Component({
    selector: 'app-dashboard',
    templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit {
    constructor(
        private _router: Router,
        private _api: MessagesApiClient
    ) { }

    public metaItems: HeaderMetaItem[] | null = [];
    public loaded = false;
    public campaignsCount = 0;
    public activeCampaignsCount = 0;

    public ngOnInit(): void {
        this.metaItems = [
            { key: 'NG-LIB version :', icon: Icons.DateTime, text: new Date().toLocaleTimeString() }
        ];
        const campaigns$ = this._api.getCampaigns(undefined, undefined, 1, 0, undefined, undefined);
        const activeCampaigns$ = this._api.getCampaigns(undefined, true, 1, 0, undefined, undefined);
        forkJoin([campaigns$, activeCampaigns$]).subscribe((results: [CampaignResultSet, CampaignResultSet]) => {
            this.campaignsCount = results[0].count || 0;
            this.activeCampaignsCount = results[1].count || 0;
            this.loaded = true;
        });
    }

    public navigate(path: string): void {
        this._router.navigateByUrl(path);
    }
}
