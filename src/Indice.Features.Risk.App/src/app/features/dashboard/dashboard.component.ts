import { DatePipe, DecimalPipe, NgTemplateOutlet, PercentPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { rxResource, takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { AuthService } from '@indice/ng-auth';
import {
  LucideActivity, LucideChevronRight, LucideGauge, LucideListChecks, LucideRefreshCw, LucideShieldAlert, LucideTriangleAlert
} from '@lucide/angular';
import { forkJoin, map } from 'rxjs';
import { RiskApiService } from 'src/app/core/services/risk-api.service';
import { DATE_FROM, FilterClause } from 'src/app/ui/list/filter-clause';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { RiskLevelBadgeComponent } from 'src/app/ui/risk/risk-level-badge.component';
import { SidePaneService } from 'src/app/ui/side-pane/side-pane';

type TimeFrame = 'today' | '7d' | '30d';
type Level = 'None' | 'Low' | 'Medium' | 'High';

const TIME_FRAMES: { key: TimeFrame; label: string; days: number }[] = [
  { key: 'today', label: 'Today', days: 1 },
  { key: '7d', label: '7 days', days: 7 },
  { key: '30d', label: '30 days', days: 30 }
];
const LEVELS: Level[] = ['None', 'Low', 'Medium', 'High'];
/** Medium and High: the evaluations someone should look at. */
const ELEVATED = ['riskLevel::neq::None', 'riskLevel::neq::Low'];

/**
 * Landing page: how much activity was evaluated in a time frame, how it splits across risk levels and the latest
 * evaluations that need a look. Every figure comes from the list endpoints (`size=0` + filters) and links to the
 * matching, pre-filtered list.
 */
@Component({
  selector: 'app-dashboard',
  imports: [
    DatePipe, DecimalPipe, PercentPipe, NgTemplateOutlet, RouterLink, RiskLevelBadgeComponent,
    LucideActivity, LucideChevronRight, LucideGauge, LucideListChecks, LucideRefreshCw, LucideShieldAlert, LucideTriangleAlert
  ],
  template: `
    <header class="mb-6 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
      <div class="min-w-0">
        <h1 class="text-2xl font-semibold tracking-tight text-balance sm:text-[1.75rem]">{{ greeting() }}</h1>
        <p class="mt-1 text-sm text-base-content/70">
        <span class="inline-block first-letter:uppercase">{{ today() }}</span>. Here is how risk is trending.
        </p>
      </div>
      <div class="flex items-center gap-2">
        <div class="join" role="group" aria-label="Time frame">
          @for (frame of timeFrames; track frame.key) {
            <button type="button" class="btn join-item btn-sm" [class.btn-active]="timeFrame() === frame.key"
              [attr.aria-pressed]="timeFrame() === frame.key" (click)="timeFrame.set(frame.key)">{{ frame.label }}</button>
          }
        </div>
        <button type="button" class="btn btn-sm btn-ghost btn-square" aria-label="Refresh" title="Refresh" (click)="reload()">
          <svg lucideRefreshCw class="size-4" [class.animate-spin]="busy()"></svg>
        </button>
      </div>
    </header>

    <!-- KPIs -->
    <div class="mb-6 grid gap-4 sm:grid-cols-3">
      <a class="panel group p-5 transition-colors hover:border-primary/40" routerLink="/risk-events" [queryParams]="link()">
        <span class="flex items-center justify-between gap-3">
          <span class="text-sm font-semibold text-base-content/70">Events received</span>
          <span class="grid size-9 place-items-center rounded-full bg-primary/10 text-primary"><svg lucideActivity class="size-4"></svg></span>
        </span>
        @if (counts.hasValue()) {
          <span class="tabular mt-1 block text-3xl font-extrabold tracking-tight group-hover:text-primary">{{ counts.value().events | number }}</span>
          <span class="mt-1 block text-xs text-base-content/60">Transactions and actions reported {{ frameLabel() }}</span>
        } @else if (counts.error()) {
          <span class="mt-2 block text-sm text-base-content/60">Unavailable</span>
        } @else {
          <span class="skeleton mt-2 block h-8 w-24"></span>
        }
      </a>

      <a class="panel group p-5 transition-colors hover:border-primary/40" routerLink="/risk-results" [queryParams]="link('riskLevel::eq::High')"
        [style.border-color]="counts.hasValue() && counts.value().levels.High > 0 ? 'color-mix(in oklab, var(--app-risk-high) 45%, transparent)' : null">
        <span class="flex items-center justify-between gap-3">
          <span class="text-sm font-semibold text-base-content/70">High risk</span>
          <span class="grid size-9 place-items-center rounded-full"
            style="background: color-mix(in oklab, var(--app-risk-high) 14%, transparent); color: var(--app-risk-high)">
            <svg lucideShieldAlert class="size-4"></svg>
          </span>
        </span>
        @if (counts.hasValue()) {
          <span class="tabular mt-1 block text-3xl font-extrabold tracking-tight group-hover:text-primary">{{ counts.value().levels.High | number }}</span>
          <span class="mt-1 block text-xs text-base-content/60">
            <span class="tabular">{{ share('High') | percent: '1.0-1' }}</span> of evaluations {{ frameLabel() }}
          </span>
        } @else if (counts.error()) {
          <span class="mt-2 block text-sm text-base-content/60">Unavailable</span>
        } @else {
          <span class="skeleton mt-2 block h-8 w-24"></span>
        }
      </a>

      <a class="panel group p-5 transition-colors hover:border-primary/40" routerLink="/rules">
        <span class="flex items-center justify-between gap-3">
          <span class="text-sm font-semibold text-base-content/70">Active rules</span>
          <span class="grid size-9 place-items-center rounded-full bg-primary/10 text-primary"><svg lucideListChecks class="size-4"></svg></span>
        </span>
        @if (rules.hasValue()) {
          <span class="tabular mt-1 block text-3xl font-extrabold tracking-tight group-hover:text-primary">{{ activeRules() | number }}</span>
          <span class="mt-1 block text-xs text-base-content/60">Of <span class="tabular">{{ rules.value().length | number }}</span> registered</span>
        } @else if (rules.error()) {
          <span class="mt-2 block text-sm text-base-content/60">Unavailable</span>
        } @else {
          <span class="skeleton mt-2 block h-8 w-24"></span>
        }
      </a>
    </div>

    <!-- Risk level distribution -->
    <section class="panel mb-6 overflow-hidden" aria-labelledby="levels-title">
      @if (counts.error()) {
        <div class="p-5"><ng-container *ngTemplateOutlet="cardError; context: { $implicit: counts }" /></div>
      } @else if (!counts.hasValue()) {
        <div class="p-6" aria-busy="true">
          <div class="skeleton h-4 w-32"></div>
          <div class="skeleton mt-3 h-12 w-40"></div>
          <div class="skeleton mt-6 h-3 w-full"></div>
          <div class="mt-6 grid gap-4 sm:grid-cols-4">
            @for (i of [1, 2, 3, 4]; track i) { <div class="skeleton h-16"></div> }
          </div>
        </div>
      } @else {
        <div class="px-5 pt-5 sm:px-6 sm:pt-6">
          <h2 id="levels-title" class="text-sm font-medium text-base-content/70">Risk evaluations {{ frameLabel() }}</h2>
          <a class="group mt-1 inline-flex items-baseline gap-2" routerLink="/risk-results" [queryParams]="link()">
            <span class="tabular text-5xl font-bold tracking-tight group-hover:text-primary">{{ counts.value().total | number }}</span>
            <span class="text-base-content/70">evaluated</span>
          </a>
          <!-- one segment per level, proportional to its count -->
          <div class="mt-5 flex h-3 w-full gap-0.5 overflow-hidden rounded-full bg-base-200" role="img"
            [attr.aria-label]="distributionLabel()">
            @for (level of levels; track level) {
              @if (counts.value().levels[level] > 0) {
                <span class="h-full" [style.flex-grow]="counts.value().levels[level]" [style.background]="levelColor(level)"></span>
              }
            }
          </div>
        </div>
        <!-- 1px gaps over a border-coloured track draw the cell separators at every breakpoint -->
        <ol class="mt-5 grid gap-px border-t border-base-300 bg-base-300 sm:grid-cols-2 lg:grid-cols-4">
          @for (level of levels; track level) {
            <li class="bg-base-100">
              <a class="flex h-full flex-col gap-2 px-5 py-4 transition-colors hover:bg-base-200/60 sm:px-6"
                routerLink="/risk-results" [queryParams]="link('riskLevel::eq::' + level)">
                <app-risk-level-badge [level]="level" />
                <span class="flex items-baseline gap-2">
                  <span class="tabular text-3xl font-semibold">{{ counts.value().levels[level] | number }}</span>
                  <span class="tabular text-xs text-base-content/60">{{ share(level) | percent: '1.0-0' }}</span>
                </span>
              </a>
            </li>
          }
        </ol>
        @if (counts.value().total === 0) {
          <p class="border-t border-base-300 px-6 py-3 text-sm text-base-content/70">No risk was evaluated {{ frameLabel() }}.</p>
        }
      }
    </section>

    <div class="grid gap-6 lg:grid-cols-5">
      <!-- Latest medium and high risk evaluations -->
      <section class="panel flex flex-col lg:col-span-3" aria-labelledby="review-title">
        <div class="flex items-start justify-between gap-4 px-5 pt-5">
          <div>
            <h2 id="review-title" class="flex items-center gap-2 font-semibold">
              <svg lucideTriangleAlert class="size-4" style="color: var(--app-risk-medium)"></svg>Needs review
            </h2>
            <p class="text-sm text-base-content/70">Latest medium and high risk evaluations {{ frameLabel() }}.</p>
          </div>
          <a class="btn btn-ghost btn-xs shrink-0" routerLink="/risk-results" [queryParams]="elevatedLink()">View all</a>
        </div>
        <div class="flex-1 px-2 pt-3 pb-2">
          @if (elevated.error()) {
            <div class="px-3 pb-3"><ng-container *ngTemplateOutlet="cardError; context: { $implicit: elevated }" /></div>
          } @else if (!elevated.hasValue()) {
            <ng-container *ngTemplateOutlet="listSkeleton" />
          } @else if (elevated.value().length) {
            <ul>
              @for (item of elevated.value(); track item.id) {
                <li>
                  <a class="grid grid-cols-[auto_1fr_auto] items-center gap-x-3 gap-y-0.5 rounded-field px-3 py-2.5 transition-colors hover:bg-base-200/70"
                    [routerLink]="pane.link(['result', item.id!])">
                    <span class="row-span-2 flex flex-col items-start gap-1 sm:row-span-1 sm:flex-row sm:items-center sm:gap-2">
                      <app-risk-level-badge [level]="item.riskLevel" />
                      <span class="tabular w-10 text-sm font-semibold">{{ item.riskScore ?? '—' }}</span>
                    </span>
                    <span class="min-w-0">
                      <span class="block truncate text-sm font-medium">{{ item.name || item.type || '—' }}</span>
                      <span class="block truncate font-mono text-xs text-base-content/60">{{ item.subjectId || '—' }}</span>
                    </span>
                    <span class="flex items-center gap-1 text-right">
                      <span>
                        @if (item.amount != null) { <span class="tabular block text-sm font-medium">{{ item.amount | number: '1.2-2' }}</span> }
                        <span class="tabular block text-xs whitespace-nowrap text-base-content/60">{{ item.createdAt | date: 'dd MMM, HH:mm' }}</span>
                      </span>
                      <svg lucideChevronRight class="size-4 shrink-0 text-base-content/40"></svg>
                    </span>
                  </a>
                </li>
              }
            </ul>
          } @else {
            <p class="flex items-center gap-2 px-3 py-6 text-sm text-base-content/70">
              <svg lucideGauge class="size-4 shrink-0"></svg>Nothing above low risk {{ frameLabel() }}.
            </p>
          }
        </div>
      </section>

      <!-- Rules -->
      <section class="panel flex flex-col lg:col-span-2" aria-labelledby="rules-title">
        <div class="flex items-start justify-between gap-4 px-5 pt-5">
          <div>
            <h2 id="rules-title" class="flex items-center gap-2 font-semibold">
              <svg lucideListChecks class="size-4 text-primary"></svg>Rules
            </h2>
            <p class="text-sm text-base-content/70">Rules that score every evaluated event.</p>
          </div>
          <a class="btn btn-ghost btn-xs shrink-0" routerLink="/rules">Manage</a>
        </div>
        <div class="flex-1 px-2 pt-3 pb-2">
          @if (rules.error()) {
            <div class="px-3 pb-3"><ng-container *ngTemplateOutlet="cardError; context: { $implicit: rules }" /></div>
          } @else if (!rules.hasValue()) {
            <ng-container *ngTemplateOutlet="listSkeleton" />
          } @else if (rules.value().length) {
            <ul>
              @for (rule of rules.value(); track rule.name) {
                <li>
                  <a class="flex items-center gap-3 rounded-field px-3 py-2.5 transition-colors hover:bg-base-200/70"
                    [routerLink]="pane.link(['rule', rule.name!])">
                    <span class="size-2 shrink-0 rounded-full" [class]="rule.enabled ? 'bg-success' : 'bg-base-content/25'"></span>
                    <span class="min-w-0 flex-1">
                      <span class="block truncate text-sm font-medium">{{ rule.friendlyName || rule.name }}</span>
                      @if (rule.description) { <span class="block truncate text-xs text-base-content/60">{{ rule.description }}</span> }
                    </span>
                    <span class="text-xs whitespace-nowrap" [class]="rule.enabled ? 'text-success' : 'text-base-content/50'">
                      {{ rule.enabled ? 'Enabled' : 'Disabled' }}
                    </span>
                  </a>
                </li>
              }
            </ul>
          } @else {
            <p class="px-3 py-6 text-sm text-base-content/70">No rules are registered.</p>
          }
        </div>
      </section>
    </div>

    <ng-template #listSkeleton>
      <div class="space-y-3 px-3 py-2" aria-busy="true">
        @for (i of [1, 2, 3, 4]; track i) {
          <div class="flex items-center gap-4"><div class="skeleton h-5 w-20"></div><div class="skeleton h-4 flex-1"></div><div class="skeleton h-4 w-16"></div></div>
        }
      </div>
    </ng-template>

    <ng-template #cardError let-resource>
      <div class="flex items-center gap-3 text-sm text-base-content/75">
        <svg lucideTriangleAlert class="size-4 shrink-0 text-error"></svg>
        <span class="flex-1">This could not be loaded.</span>
        <button type="button" class="btn btn-ghost btn-xs" (click)="resource.reload()">
          <svg lucideRefreshCw class="size-3.5"></svg>Try again
        </button>
      </div>
    </ng-template>
  `
})
export class DashboardComponent {
  private readonly _api = inject(RiskApiService);
  private readonly _auth = inject(AuthService);
  protected readonly pane = inject(SidePaneService);
  protected readonly timeFrames = TIME_FRAMES;
  protected readonly levels = LEVELS;
  protected readonly timeFrame = signal<TimeFrame>('7d');
  private readonly _todayDate = signal(new Date());
  protected readonly today = computed(() => new Intl.DateTimeFormat(undefined, { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' }).format(this._todayDate()));

  /** Start of the time frame as a `yyyy-MM-dd` date (local), the format the list pages keep in the URL. */
  private readonly _fromDate = computed(() => {
    const days = TIME_FRAMES.find(f => f.key === this.timeFrame())!.days;
    const date = new Date(this._todayDate());
    date.setDate(date.getDate() - (days - 1));
    return `${date.getFullYear()}-${`${date.getMonth() + 1}`.padStart(2, '0')}-${`${date.getDate()}`.padStart(2, '0')}`;
  });
  private readonly _fromClause = computed(() => new FilterClause(DATE_FROM, this._fromDate()));

  protected readonly counts = rxResource({
    params: () => ({ from: this._fromClause().toApiTerm() }),
    stream: ({ params }) => {
      const results = (...filter: string[]) => this._api.getRiskResults([...filter, params.from], 1, 0).pipe(map(r => r.count ?? 0));
      return forkJoin({
        events: this._api.getRiskEvents([params.from], 1, 0).pipe(map(r => r.count ?? 0)),
        total: results(),
        None: results('riskLevel::eq::None'),
        Low: results('riskLevel::eq::Low'),
        Medium: results('riskLevel::eq::Medium'),
        High: results('riskLevel::eq::High')
      }).pipe(map(({ events, total, ...levels }) => ({ events, total, levels: levels as Record<Level, number> })));
    }
  });
  protected readonly elevated = rxResource({
    params: () => ({ from: this._fromClause().toApiTerm() }),
    stream: ({ params }) => this._api.getRiskResults([...ELEVATED, params.from], 1, 6, 'createdAt-').pipe(map(r => r.items ?? []))
  });
  protected readonly rules = rxResource({
    stream: () => this._api.getRiskRules().pipe(map(r => r.items ?? []))
  });

  protected readonly busy = computed(() => this.counts.isLoading() || this.elevated.isLoading() || this.rules.isLoading());
  protected readonly activeRules = computed(() => this.rules.hasValue() ? this.rules.value().filter(r => r.enabled).length : 0);
  protected readonly frameLabel = computed(() => {
    switch (this.timeFrame()) {
      case 'today': return 'today';
      case '7d': return 'in the last 7 days';
      default: return 'in the last 30 days';
    }
  });
  protected readonly elevatedLink = computed(() => this.link(...ELEVATED));
  protected readonly distributionLabel = computed(() => {
    const levels = this.counts.value()?.levels;
    return levels ? LEVELS.map(level => `${level}: ${levels[level]}`).join(', ') : '';
  });

  private readonly _user = toSignal(this._auth.user$, { initialValue: this._auth.getCurrentUser() });
  protected readonly greeting = computed(() => {
    const hour = new Date().getHours();
    const part = hour < 12 ? 'morning' : hour < 18 ? 'afternoon' : 'evening';
    const profile = this._user()?.profile;
    const name = profile?.given_name ?? profile?.name?.split(/\s+/)[0];
    return name ? `Good ${part}, ${name}` : `Good ${part}`;
  });

  constructor() {
    // a rule toggled in its side pane changes the active rule count
    inject(RefreshBus).on('rules').pipe(takeUntilDestroyed()).subscribe(() => this.rules.reload());
  }

  /** Query params of a list page filtered to the time frame plus the given clauses. */
  protected link(...filters: string[]): { filter: string } {
    return { filter: [...filters, this._fromClause().toString()].join(',') };
  }

  protected share(level: Level): number {
    const counts = this.counts.value();
    return counts?.total ? counts.levels[level] / counts.total : 0;
  }

  protected levelColor(level: Level): string {
    return `var(--app-risk-${level.toLowerCase()})`;
  }

  protected reload(): void {
    const previousFromDate = this._fromDate();
    this._todayDate.set(new Date());
    if (previousFromDate === this._fromDate()) {
      this.counts.reload();
      this.elevated.reload();
    }
    this.rules.reload();
  }
}
