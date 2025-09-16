import { Component, OnInit } from '@angular/core';
import { CampaignStatistics, MessagesApiClient } from '../../../../core/services/messages-api.service';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-campaign-reports',
  templateUrl: './campaign-reports.component.html'
})

export class CampaignReportsComponent implements OnInit {
  private _campaignId: string | undefined;
  public loaded = false;
  public counters: CampaignStatistics | undefined;

    constructor(
      private _router: Router,
      private _activatedRoute: ActivatedRoute,
      private _api: MessagesApiClient
    ) { }

  public ngOnInit(): void {
    this._campaignId = this._activatedRoute.parent?.snapshot.params['campaignId'];
    this._api.getCampaignStatistics(this._campaignId!).subscribe(stats => {
      this.counters = stats;
      this.loaded = true;
    });
  }

  public navigate(path: string): void {
    this._router.navigateByUrl(path);
  }
}
