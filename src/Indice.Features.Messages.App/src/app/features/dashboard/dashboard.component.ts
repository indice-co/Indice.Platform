import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';

import { forkJoin } from 'rxjs';
import { HeaderMetaItem, Icons } from '@indice/ng-components';
import { DashboardCounters, MessagesApiClient } from 'src/app/core/services/messages-api.service';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardComponent implements OnInit {
  constructor(
    private _router: Router,
    private _api: MessagesApiClient,
    private _cdr: ChangeDetectorRef
  ) { }

  public metaItems: HeaderMetaItem[] | null = [];
  public loaded = false;
  public counters: DashboardCounters | undefined;
  public gaugeChannels: { name: string; value: number; color: string; }[] = [];
  public ngOnInit(): void {
    this.metaItems = [
      { key: 'NG-LIB version :', icon: Icons.DateTime, text: new Date().toLocaleTimeString() }
    ];
    this._api.getDashboardStats().subscribe({
      next: stats => {
        console.debug('[Dashboard] Stats received', stats);
        this.counters = stats;
        this.gaugeChannels = this.buildGaugeChannels(stats);
        this.loaded = true;
        // Manually mark for check since we are OnPush
        this._cdr.markForCheck();
      },
      error: err => {
        console.error('[Dashboard] Failed to load stats', err);
        this.loaded = true;
        this._cdr.markForCheck();
      }
    });
  }

  public navigate(path: string): void {
    this._router.navigateByUrl(path);
  }

  private buildGaugeChannels(stats: DashboardCounters) {
    if (!stats?.campaignsByType) return [];
    return [
      { name: 'Email', value: stats.campaignsByType.Email ?? 0, color: '#4CAF50' },
      { name: 'SMS', value: stats.campaignsByType.SMS ?? 0, color: '#2196F3' },
      { name: 'Push', value: stats.campaignsByType.PushNotification ?? 0, color: '#FFC107' },
      { name: 'Inbox', value: stats.campaignsByType.Inbox ?? 0, color: '#F44336' }
    ].filter(x => x.value > 0);
  }

}
