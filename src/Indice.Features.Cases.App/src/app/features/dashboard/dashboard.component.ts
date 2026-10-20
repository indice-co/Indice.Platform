import { Component, DestroyRef, computed, inject } from '@angular/core';
import { DatePipe, DecimalPipe, NgTemplateOutlet, PercentPipe, SlicePipe } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '@indice/ng-auth';
import { catchError, of, switchMap } from 'rxjs';
import {
  LucideCalendarPlus, LucideChevronRight, LucideCircleAlert, LucideCircleCheck, LucideHourglass, LucideInbox, LucideMinus,
  LucideRefreshCw, LucideTrendingDown, LucideTrendingUp, LucideUserCheck, LucideUserX, LucideClockAlert
} from '@lucide/angular';
import { settings } from 'src/app/core/models/settings';
import { CasePartial, ReportTag } from 'src/app/core/services/cases-api.service';
import { AppLanguagesService } from 'src/app/shared/services/app-languages.service';
import { BarChartComponent } from './bar-chart.component';
import { BreakdownListComponent } from './breakdown-list.component';
import { CardLoader } from './card-loader';
import { DashboardMetricsService, STALE_AFTER_DAYS } from './dashboard-metrics.service';

const REPORTS: { tag: ReportTag; title: string; adminOnly?: boolean }[] = [
  { tag: ReportTag.GroupedByCasetype, title: 'dashboard.groupedByCasetype' },
  { tag: ReportTag.GroupedByStatus, title: 'dashboard.groupedByStatus' },
  { tag: ReportTag.AgentGroupedByCasetype, title: 'dashboard.agentGroupedByCasetype' },
  { tag: ReportTag.CustomerGroupedByCasetype, title: 'dashboard.customerGroupedByCasetype' },
  { tag: ReportTag.AgentGroupedByStatus, title: 'dashboard.agentGroupedByStatus' },
  { tag: ReportTag.CustomerGroupedByStatus, title: 'dashboard.customerGroupedByStatus' },
  { tag: ReportTag.GroupedByGroupId, title: 'dashboard.groupedByGroupId', adminOnly: true }
];

const M = DashboardMetricsService;

@Component({
  selector: 'app-dashboard',
  providers: [DashboardMetricsService],
  imports: [
    RouterLink, DatePipe, DecimalPipe, PercentPipe, SlicePipe, NgTemplateOutlet, TranslatePipe, BarChartComponent, BreakdownListComponent,
    LucideCalendarPlus, LucideChevronRight, LucideCircleAlert, LucideCircleCheck, LucideHourglass, LucideInbox, LucideMinus,
    LucideRefreshCw, LucideTrendingDown, LucideTrendingUp, LucideUserCheck, LucideUserX, LucideClockAlert
  ],
  templateUrl: './dashboard.component.html',
  // non-zero "needs attention" figures get the highlighter treatment
  styles: `.attention { box-shadow: inset 0 -0.45em 0 color-mix(in oklab, var(--app-highlight) 45%, transparent); }`
})
export class DashboardComponent {
  private readonly _metrics = inject(DashboardMetricsService);
  private readonly _auth = inject(AuthService);
  private readonly _languages = inject(AppLanguagesService);
  private readonly _destroyRef = inject(DestroyRef);

  private readonly _user = toSignal(this._auth.user$, { initialValue: this._auth.getCurrentUser() });
  protected readonly firstName = computed(() => {
    const profile = this._user()?.profile;
    return profile?.given_name ?? profile?.name?.split(' ')[0] ?? '';
  });
  protected readonly greetingKey = (() => {
    const hour = new Date().getHours();
    return hour < 12 ? 'dashboard.greeting.morning' : hour < 18 ? 'dashboard.greeting.afternoon' : 'dashboard.greeting.evening';
  })();
  protected readonly today = computed(() =>
    new Intl.DateTimeFormat(this._languages.selected().toLowerCase(), { weekday: 'long', day: 'numeric', month: 'long' }).format(new Date()));

  // --- cards (each loads on its own) ---
  /** All headline figures, from the dashboard metrics endpoint. */
  protected readonly metrics = new CardLoader(() => this._metrics.metrics(), this._destroyRef);
  /** Checkpoint codes per status, only needed to link KPIs to the case list; `null` when they could not be loaded. */
  private readonly _codes = toSignal(this._metrics.statusCodes$.pipe(catchError(() => of(null))));
  protected readonly myQueue = new CardLoader(() => this._metrics.statusCodes$.pipe(
    catchError(() => of(null)), // statuses unknown: fall back to everything assigned to me
    switchMap(codes => this._metrics.myQueue(codes ? [...codes.Submitted, ...codes.InProgress] : null))
  ), this._destroyRef);
  protected readonly latest = new CardLoader(() => this._metrics.latest(), this._destroyRef);
  protected readonly reports = REPORTS
    .filter(r => this.showReport(r.tag) && (!r.adminOnly || this._auth.isAdmin()))
    .map(r => ({ ...r, loader: new CardLoader(() => this._metrics.report(r.tag), this._destroyRef) }));

  // --- derived views ---
  private readonly _openCodes = computed(() => {
    const codes = this._codes();
    return codes ? [...codes.Submitted, ...codes.InProgress] : null;
  });

  protected readonly pipelineView = computed(() => {
    const state = this.metrics.state();
    if (state.status !== 'ready') {
      return null;
    }
    const statuses = state.value.statuses;
    const codes = this._codes();
    const submitted = statuses?.submitted ?? 0;
    const inProgress = statuses?.inProgress ?? 0;
    const completed = statuses?.completed ?? 0;
    const total = submitted + inProgress + completed;
    const link = (stageCodes: string[] | undefined) => stageCodes?.length ? M.casesLink(M.codeFilters(stageCodes)) : null;
    const stage = (key: string, label: string, count: number, stageCodes: string[] | undefined) => ({
      key, label, count, share: total ? count / total : 0, link: link(stageCodes)
    });
    return {
      open: submitted + inProgress,
      openLink: link(this._openCodes() ?? undefined),
      total,
      stages: [
        stage('submitted', 'dashboard.status.submitted', submitted, codes?.Submitted),
        stage('inprogress', 'dashboard.status.inProgress', inProgress, codes?.InProgress),
        stage('completed', 'dashboard.status.completed', completed, codes?.Completed)
      ]
    };
  });

  /** Open cases that need someone's attention: nobody assigned, or waiting longer than the stale threshold. */
  protected readonly attentionView = computed(() => {
    const state = this.metrics.state();
    if (state.status !== 'ready') {
      return null;
    }
    const staleAfterDays = state.value.staleAfterDays ?? STALE_AFTER_DAYS;
    const openCodes = this._openCodes();
    return {
      unassigned: state.value.unassigned ?? 0,
      stale: state.value.stale ?? 0,
      staleAfterDays,
      // the list has no "unassigned" filter, so only the stale figure links to it
      staleLink: openCodes?.length ? M.casesLink([...M.codeFilters(openCodes), M.toFilter(M.day(-staleAfterDays))]) : null
    };
  });

  protected readonly intakeView = computed(() => {
    const state = this.metrics.state();
    if (state.status !== 'ready') {
      return null;
    }
    const days = state.value.intake ?? [];
    const format = new Intl.DateTimeFormat(this._languages.selected().toLowerCase(), { weekday: 'short', day: 'numeric' });
    const thisWeek = state.value.newThisWeek ?? 0;
    return {
      thisWeek,
      delta: thisWeek - (state.value.newPreviousWeek ?? 0),
      categories: days.map(d => d.date ? format.format(d.date) : ''),
      values: days.map(d => d.count ?? 0),
      link: M.casesLink([M.fromFilter(M.day(-6))])
    };
  });

  protected readonly myQueueCount = computed(() => {
    const state = this.metrics.state();
    return state.status === 'ready' ? state.value.assignedToMe ?? 0 : null;
  });

  protected readonly myQueueLink = computed(() => {
    const userId = this._metrics.userId;
    return userId ? M.casesLink([M.assignedToFilter(userId), ...M.codeFilters(this._openCodes() ?? [])]) : null;
  });

  protected statusClass(item: CasePartial): string {
    return item.checkpointType?.status ? `stamp-${item.checkpointType.status.toLowerCase()}` : 'stamp-neutral';
  }

  private showReport(tag: ReportTag): boolean {
    return settings.dashboardTags === '' || settings.dashboardTags.split(',').some(t => t.trim() === tag);
  }
}
