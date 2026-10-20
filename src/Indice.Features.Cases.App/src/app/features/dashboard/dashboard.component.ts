import { DecimalPipe, formatDate } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { rxResource, toSignal } from '@angular/core/rxjs-interop';
import { Params, RouterLink } from '@angular/router';
import { AuthService } from '@indice/ng-auth';
import { NgIcon } from '@ng-icons/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { forkJoin, of } from 'rxjs';
import { map, take } from 'rxjs/operators';
import { settings } from 'src/app/core/models/settings';
import { CaseTypeService } from 'src/app/core/services/case-type.service';
import { CaseStatus, GroupByReportResult, ReportTag } from 'src/app/core/services/cases-api.service';
import { FilterCachingService } from 'src/app/core/services/filter-caching.service';
import { AppLanguagesService } from 'src/app/shared/services/app-languages.service';
import { PageHeaderComponent } from 'src/app/ui/page/page-header.component';
import { CASE_STATUS_ORDER, inkOf, statusLabelKey } from 'src/app/ui/status/case-status';
import { BarListComponent, ChartDatum, CheckpointRouteComponent, ColumnChartComponent, RouteStation, StackedBarComponent } from './charts';
import { ageBands, daysBetween, foldRoute, listQueryParams, percentDelta, startOfDay, weekWindows } from './dashboard-math';
import { DashboardMetricsService } from './dashboard-metrics.service';

type Channel = 'all' | 'agent' | 'customer';

/** The reports behind each channel of the breakdown section. */
const CHANNEL_REPORTS: Record<Channel, { status: ReportTag; caseType: ReportTag; label: string }> = {
  all: { status: ReportTag.GroupedByStatus, caseType: ReportTag.GroupedByCasetype, label: 'dashboard.channel.all' },
  agent: { status: ReportTag.AgentGroupedByStatus, caseType: ReportTag.AgentGroupedByCasetype, label: 'dashboard.channel.agent' },
  customer: { status: ReportTag.CustomerGroupedByStatus, caseType: ReportTag.CustomerGroupedByCasetype, label: 'dashboard.channel.customer' }
};

/** Titles of the report panels (existing keys, so installations that override them keep their wording). */
const REPORT_TITLES: Record<ReportTag, string> = {
  [ReportTag.GroupedByCasetype]: 'dashboard.groupedByCasetype',
  [ReportTag.AgentGroupedByCasetype]: 'dashboard.agentGroupedByCasetype',
  [ReportTag.CustomerGroupedByCasetype]: 'dashboard.customerGroupedByCasetype',
  [ReportTag.GroupedByStatus]: 'dashboard.groupedByStatus',
  [ReportTag.AgentGroupedByStatus]: 'dashboard.agentGroupedByStatus',
  [ReportTag.CustomerGroupedByStatus]: 'dashboard.customerGroupedByStatus',
  [ReportTag.GroupedByGroupId]: 'dashboard.groupedByGroupId'
};

const WEEKS = 8;
/** One count query per open checkpoint; past this many the route shows the two open statuses instead. */
const ROUTE_QUERY_LIMIT = 16;
/** Stops drawn on the route before the quietest fold into "Other". */
const ROUTE_STOPS = 6;

/** A stop of the route with the checkpoint codes it counts. */
interface CountedStop {
  key: string;
  label: string;
  /** Translated at render time instead of `label` (status stops). */
  labelKey?: string;
  status: CaseStatus | 'other';
  codes: string[];
  value: number | null;
}

/**
 * The desk. It answers "where is open work waiting, and for how long?": the finding under the title, open cases and the
 * route of checkpoints they wait at, eight weeks of intake, how long open cases have waited, and the group-by reports
 * (honouring the host's `dashboardTags`). Every figure opens the case list with matching filters.
 */
@Component({
  selector: 'app-dashboard',
  imports: [DecimalPipe, RouterLink, NgIcon, TranslatePipe, PageHeaderComponent, ColumnChartComponent, BarListComponent, StackedBarComponent, CheckpointRouteComponent],
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent {
  private readonly auth = inject(AuthService);
  private readonly metrics = inject(DashboardMetricsService);
  private readonly caseTypeService = inject(CaseTypeService);
  private readonly filterCaching = inject(FilterCachingService);
  private readonly translate = inject(TranslateService);
  /** Re-evaluates translated chart labels when the language changes. */
  private readonly language = toSignal(inject(AppLanguagesService).onLanguageChange());

  private readonly now = new Date();
  private readonly user = toSignal(this.auth.user$, { initialValue: null });
  protected readonly isAdmin = computed(() => !!this.user() && this.auth.isAdmin());
  protected readonly canCreateCase = toSignal(this.caseTypeService.getCanCreateCaseTypes().pipe(take(1), map(result => result.count !== 0)), { initialValue: false });

  // ── Desk ────────────────────────────────────────────────────────────────────

  protected readonly codes = rxResource({ stream: () => this.metrics.checkpointCodes() });

  protected readonly desk = rxResource({
    params: () => this.codes.value() ? { open: this.codes.value()!.open, userId: this.user()?.profile?.sub } : undefined,
    stream: ({ params }) => forkJoin({
      open: this.metrics.count({ checkpointTypeCodes: params.open }),
      mine: params.userId ? this.metrics.count({ checkpointTypeCodes: params.open, assignedTo: params.userId }) : of(null),
      today: this.metrics.count({ window: { from: startOfDay(this.now) } }),
      oldest: this.metrics.oldestOpen(params.open)
    })
  });

  protected readonly openListParams = computed(() => listQueryParams({ checkpointTypeCodes: this.codes.value()?.open }));

  // ── The route: where open cases are waiting ─────────────────────────────────

  protected readonly routeStatuses = [CaseStatus.Submitted, CaseStatus.InProgress];
  protected readonly route = rxResource({
    params: () => this.codes.value(),
    stream: ({ params }) => {
      const stops: Omit<CountedStop, 'value'>[] = params.route.length <= ROUTE_QUERY_LIMIT
        ? params.route.map(stop => ({ key: stop.code, label: stop.title, status: stop.status, codes: [stop.code] }))
        : this.routeStatuses
          .filter(status => params.byStatus[status]?.length)
          .map(status => ({ key: status, label: status, labelKey: statusLabelKey(status), status, codes: params.byStatus[status]! }));
      return this.metrics.counts(stops.map(stop => ({ checkpointTypeCodes: stop.codes })))
        .pipe(map(counts => stops.map((stop, i): CountedStop => ({ ...stop, value: counts[i] }))));
    }
  });

  protected readonly routeStations = computed<RouteStation[] | undefined>(() => {
    this.language();
    const stops = this.route.value();
    if (!stops) {
      return undefined;
    }
    const folded = foldRoute(stops, ROUTE_STOPS, rest => ({
      key: '__other',
      label: '',
      labelKey: 'dashboard.other',
      status: 'other' as const,
      codes: rest.flatMap(stop => stop.codes),
      value: rest.some(stop => stop.value === null) ? null : rest.reduce((sum, stop) => sum + (stop.value ?? 0), 0)
    }));
    return folded.map(stop => ({
      key: stop.key,
      label: stop.labelKey ? this.translate.instant(stop.labelKey) : stop.label,
      status: stop.status,
      value: stop.value,
      queryParams: listQueryParams({ checkpointTypeCodes: stop.codes })
    }));
  });

  /** The fact under the title: open cases waiting past the last age band, or that nothing is waiting that long. */
  protected readonly finding = computed<{ text: string; queryParams?: Params } | null>(() => {
    this.language();
    const open = this.desk.value()?.open;
    const old = this.aging.value()?.[this.bands.length - 1];
    if (open === 0) {
      return { text: this.translate.instant('dashboard.nothingOpen') };
    }
    if (old === undefined || old === null) {
      return null;
    }
    if (old === 0) {
      return { text: this.translate.instant('dashboard.findingNoneOld') };
    }
    return {
      text: this.translate.instant(old === 1 ? 'dashboard.findingOld.one' : 'dashboard.findingOld.other', { count: old }),
      queryParams: this.agingRows()[this.bands.length - 1].queryParams
    };
  });
  protected readonly oldestAge = computed(() => {
    const oldest = this.desk.value()?.oldest;
    return oldest?.createdByWhen ? daysBetween(new Date(oldest.createdByWhen), this.now) : null;
  });

  // ── Intake ──────────────────────────────────────────────────────────────────

  private readonly weekWindows = weekWindows(this.now, WEEKS);
  protected readonly weeks = rxResource({ stream: () => this.metrics.counts(this.weekWindows.map(window => ({ window }))) });
  protected readonly showIntakeTable = signal(false);

  protected readonly intake = computed<ChartDatum[]>(() => {
    this.language();
    const counts = this.weeks.value() ?? [];
    return this.weekWindows.map((window, i) => ({
      key: window.from.toISOString(),
      label: i === WEEKS - 1 ? this.translate.instant('dashboard.thisWeek') : formatDate(window.from, 'dd/MM', 'en-US'),
      detail: `${formatDate(window.from, 'dd/MM/yyyy', 'en-US')} – ${formatDate(window.to, 'dd/MM/yyyy', 'en-US')}`,
      value: counts[i] ?? null,
      queryParams: listQueryParams({ window })
    }));
  });
  protected readonly thisWeek = computed(() => this.weeks.value()?.[WEEKS - 1] ?? null);
  protected readonly weekDelta = computed(() => {
    const counts = this.weeks.value();
    return counts && counts[WEEKS - 1] !== null && counts[WEEKS - 2] !== null ? percentDelta(counts[WEEKS - 1]!, counts[WEEKS - 2]!) : null;
  });
  protected readonly intakeTotal = computed(() => (this.weeks.value() ?? []).reduce<number>((sum, c) => sum + (c ?? 0), 0));

  // ── Aging of open cases ────────────────────────────────────────────────────

  private readonly bands = ageBands(this.now);
  protected readonly aging = rxResource({
    params: () => this.codes.value()?.open,
    stream: ({ params }) => this.metrics.counts(this.bands.map(band => ({ window: band, checkpointTypeCodes: params })))
  });
  protected readonly agingRows = computed<ChartDatum[]>(() => {
    this.language();
    const counts = this.aging.value() ?? [];
    const open = this.codes.value()?.open ?? [];
    return this.bands.map((band, i) => ({
      key: band.label,
      label: this.translate.instant(band.label),
      value: counts[i] ?? null,
      color: `var(--cases-age-${i + 1})`,
      queryParams: listQueryParams({ checkpointTypeCodes: open, window: band })
    }));
  });

  // ── Breakdown (group-by reports) ────────────────────────────────────────────

  private readonly enabledTags = new Set(settings.dashboardTags ? settings.dashboardTags.split(',') : Object.values(ReportTag));
  protected readonly channels = (Object.keys(CHANNEL_REPORTS) as Channel[])
    .filter(channel => this.enabledTags.has(CHANNEL_REPORTS[channel].status) || this.enabledTags.has(CHANNEL_REPORTS[channel].caseType));
  protected readonly channel = signal<Channel>(this.channels[0] ?? 'all');
  protected readonly channelReports = CHANNEL_REPORTS;
  protected readonly statusTag = computed(() => this.enabledTags.has(CHANNEL_REPORTS[this.channel()].status) ? CHANNEL_REPORTS[this.channel()].status : undefined);
  protected readonly caseTypeTag = computed(() => this.enabledTags.has(CHANNEL_REPORTS[this.channel()].caseType) ? CHANNEL_REPORTS[this.channel()].caseType : undefined);
  protected readonly groupTag = computed(() => this.isAdmin() && this.enabledTags.has(ReportTag.GroupedByGroupId) ? ReportTag.GroupedByGroupId : undefined);
  protected readonly reportTitles = REPORT_TITLES;
  protected readonly breakdownColumns = computed(() => [this.statusTag(), this.caseTypeTag(), this.groupTag()].filter(Boolean).length);

  protected readonly statusReport = rxResource({ params: () => this.statusTag(), stream: ({ params }) => this.metrics.report(params) });
  protected readonly caseTypeReport = rxResource({ params: () => this.caseTypeTag(), stream: ({ params }) => this.metrics.report(params) });
  protected readonly groupReport = rxResource({ params: () => this.groupTag(), stream: ({ params }) => this.metrics.report(params) });
  private readonly caseTypes = toSignal(this.caseTypeService.getCaseTypes().pipe(take(1), map(result => result.items ?? [])), { initialValue: [] });

  /** Drill-downs only make sense for all channels: the case list cannot filter by channel. */
  private readonly drillDown = computed(() => this.channel() === 'all');

  protected readonly statusSegments = computed<ChartDatum[]>(() => {
    this.language();
    const report = this.statusReport.value() ?? [];
    const byStatus = this.codes.value()?.byStatus ?? {};
    return CASE_STATUS_ORDER
      .map(status => ({ status, value: report.find(r => r.label === status)?.count ?? 0 }))
      .filter(({ value }) => value > 0)
      .map(({ status, value }) => ({
        key: status,
        label: this.translate.instant(statusLabelKey(status)),
        value,
        color: `var(--cases-status-${status.toLowerCase()})`,
        queryParams: this.drillDown() && byStatus[status]?.length ? listQueryParams({ checkpointTypeCodes: byStatus[status] }) : undefined
      }));
  });

  protected readonly caseTypeBars = computed<ChartDatum[]>(() => {
    const caseTypes = this.caseTypes();
    return this.sorted(this.caseTypeReport.value()).map(row => {
      const matches = caseTypes.filter(caseType => caseType.title === row.label);
      return {
        key: row.label ?? '',
        label: row.label ?? '',
        value: row.count ?? 0,
        queryParams: this.drillDown() && matches.length === 1 ? listQueryParams({ caseTypeCodes: [matches[0].code!] }) : undefined
      };
    });
  });

  protected readonly groupBars = computed<ChartDatum[]>(() => {
    this.language();
    return this.sorted(this.groupReport.value()).map(row => ({
      key: row.label || '__none',
      label: row.label || this.translate.instant('dashboard.noGroup'),
      value: row.count ?? 0,
      queryParams: row.label ? listQueryParams({ groupIds: [row.label] }) : undefined
    }));
  });

  protected readonly otherLabel = computed(() => {
    this.language();
    return this.translate.instant('dashboard.other');
  });

  protected ink(status: CaseStatus): string {
    return inkOf(status);
  }

  protected statusKey(status: CaseStatus): string {
    return statusLabelKey(status);
  }

  /** A drill-down must not be overridden by the filters the case list remembers from the last visit. */
  protected resetListFilters(): void {
    this.filterCaching.resetParams('cases');
  }

  private sorted(report: GroupByReportResult[] | null | undefined): GroupByReportResult[] {
    return [...(report ?? [])].sort((a, b) => (b.count ?? 0) - (a.count ?? 0));
  }
}
