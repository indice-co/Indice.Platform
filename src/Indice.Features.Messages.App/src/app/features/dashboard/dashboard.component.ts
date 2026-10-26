import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe, NgTemplateOutlet, PercentPipe } from '@angular/common';
import { rxResource, toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AuthService } from '@indice/ng-auth';
import { forkJoin, map, merge } from 'rxjs';
import {
  LucideArrowDownRight, LucideArrowRight, LucideArrowUpRight, LucideBookUser, LucideFilePen, LucideLayoutTemplate,
  LucideMegaphone, LucidePlus, LucideSend, LucideTriangleAlert
} from '@lucide/angular';
import {
  DeliveryInterval, DeliveryMetrics, MessageChannelKind, MessagesApiClient, SeriesTimeFrame
} from 'src/app/core/services/messages-api.service';
import { AppLanguagesService } from 'src/app/shared/services/app-languages.service';
import { CHANNEL_KINDS, ChannelIconComponent, MessageTypeBadgeComponent, channelColor, channelLabel } from 'src/app/ui/badges/badges';
import { DoughnutChartComponent, DoughnutChartItem } from 'src/app/ui/charts/doughnut-chart.component';
import { LineSeries, StackedLineChartComponent } from 'src/app/ui/charts/stacked-line-chart.component';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';

interface Trend {
  /** Relative change vs the previous period, e.g. 0.12 for +12%. */
  change: number;
  /** Whether the change is good news (for failures, going down is good). */
  positive: boolean;
}

const TIME_FRAMES: { value: SeriesTimeFrame; label: string }[] = [
  { value: SeriesTimeFrame.Last24Hours, label: 'DashBoard.Range24h' },
  { value: SeriesTimeFrame.Last7Days, label: 'DashBoard.Range7d' },
  { value: SeriesTimeFrame.Last30Days, label: 'DashBoard.Range30d' },
  { value: SeriesTimeFrame.Last90Days, label: 'DashBoard.Range90d' },
  { value: SeriesTimeFrame.Last12Months, label: 'DashBoard.Range12m' }
];

/**
 * Messaging overview: delivery KPIs (dispatched / failed / success rate with trends vs the previous period), a stacked
 * dispatched vs failed time series, the channel mix, audience reach, top message types and the latest campaigns.
 */
@Component({
  selector: 'app-dashboard',
  imports: [
    RouterLink, DatePipe, DecimalPipe, NgTemplateOutlet, PercentPipe, TranslatePipe, PageHeaderComponent, StackedLineChartComponent, DoughnutChartComponent,
    ChannelIconComponent, MessageTypeBadgeComponent,
    LucideArrowDownRight, LucideArrowRight, LucideArrowUpRight, LucideBookUser, LucideFilePen, LucideLayoutTemplate,
    LucideMegaphone, LucidePlus, LucideSend, LucideTriangleAlert
  ],
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent {
  private readonly _api = inject(MessagesApiClient);
  private readonly _languages = inject(AppLanguagesService);
  private readonly _translate = inject(TranslateService);
  private readonly _user = toSignal(inject(AuthService).user$);
  /** Changes whenever the language or its translations (loaded asynchronously) change, so computed labels re-translate. */
  private readonly _translations = toSignal(merge(this._translate.onLangChange, this._translate.onTranslationChange), { initialValue: null });

  protected readonly timeFrames = TIME_FRAMES;
  protected readonly channels = CHANNEL_KINDS;
  protected readonly channelLabel = channelLabel;
  protected readonly timeFrame = signal(SeriesTimeFrame.Last30Days);
  protected readonly channel = signal<MessageChannelKind | undefined>(undefined);
  protected readonly typesScope = signal<'today' | 'all'>('today');
  protected readonly firstName = computed(() => {
    const profile = this._user()?.profile;
    return profile?.given_name ?? profile?.name?.split(' ')[0] ?? '';
  });

  protected readonly delivery = rxResource({
    params: () => ({ timeFrame: this.timeFrame(), channel: this.channel() }),
    stream: ({ params }) => this._api.getDeliveryMetrics(params.timeFrame, params.channel)
  });

  protected readonly overview = rxResource({ stream: () => this._api.getOverview() });

  /** Workspace counters, read as list counts (size = 0 returns only the count). */
  protected readonly counts = rxResource({
    stream: () => forkJoin({
      drafts: this._api.getCampaigns(1, 0, undefined, undefined, undefined, false).pipe(map(r => r.count ?? 0)),
      distributionLists: this._api.getDistributionLists(1, 0).pipe(map(r => r.count ?? 0)),
      templates: this._api.getTemplates(1, 0).pipe(map(r => r.count ?? 0))
    })
  });

  protected readonly recentCampaigns = rxResource({
    stream: () => this._api.getCampaigns(1, 5, 'createdAt-').pipe(map(r => r.items ?? []))
  });

  private readonly _metrics = computed<DeliveryMetrics | undefined>(() => this.delivery.hasValue() ? this.delivery.value() : undefined);
  protected readonly current = computed(() => this._metrics()?.current);
  protected readonly overviewValue = computed(() => this.overview.hasValue() ? this.overview.value() : undefined);

  protected readonly dispatchedTrend = computed(() => this.trend(this._metrics()?.current?.dispatched, this._metrics()?.previous?.dispatched, false));
  protected readonly failedTrend = computed(() => this.trend(this._metrics()?.current?.failed, this._metrics()?.previous?.failed, true));
  protected readonly failureRate = computed(() => {
    const totals = this.current();
    return totals?.total ? (totals.failed ?? 0) / totals.total : 0;
  });
  protected readonly successRatePercent = computed(() => Math.round((this.current()?.successRate ?? 0) * 1000) / 10);

  protected readonly chartLabels = computed(() => {
    const metrics = this._metrics();
    const lang = this._languages.selected().toLowerCase();
    const format = new Intl.DateTimeFormat(lang, metrics?.interval === DeliveryInterval.Hour
      ? { hour: '2-digit', minute: '2-digit' }
      : metrics?.interval === DeliveryInterval.Month ? { month: 'short', year: '2-digit' } : { day: 'numeric', month: 'short' });
    return (metrics?.items ?? []).map(item => item.date ? format.format(item.date) : '');
  });

  protected readonly chartSeries = computed<LineSeries[]>(() => {
    const items = this._metrics()?.items ?? [];
    this._translations();
    return [
      { label: this._translate.instant('DashBoard.Dispatched'), data: items.map(i => i.dispatched ?? 0), color: 'var(--app-chart-dispatched)' },
      { label: this._translate.instant('DashBoard.Failed'), data: items.map(i => i.failed ?? 0), color: 'var(--app-chart-failed)' }
    ];
  });

  protected readonly perChannel = computed(() => this._metrics()?.perChannel ?? []);
  protected readonly channelMix = computed<DoughnutChartItem[]>(() => {
    this._translations();
    return this.perChannel().map(c => ({
      name: this._translate.instant(channelLabel(c.kind)),
      value: c.total ?? 0,
      color: channelColor(c.kind)
    }));
  });

  protected readonly messageTypes = computed(() => {
    const overview = this.overviewValue();
    return (this.typesScope() === 'today' ? overview?.perTypeToday : overview?.perType) ?? [];
  });

  protected readonly engagementRate = computed(() => {
    const recipient = this.overviewValue()?.recipient;
    return recipient?.reached ? (recipient.engaged ?? 0) / recipient.reached : 0;
  });

  protected readonly knownContactsRate = computed(() => {
    const contact = this.overviewValue()?.contact;
    return contact?.total ? (contact.known ?? 0) / contact.total : 0;
  });

  protected channelColor(kind: string | undefined): string {
    return channelColor(kind);
  }

  protected setChannel(value: string): void {
    this.channel.set(value ? value as MessageChannelKind : undefined);
  }

  private trend(current: number | undefined, previous: number | undefined, lowerIsBetter: boolean): Trend | null {
    if (current === undefined || !previous) {
      return null;
    }
    const change = (current - previous) / previous;
    return { change, positive: lowerIsBetter ? change <= 0 : change >= 0 };
  }
}
