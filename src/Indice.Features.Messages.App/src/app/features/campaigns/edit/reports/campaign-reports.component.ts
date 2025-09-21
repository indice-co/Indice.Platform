import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { RecipientMetrics, MessagesApiClient } from '../../../../core/services/messages-api.service';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-campaign-reports',
  templateUrl: './campaign-reports.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})

export class CampaignReportsComponent implements OnInit {
  private _campaignId: string | undefined;
  public loaded = false;
  public counters: RecipientMetrics | undefined;


  public gaugeChannels: { name: string; value: number; color: string; }[] = [];
  public gaugeItems: { name: string; value: number; color: string; }[] = [];



  private fillData(stats: RecipientMetrics) {
    if (!this.counters) return [];
    //this.gaugeItems = [
    //  { name: 'Αναγνωσμένα', value: this.counters.readCount ?? 0, color: '#5e6366ff' },
    //  { name: 'Μη Αναγνωσμένα', value: this.counters.notReadCount ?? 0, color: '#2D3B45' }
    //].filter(x => x.value > 0);
    //this.gaugeChannels = [
    //  { name: 'Email', value: this.counters?.messagesperChannel?.Email ?? 0, color: '#4CAF50' },
    //  { name: 'SMS', value: this.counters?.messagesperChannel?.SMS ?? 0, color: '#2196F3' },
    //  { name: 'Push', value: this.counters?.messagesperChannel?.PushNotification ?? 0, color: '#FFC107' },
    //  { name: 'Inbox', value: this.counters?.messagesperChannel?.Inbox ?? 0, color: '#F44336' }
    //].filter(x => x.value > 0);
    return;
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
