import { Component, DestroyRef, computed, inject } from '@angular/core';
import { DatePipe, DecimalPipe, NgTemplateOutlet, PercentPipe, SlicePipe } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '@indice/ng-auth';
import { catchError, map, of, switchMap } from 'rxjs';
import {
  LucideCalendarPlus, LucideChevronRight, LucideCircleAlert, LucideCircleCheck, LucideHourglass, LucideInbox, LucideMinus,
  LucideRefreshCw, LucideTrendingDown, LucideTrendingUp, LucideUserCheck
} from '@lucide/angular';
import { settings } from 'src/app/core/models/settings';
import { CasePartial, ReportTag } from 'src/app/core/services/cases-api.service';
import { AppLanguagesService } from 'src/app/shared/services/app-languages.service';
import { BarChartComponent } from './bar-chart.component';
import { BreakdownListComponent } from './breakdown-list.component';
import { CardLoader } from './card-loader';
import { DashboardMetricsService } from './dashboard-metrics.service';

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
    LucideRefreshCw, LucideTrendingDown, LucideTrendingUp, LucideUserCheck
  ],
  templateUrl: './dashboard.component.html'
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
  protected readonly pipeline = new CardLoader(() => this._metrics.pipeline(), this._destroyRef);
  protected readonly intake = new CardLoader(() => this._metrics.intake(14), this._destroyRef);
  protected readonly myQueue = new CardLoader(() => this._metrics.statusCodes$.pipe(
    catchError(() => of(null)), // statuses unknown: fall back to everything assigned to me
    switchMap(codes => {
      const open = codes ? [...codes.Submitted, ...codes.InProgress] : null;
      return this._metrics.myQueue(open).pipe(map(queue => ({ ...queue, open })));
    })
  ), this._destroyRef);
  protected readonly latest = new CardLoader(() => this._metrics.latest(), this._destroyRef);
  protected readonly reports = REPORTS
    .filter(r => this.showReport(r.tag) && (!r.adminOnly || this._auth.isAdmin()))
    .map(r => ({ ...r, loader: new CardLoader(() => this._metrics.report(r.tag), this._destroyRef) }));

  // --- derived views ---
  protected readonly pipelineView = computed(() => {
    const state = this.pipeline.state();
    if (state.status !== 'ready') {
      return null;
    }
    const { submitted, inProgress, completed, codes } = state.value;
    const total = submitted + inProgress + completed;
    const stage = (key: string, label: string, count: number, stageCodes: string[]) => ({
      key, label, count, share: total ? count / total : 0, link: M.casesLink(M.codeFilters(stageCodes)), available: stageCodes.length > 0
    });
    return {
      open: submitted + inProgress,
      openLink: M.casesLink(M.codeFilters([...codes.Submitted, ...codes.InProgress])),
      total,
      stages: [
        stage('submitted', 'dashboard.status.submitted', submitted, codes.Submitted),
        stage('inprogress', 'dashboard.status.inProgress', inProgress, codes.InProgress),
        stage('completed', 'dashboard.status.completed', completed, codes.Completed)
      ]
    };
  });

  protected readonly intakeView = computed(() => {
    const state = this.intake.state();
    if (state.status !== 'ready') {
      return null;
    }
    const days = state.value;
    const format = new Intl.DateTimeFormat(this._languages.selected().toLowerCase(), { weekday: 'short', day: 'numeric', timeZone: 'UTC' });
    const thisWeek = days.slice(-7).reduce((sum, d) => sum + d.count, 0);
    const previousWeek = days.slice(0, -7).reduce((sum, d) => sum + d.count, 0);
    return {
      thisWeek,
      delta: thisWeek - previousWeek,
      categories: days.map(d => format.format(d.date)),
      values: days.map(d => d.count),
      link: M.casesLink([M.fromFilter(M.day(-6))])
    };
  });

  protected readonly myQueueLink = computed(() => {
    const state = this.myQueue.state();
    const userId = this._metrics.userId;
    if (state.status !== 'ready' || !userId) {
      return null;
    }
    return M.casesLink([M.assignedToFilter(userId), ...M.codeFilters(state.value.open ?? [])]);
  });

  protected statusClass(item: CasePartial): string {
    return item.checkpointType?.status ? `stamp-${item.checkpointType.status.toLowerCase()}` : 'stamp-neutral';
  }

  private showReport(tag: ReportTag): boolean {
    return settings.dashboardTags === '' || settings.dashboardTags.split(',').some(t => t.trim() === tag);
  }
}
