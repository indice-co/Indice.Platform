import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';

import { forkJoin } from 'rxjs';
import { HeaderMetaItem, Icons } from '@indice/ng-components';
import { CampaignResultSet, DashboardCounters, MessagesApiClient } from 'src/app/core/services/messages-api.service';

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
  public counters: DashboardCounters | undefined;

    public ngOnInit(): void {
        this.metaItems = [
            { key: 'NG-LIB version :', icon: Icons.DateTime, text: new Date().toLocaleTimeString() }
      ];
      this._api.getDashboardStats().subscribe(stats => {
        this.counters = stats;
        this.loaded = true;
      });
    }

    public navigate(path: string): void {
        this._router.navigateByUrl(path);
    }
}
