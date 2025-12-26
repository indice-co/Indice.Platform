import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CampaignDetailsMetrics, MessagesApiClient } from '../../../../core/services/messages-api.service';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
    selector: 'app-campaign-reports',
    templateUrl: './campaign-reports.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false
})

export class CampaignReportsComponent implements OnInit {
  private _campaignId: string | undefined;
  public loaded = false;
  public counters: CampaignDetailsMetrics | undefined;


  public gaugeChannels: { name: string; value: number; color: string; }[] = [];
  public gaugeItems: { name: string; value: number; color: string; }[] = [];



  private fillData(stats: CampaignDetailsMetrics) {
    if (!stats) return;

    this.gaugeChannels = stats.channels!.map(x => {
      return {
        name: x.kind!,
        value: x.total || 0,
        color: (x.kind === 'Email' ? '#5985ee' :
                x.kind === 'SMS' ? '#46cd93' :
                x.kind === 'PushNotification' ? '#fdba45' : '#4bbbce')
      };
    }).filter(x => x.value > 0);
  }


  constructor(
    private _router: Router,
    private _activatedRoute: ActivatedRoute,
    private _api: MessagesApiClient,
    private _cdr: ChangeDetectorRef
  ) { }

  public ngOnInit(): void {
    this._campaignId = this._activatedRoute.parent?.snapshot.params['campaignId'];
    this._api.getCampaignStatistics(this._campaignId!).subscribe(stats => {
      this.counters = stats;
      this.loaded = true;
      this.fillData(stats);
      this._cdr.markForCheck();
    });
  }

  public navigate(path: string): void {
    this._router.navigateByUrl(path);
  }
}
