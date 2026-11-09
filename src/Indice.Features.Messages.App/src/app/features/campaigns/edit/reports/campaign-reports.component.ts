import { DecimalPipe, PercentPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { LucideChartPie, LucideMousePointerClick, LucideSend, LucideTarget, LucideUsers } from '@lucide/angular';
import { CampaignDetailsMetrics, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { ChannelIconComponent, channelColor, channelLabel } from 'src/app/ui/badges/badges';
import { DoughnutChartComponent, DoughnutChartItem } from 'src/app/ui/charts/doughnut-chart.component';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';

interface ChannelRow {
  kind: string;
  total: number;
  failures: number;
  share: number;
  successRate: number;
}

/** Reports tab: reach / coverage / engagement KPIs and the per channel delivery breakdown. */
@Component({
  selector: 'app-campaign-reports',
  imports: [DecimalPipe, PercentPipe, TranslatePipe, ChannelIconComponent, DoughnutChartComponent, EmptyStateComponent,
    LucideChartPie, LucideMousePointerClick, LucideSend, LucideTarget, LucideUsers],
  template: `
    @let recipient = metrics()?.recipient;
    <div class="space-y-6">
      <div>
        <h2 class="text-lg font-semibold">{{ 'Campaigns.ReportsHeader' | translate }}</h2>
        <p class="text-sm text-base-content/70">{{ 'Campaigns.ReportsDescription' | translate }}</p>
      </div>

      @if (loading()) {
        <div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          @for (row of [1, 2, 3, 4]; track row) { <div class="skeleton h-28 w-full"></div> }
        </div>
        <div class="skeleton h-72 w-full"></div>
      } @else {
        <div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <div class="stat rounded-box border border-base-300 bg-base-100">
            <div class="stat-figure text-primary"><svg lucideUsers class="size-7"></svg></div>
            <div class="stat-title">{{ 'Campaigns.TotalRecipientReachLabel' | translate }}</div>
            <div class="stat-value tabular">{{ (recipient?.reached ?? 0) | number }}</div>
            <div class="stat-desc">{{ 'Campaigns.OutOfConnector' | translate }} <span class="tabular font-medium">{{ (recipient?.total ?? 0) | number }}</span> {{ 'Campaigns.TotalSuffix' | translate }}</div>
          </div>
          <div class="stat rounded-box border border-base-300 bg-base-100">
            <div class="stat-figure">
              <div class="radial-progress text-success text-xs" role="progressbar" [attr.aria-valuenow]="coveragePercent()"
                [style.--value]="coveragePercent()" style="--size: 3.25rem; --thickness: 5px">{{ coveragePercent() }}%</div>
            </div>
            <div class="stat-title">{{ 'Campaigns.AvgReachabilityLabel' | translate }}</div>
            <div class="stat-value tabular text-success">{{ (recipient?.coverage ?? 0) | percent: '1.0-1' }}</div>
            <div class="stat-desc">{{ 'Campaigns.CoverageHint' | translate }}</div>
          </div>
          <div class="stat rounded-box border border-base-300 bg-base-100">
            <div class="stat-figure text-info"><svg lucideTarget class="size-7"></svg></div>
            <div class="stat-title">{{ 'Campaigns.EngagedLabel' | translate }}</div>
            <div class="stat-value tabular">{{ (recipient?.engaged ?? 0) | number }}</div>
            <div class="stat-desc">{{ rate(recipient?.engaged, recipient?.reached) | percent: '1.0-1' }} {{ 'Campaigns.OfReachedSuffix' | translate }}</div>
          </div>
          <div class="stat rounded-box border border-base-300 bg-base-100">
            <div class="stat-figure text-accent"><svg lucideMousePointerClick class="size-7"></svg></div>
            <div class="stat-title">{{ 'Campaigns.ActedLabel' | translate }}</div>
            <div class="stat-value tabular">{{ (recipient?.acted ?? 0) | number }}</div>
            <div class="stat-desc">{{ rate(recipient?.acted, recipient?.reached) | percent: '1.0-1' }} {{ 'Campaigns.OfReachedSuffix' | translate }}</div>
          </div>
        </div>

        <section class="panel p-5 sm:p-6">
          <div class="mb-5 flex flex-wrap items-end justify-between gap-3">
            <div>
              <h3 class="font-semibold">{{ 'Campaigns.ChannelBreakdownHeader' | translate }}</h3>
              <p class="text-sm text-base-content/70">{{ 'Campaigns.TotalMessagesSentLabel' | translate }}</p>
            </div>
            <div class="flex items-center gap-2 text-3xl font-bold tabular text-primary">
              <svg lucideSend class="size-6"></svg>{{ (recipient?.totalMessages ?? 0) | number }}
            </div>
          </div>
          @if (channels().length) {
            <div class="grid items-center gap-8 md:grid-cols-[16rem_1fr]">
              <div class="relative mx-auto size-64 max-w-full">
                <app-doughnut-chart class="size-full" [items]="chartItems()" />
                <div class="pointer-events-none absolute inset-0 grid place-items-center text-center">
                  <div>
                    <p class="text-2xl font-bold tabular">{{ totalDelivered() | number }}</p>
                    <p class="text-xs text-base-content/60">{{ 'Campaigns.MessagesLabel' | translate }}</p>
                  </div>
                </div>
              </div>
              <ul class="divide-y divide-base-300">
                @for (row of channels(); track row.kind) {
                  <li class="grid grid-cols-[auto_1fr_auto] items-center gap-x-4 gap-y-1.5 py-3">
                    <app-channel-icon [kind]="row.kind" [label]="true" />
                    <progress class="progress h-2 w-full" [value]="row.share * 100" max="100" [style.color]="color(row.kind)"></progress>
                    <span class="text-right font-semibold tabular">{{ row.total | number }}</span>
                    <span></span>
                    <span class="text-xs text-base-content/60 tabular">
                      {{ row.share | percent: '1.0-1' }} {{ 'Campaigns.OfMessagesSuffix' | translate }}
                      · <span [class]="row.failures ? 'text-error' : ''">{{ row.failures | number }} {{ 'Campaigns.FailuresLabel' | translate }}</span>
                    </span>
                    <span class="badge badge-sm tabular" [class]="row.successRate >= 0.95 ? 'badge-soft badge-success' : row.successRate >= 0.8 ? 'badge-soft badge-warning' : 'badge-soft badge-error'">
                      {{ row.successRate | percent: '1.0-0' }}
                    </span>
                  </li>
                }
              </ul>
            </div>
          } @else {
            <app-empty-state [title]="'Campaigns.NoReportDataTitle' | translate" [message]="'Campaigns.NoReportDataMessage' | translate">
              <svg icon lucideChartPie class="size-5"></svg>
            </app-empty-state>
          }
        </section>
      }
    </div>
  `
})
export class CampaignReportsComponent implements OnInit {
  private readonly _api = inject(MessagesApiClient);
  private readonly _route = inject(ActivatedRoute);
  private readonly _translate = inject(TranslateService);

  protected readonly loading = signal(true);
  protected readonly metrics = signal<CampaignDetailsMetrics | undefined>(undefined);
  protected readonly coveragePercent = computed(() => Math.round((this.metrics()?.recipient?.coverage ?? 0) * 100));
  protected readonly channels = computed<ChannelRow[]>(() => {
    const items = (this.metrics()?.channels ?? []).filter(c => (c.total ?? 0) > 0);
    const sum = items.reduce((total, c) => total + (c.total ?? 0), 0);
    return items.map(c => ({
      kind: c.kind ?? 'None',
      total: c.total ?? 0,
      failures: c.failures ?? 0,
      share: sum ? (c.total ?? 0) / sum : 0,
      successRate: c.total ? Math.max(0, (c.total - (c.failures ?? 0)) / c.total) : 0
    }));
  });
  protected readonly totalDelivered = computed(() => this.channels().reduce((total, c) => total + c.total, 0));
  protected readonly chartItems = computed<DoughnutChartItem[]>(() =>
    this.channels().map(c => ({ name: this._translate.instant(channelLabel(c.kind)), value: c.total, color: channelColor(c.kind) })));

  public ngOnInit(): void {
    const campaignId = this._route.parent?.snapshot.paramMap.get('campaignId');
    if (!campaignId) {
      return;
    }
    this._api.getCampaignStatistics(campaignId).subscribe({
      next: metrics => {
        this.metrics.set(metrics);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  protected rate(part: number | undefined, whole: number | undefined): number {
    return whole ? (part ?? 0) / whole : 0;
  }

  protected color(kind: string): string {
    return channelColor(kind);
  }
}
