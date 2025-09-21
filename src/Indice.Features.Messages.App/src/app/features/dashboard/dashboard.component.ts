import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';

import { forkJoin } from 'rxjs';
import { HeaderMetaItem, Icons } from '@indice/ng-components';
import { OverviewMetrics, MessagesApiClient } from 'src/app/core/services/messages-api.service';

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
  public metrics: OverviewMetrics | undefined;
  public gaugeChannels: { name: string; value: number; color: string; }[] = [];
  public ngOnInit(): void {
    this.metaItems = [
      { key: 'NG-LIB version :', icon: Icons.DateTime, text: new Date().toLocaleTimeString() }
    ];
    this._api.getOverview().subscribe({
      next: overview => {
        console.debug('[Dashboard] Overview received', overview);
        this.metrics = overview;
        this.gaugeChannels = this.buildGaugeChannels(overview);
        this.loaded = true;
        // Manually mark for check since we are OnPush
        this._cdr.markForCheck();
      },
      error: err => {
        console.error('[Dashboard] Failed to load Overview', err);
        this.loaded = true;
        this._cdr.markForCheck();
      }
    });
  }

  public navigate(path: string): void {
    this._router.navigateByUrl(path);
  }

  private buildGaugeChannels(metrics: OverviewMetrics) {
    if (!metrics?.channels) return [];
    return metrics.channels.map(x => {
      return {
        name: x.kind!,
        value: x.total || 0,
        color: (x.kind === 'Email' ? '#5985ee' :
                x.kind === 'SMS' ? '#46cd93' :
                x.kind === 'PushNotification' ? '#fdba45' :
               '#4bbbce')
      };
    }).filter(x => x.value > 0);
  }

}
