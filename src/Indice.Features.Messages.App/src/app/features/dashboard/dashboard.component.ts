import { CampaignsApiService } from 'src/app/core/services/campaigns-api.services';
import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';

import { HeaderMetaItem, Icons } from '@indice/ng-components';
import { forkJoin, of } from 'rxjs';
import { take, catchError } from 'rxjs/operators';

@Component({
    selector: 'app-dashboard',
    templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit {
    constructor(private router: Router, private _api: CampaignsApiService) { }

    public metaItems: HeaderMetaItem[] | null = [];
    public loaded = false;
    public campaigns: any;
    public activeCampaigns: any;

    ngOnInit(): void {
        this.metaItems = [
            { key: 'NG-LIB version :', icon: Icons.DateTime, text: new Date().toLocaleTimeString() }
        ];
        const campaigns$ = this._api.getCampaigns(
            undefined,
            undefined,
            1,
            0,
            undefined,
            undefined
        ).pipe(
            take(1),
            catchError((err) => {
                return of({ error: err, isError: true });
            }));
        const activeCampaigns$ = this._api.getCampaigns(
            undefined,
            true,
            1,
            0,
            undefined,
            undefined
        ).pipe(
            take(1),
            catchError((err) => {
                return of({ error: err, isError: true });
            }));
            
        forkJoin([campaigns$, activeCampaigns$]).subscribe(results => {
            this.campaigns = results[0];
            this.activeCampaigns = results[1];
            this.loaded = true;
        });
    }

    public navigate(path: string): void {
        this.router.navigateByUrl(path);
    }
}
