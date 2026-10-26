import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe, NgTemplateOutlet, PercentPipe } from '@angular/common';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { Observable, catchError, combineLatest, filter, map, of, switchMap, take } from 'rxjs';
import {
  LucideAppWindow, LucideArrowDownRight, LucideArrowRight, LucideArrowUpRight, LucideBan, LucideBoxes, LucideClockAlert, LucideDynamicIcon,
  LucideExternalLink, LucideFlag, LucideIcon, LucideIdCard, LucideKeyRound, LucideLock, LucideLogIn, LucideMinus, LucideRefreshCw,
  LucideShieldAlert, LucideShieldCheck, LucideUserX, LucideUsers
} from '@lucide/angular';
import { AuthService } from 'src/app/core/services/auth.service';
import {
  BlogItemInfo, DashboardMetricsInfo, IdentityApiService, SeriesTimeFrame, SignInDailyCount, SignInLocationSet
} from 'src/app/core/services/identity-api.service';
import { UiFeaturesService } from 'src/app/core/services/ui-features.service';
import { LineSeries, StackedLineChartComponent } from 'src/app/ui/charts/stacked-line-chart.component';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';
import { StatCardComponent } from 'src/app/ui/stat/stat-card.component';
import { SignInLocationsMapComponent } from './sign-in-locations-map.component';

interface AttentionItem {
  label: string;
  hint: string;
  value: number | undefined;
  icon: LucideIcon;
  tone: string;
  link?: string;
  queryParams?: Record<string, string>;
}

const TIME_FRAMES = [
  { value: SeriesTimeFrame.Last7Days, label: '7 days' },
  { value: SeriesTimeFrame.Last30Days, label: '30 days' },
  { value: SeriesTimeFrame.Last90Days, label: '90 days' }
];

/**
 * Landing page: user base and engagement, sign in health, account security posture and the configuration inventory.
 * Sections the current user may not read (or that the server disabled) are left out.
 */
@Component({
  selector: 'app-dashboard',
  imports: [
    RouterLink, DatePipe, DecimalPipe, PercentPipe, NgTemplateOutlet, PageHeaderComponent, StatCardComponent, StackedLineChartComponent,
    SignInLocationsMapComponent, LucideArrowRight, LucideArrowUpRight, LucideArrowDownRight, LucideMinus, LucideRefreshCw, LucideExternalLink,
    LucideDynamicIcon
  ],
  template: `
    <app-page-header [title]="greeting()" subtitle="Here is what is happening across your identity platform.">
      <div actions class="flex items-center gap-3">
        @if (metrics()?.calculatedAt; as calculatedAt) {
          <span class="text-xs text-base-content/60">Updated {{ calculatedAt | date: 'shortTime' }}</span>
        }
        <button type="button" class="btn btn-ghost btn-sm btn-square" (click)="reload()" aria-label="Refresh" [disabled]="loading()">
          <svg lucideRefreshCw class="size-4" [class.animate-spin]="loading()"></svg>
        </button>
      </div>
    </app-page-header>

    <!-- headline figures -->
    <section class="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
      @if (users(); as users) {
        <app-stat-card label="Users" [value]="users.total | number" [icon]="icons.users" tone="primary" link="/users" [loading]="loading()">
          <span hint>
            <span class="font-semibold text-success">+{{ users.newLast7Days | number }}</span> in the last 7 days
          </span>
        </app-stat-card>
        <app-stat-card label="Signed in today" [value]="users.activeToday | number" [icon]="icons.activity" tone="secondary" [loading]="loading()">
          <span hint class="inline-flex items-center gap-1">
            @if (todayTrend() > 0) {
              <svg lucideArrowUpRight class="size-3.5 text-success"></svg><span class="font-semibold text-success">{{ todayTrend() | number }}</span>
            } @else if (todayTrend() < 0) {
              <svg lucideArrowDownRight class="size-3.5 text-error"></svg><span class="font-semibold text-error">{{ -todayTrend() | number }}</span>
            } @else {
              <svg lucideMinus class="size-3.5"></svg><span class="font-semibold">0</span>
            }
            vs yesterday
          </span>
        </app-stat-card>
      }
      @if (clients(); as clients) {
        <app-stat-card label="Client applications" [value]="clients.total | number" [icon]="icons.clients" tone="accent" link="/clients" [loading]="loading()">
          <span hint>{{ clients.enabled | number }} enabled · {{ clients.total - clients.enabled | number }} disabled</span>
        </app-stat-card>
      }
      @if (canSeeLogs()) {
        <app-stat-card label="Failed sign-ins (24h)" [value]="failedSignIns() | number" [icon]="icons.failed" tone="error"
          link="/sign-in-logs" [queryParams]="failedQuery" [loading]="failedSignIns() === undefined">
          <span hint>Open the sign-in log to investigate</span>
        </app-stat-card>
      } @else if (users(); as users) {
        <app-stat-card label="Roles" [value]="users.roles | number" [icon]="icons.roles" tone="info" link="/roles" [loading]="loading()">
          <span hint>Groups of users with shared access</span>
        </app-stat-card>
      }
    </section>

    <div class="mt-6 grid gap-6 xl:grid-cols-3">
      <!-- sign in activity -->
      @if (canSeeLogs()) {
        <section class="panel p-5 xl:col-span-2">
          <header class="flex flex-wrap items-start justify-between gap-3">
            <div>
              <h2 class="font-semibold">Sign-in activity</h2>
              <p class="text-sm text-base-content/65">Successful and failed sign-in attempts per day.</p>
            </div>
            <div class="join">
              @for (option of timeFrames; track option.value) {
                <button type="button" class="join-item btn btn-xs" [class.btn-active]="timeFrame() === option.value"
                  (click)="timeFrame.set(option.value)">{{ option.label }}</button>
              }
            </div>
          </header>
          <dl class="mt-4 grid grid-cols-2 gap-4 sm:grid-cols-4">
            <div>
              <dt class="text-xs text-base-content/60">Attempts</dt>
              <dd class="text-xl font-semibold tabular">{{ seriesTotals().attempts | number }}</dd>
            </div>
            <div>
              <dt class="text-xs text-base-content/60">Failure rate</dt>
              <dd class="text-xl font-semibold tabular" [class.text-error]="seriesTotals().failureRate > 0.1">{{ seriesTotals().failureRate | percent: '1.0-1' }}</dd>
            </div>
            <div>
              <dt class="text-xs text-base-content/60">Peak daily users</dt>
              <dd class="text-xl font-semibold tabular">{{ seriesTotals().peakUsers | number }}</dd>
            </div>
            <div>
              <dt class="flex items-center gap-1.5 text-xs text-base-content/60">Legend</dt>
              <dd class="mt-1 flex flex-col gap-0.5 text-xs">
                <span class="inline-flex items-center gap-1.5"><span class="size-2 rounded-full" style="background: var(--app-chart-success)"></span>Successful</span>
                <span class="inline-flex items-center gap-1.5"><span class="size-2 rounded-full" style="background: var(--app-chart-failure)"></span>Failed</span>
              </dd>
            </div>
          </dl>
          <div class="mt-4 h-64">
            @if (series() === undefined) {
              <div class="skeleton h-full w-full"></div>
            } @else {
              <app-stacked-line-chart class="h-full" [labels]="seriesLabels()" [series]="seriesData()" />
            }
          </div>
        </section>
      }

      <!-- engagement -->
      @if (users(); as users) {
        <section class="panel flex flex-col p-5" [class.xl:col-span-3]="!canSeeLogs()">
          <h2 class="font-semibold">Engagement</h2>
          <p class="text-sm text-base-content/65">Share of all users that signed in recently.</p>
          <ul class="mt-5 flex-1 space-y-5">
            @for (row of engagement(); track row.label) {
              <li>
                <div class="mb-1.5 flex items-baseline justify-between gap-3 text-sm">
                  <span>{{ row.label }}</span>
                  <span><span class="font-semibold tabular">{{ row.count | number }}</span>
                    <span class="ml-1 text-xs text-base-content/60 tabular">{{ row.ratio | percent: '1.0-1' }}</span></span>
                </div>
                <progress class="progress progress-secondary h-2 w-full" [value]="row.ratio * 100" max="100"></progress>
              </li>
            }
          </ul>
          <div class="mt-5 flex items-center justify-between rounded-field bg-base-200 px-4 py-3 text-sm">
            <span class="text-base-content/70">Stickiness <span class="text-xs">(daily ÷ monthly)</span></span>
            <span class="font-semibold tabular">{{ stickiness() | percent: '1.0-1' }}</span>
          </div>
        </section>
      }
    </div>

    <div class="mt-6 grid gap-6 lg:grid-cols-2">
      <!-- security posture -->
      @if (users(); as users) {
        <section class="panel p-5">
          <h2 class="font-semibold">Account security</h2>
          <p class="text-sm text-base-content/65">How well protected your users' accounts are.</p>
          <div class="mt-5 flex flex-col gap-6 sm:flex-row sm:items-center">
            <div class="flex flex-col items-center gap-2">
              <div class="radial-progress text-primary" role="progressbar" [attr.aria-valuenow]="ratio(users.twoFactorEnabled) * 100"
                [style.--value]="ratio(users.twoFactorEnabled) * 100" style="--size: 7.5rem; --thickness: 0.6rem">
                <span class="text-center">
                  <span class="block text-2xl font-semibold text-base-content tabular">{{ ratio(users.twoFactorEnabled) | percent: '1.0-0' }}</span>
                  <span class="block text-[0.7rem] text-base-content/60">MFA</span>
                </span>
              </div>
              <a class="link link-hover text-xs text-base-content/70" routerLink="/users" [queryParams]="{ twoFactorEnabled: 'false' }">
                {{ users.total - users.twoFactorEnabled | number }} without MFA
              </a>
            </div>
            <ul class="flex-1 space-y-4">
              <li>
                <div class="mb-1.5 flex justify-between text-sm">
                  <a class="link link-hover" routerLink="/users" [queryParams]="{ emailConfirmed: 'false' }">Verified email</a>
                  <span class="tabular">{{ users.emailConfirmed | number }} <span class="text-xs text-base-content/60">{{ ratio(users.emailConfirmed) | percent: '1.0-0' }}</span></span>
                </div>
                <progress class="progress progress-success h-2 w-full" [value]="ratio(users.emailConfirmed) * 100" max="100"></progress>
              </li>
              <li>
                <div class="mb-1.5 flex justify-between text-sm">
                  <a class="link link-hover" routerLink="/users" [queryParams]="{ phoneNumberConfirmed: 'false' }">Verified phone</a>
                  <span class="tabular">{{ users.phoneNumberConfirmed | number }} <span class="text-xs text-base-content/60">{{ ratio(users.phoneNumberConfirmed) | percent: '1.0-0' }}</span></span>
                </div>
                <progress class="progress progress-success h-2 w-full" [value]="ratio(users.phoneNumberConfirmed) * 100" max="100"></progress>
              </li>
              <li>
                <div class="mb-1.5 flex justify-between text-sm">
                  <span>Signed in at least once</span>
                  <span class="tabular">{{ users.total - users.neverSignedIn | number }} <span class="text-xs text-base-content/60">{{ ratio(users.total - users.neverSignedIn) | percent: '1.0-0' }}</span></span>
                </div>
                <progress class="progress progress-info h-2 w-full" [value]="ratio(users.total - users.neverSignedIn) * 100" max="100"></progress>
              </li>
            </ul>
          </div>
        </section>
      }

      <!-- needs attention -->
      @if (attention().length > 0) {
        <section class="panel p-5">
          <h2 class="font-semibold">Needs attention</h2>
          <p class="text-sm text-base-content/65">Accounts and events an administrator may want to look at.</p>
          <ul class="mt-4 divide-y divide-base-200">
            @for (item of attention(); track item.label) {
              <li>
                <ng-template #row>
                  <span class="grid size-9 shrink-0 place-items-center rounded-field" [class]="item.tone">
                    <svg [lucideIcon]="item.icon" class="size-4"></svg>
                  </span>
                  <span class="min-w-0 flex-1">
                    <span class="block text-sm font-medium">{{ item.label }}</span>
                    <span class="block truncate text-xs text-base-content/60">{{ item.hint }}</span>
                  </span>
                  <span class="text-lg font-semibold tabular">
                    @if (item.value === undefined) { <span class="skeleton inline-block h-5 w-8 align-middle"></span> } @else { {{ item.value | number }} }
                  </span>
                </ng-template>
                @if (item.link) {
                  <a class="-mx-2 flex items-center gap-3 rounded-field px-2 py-2.5 hover:bg-base-200/70" [routerLink]="item.link" [queryParams]="item.queryParams">
                    <ng-container [ngTemplateOutlet]="row" />
                    <svg lucideArrowRight class="size-4 text-base-content/40"></svg>
                  </a>
                } @else {
                  <div class="-mx-2 flex items-center gap-3 px-2 py-2.5"><ng-container [ngTemplateOutlet]="row" /><span class="w-4"></span></div>
                }
              </li>
            }
          </ul>
        </section>
      }
    </div>

    <!-- inventory -->
    @if (inventory().length > 0) {
      <section class="mt-6">
        <h2 class="mb-3 font-semibold">Configuration</h2>
        <div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          @for (item of inventory(); track item.label) {
            <app-stat-card [label]="item.label" [value]="item.value | number" [icon]="item.icon" tone="neutral" [link]="item.link" [loading]="loading()">
              <span hint>{{ item.hint }}</span>
            </app-stat-card>
          }
        </div>
      </section>
    }

    <!-- locations -->
    @if (canSeeLogs() && (locations()?.items?.length ?? 0) > 0) {
      <section class="panel mt-6 p-5">
        <h2 class="font-semibold">Where users sign in from</h2>
        <p class="mb-5 text-sm text-base-content/65">{{ locations()!.count | number }} locations in the last 7 days.</p>
        <app-sign-in-locations-map [locations]="locations()" />
      </section>
    }

    <!-- news -->
    @if (news().length > 0) {
      <section class="mt-6">
        <h2 class="mb-3 font-semibold">From the IdentityServer blog</h2>
        <div class="grid gap-4 md:grid-cols-3">
          @for (post of news(); track post.link) {
            <a class="panel group flex flex-col gap-2 p-5 transition-colors hover:border-primary/40" [href]="post.link" target="_blank" rel="noopener">
              <span class="text-xs text-base-content/60">{{ post.publishDate | date: 'mediumDate' }}</span>
              <span class="font-semibold group-hover:text-primary">{{ post.title }}</span>
              <span class="line-clamp-3 text-sm text-base-content/70">{{ post.description }}</span>
              <span class="mt-auto inline-flex items-center gap-1 pt-1 text-xs font-medium text-primary">Read more <svg lucideExternalLink class="size-3"></svg></span>
            </a>
          }
        </div>
      </section>
    }
  `
})
export class DashboardComponent {
  private readonly _api = inject(IdentityApiService);
  private readonly _auth = inject(AuthService);
  private readonly _features = inject(UiFeaturesService);
  private readonly _date = new DatePipe('en-GB');

  protected readonly icons = {
    users: LucideUsers, activity: LucideLogIn, clients: LucideAppWindow, failed: LucideShieldAlert, roles: LucideShieldCheck
  };
  protected readonly timeFrames = TIME_FRAMES;
  protected readonly failedQuery = { succeeded: 'false', from: isoDate(daysAgo(1)) };

  protected readonly loading = signal(true);
  protected readonly metrics = signal<DashboardMetricsInfo | undefined>(undefined);
  protected readonly users = computed(() => this.metrics()?.users);
  protected readonly clients = computed(() => this.metrics()?.clients);
  protected readonly timeFrame = signal(SeriesTimeFrame.Last30Days);
  protected readonly series = signal<SignInDailyCount[] | undefined>(undefined);
  protected readonly failedSignIns = signal<number | undefined>(undefined);
  protected readonly flaggedSignIns = signal<number | undefined>(undefined);
  protected readonly locations = signal<SignInLocationSet | undefined>(undefined);
  protected readonly news = signal<BlogItemInfo[]>([]);

  protected readonly canSeeLogs = computed(() => {
    this._auth.currentUser();
    return this._auth.isAdmin() && !!this._features.features()?.signInLogsEnabled;
  });

  protected readonly greeting = computed(() => {
    const name = this._auth.currentUser()?.profile?.given_name;
    const hour = new Date().getHours();
    const part = hour < 12 ? 'Good morning' : hour < 18 ? 'Good afternoon' : 'Good evening';
    return name ? `${part}, ${name}` : part;
  });

  protected readonly todayTrend = computed(() => (this.users()?.activeToday ?? 0) - (this.users()?.activeYesterday ?? 0));
  protected readonly engagement = computed(() => {
    const users = this.users();
    return [
      { label: 'Today', count: users?.activeToday ?? 0, ratio: this.ratio(users?.activeToday) },
      { label: 'Last 7 days', count: users?.activeLast7Days ?? 0, ratio: this.ratio(users?.activeLast7Days) },
      { label: 'Last 30 days', count: users?.activeLast30Days ?? 0, ratio: this.ratio(users?.activeLast30Days) }
    ];
  });
  protected readonly stickiness = computed(() => {
    const users = this.users();
    return users?.activeLast30Days ? (users.activeToday ?? 0) / users.activeLast30Days : 0;
  });

  protected readonly seriesLabels = computed(() => (this.series() ?? []).map(point => this._date.transform(point.date, 'MMM d', 'UTC') ?? ''));
  protected readonly seriesData = computed<LineSeries[]>(() => {
    const series = this.series() ?? [];
    return [
      { label: 'Successful', data: series.map(point => point.succeeded), color: 'var(--app-chart-success)' },
      { label: 'Failed', data: series.map(point => point.failed), color: 'var(--app-chart-failure)' }
    ];
  });
  protected readonly seriesTotals = computed(() => {
    const series = this.series() ?? [];
    const succeeded = series.reduce((sum, point) => sum + point.succeeded, 0);
    const failed = series.reduce((sum, point) => sum + point.failed, 0);
    const attempts = succeeded + failed;
    return { attempts, failureRate: attempts ? failed / attempts : 0, peakUsers: Math.max(0, ...series.map(point => point.uniqueUsers)) };
  });

  protected readonly attention = computed<AttentionItem[]>(() => {
    const users = this.users();
    const items: AttentionItem[] = [];
    if (users) {
      items.push(
        { label: 'Locked out', hint: 'Too many failed sign-in attempts', value: users.lockedOut, icon: LucideLock, tone: 'bg-warning/15 text-warning', link: '/users', queryParams: { lockedOut: 'true' } },
        { label: 'Blocked', hint: 'Blocked by an administrator', value: users.blocked, icon: LucideBan, tone: 'bg-error/10 text-error', link: '/users', queryParams: { blocked: 'true' } },
        { label: 'Expired passwords', hint: 'Must change their password at next sign-in', value: users.passwordExpired, icon: LucideClockAlert, tone: 'bg-info/10 text-info' },
        { label: 'Never signed in', hint: 'Accounts that were created but never used', value: users.neverSignedIn, icon: LucideUserX, tone: 'bg-base-200 text-base-content/70' }
      );
    }
    if (this.canSeeLogs()) {
      items.push({
        label: 'Sign-ins flagged for review', hint: 'Marked as suspicious, e.g. impossible travel', value: this.flaggedSignIns(), icon: LucideFlag,
        tone: 'bg-accent/25 text-accent-content dark:text-accent', link: '/sign-in-logs', queryParams: { review: 'true' }
      });
    }
    return items;
  });

  protected readonly inventory = computed(() => {
    const users = this.users();
    const clients = this.clients();
    return [
      ...(users ? [{ label: 'Roles', value: users.roles, hint: 'Groups of users with shared access', icon: LucideShieldCheck, link: '/roles' }] : []),
      ...(clients ? [
        { label: 'Identity resources', value: clients.identityResources, hint: 'User claims clients can request', icon: LucideIdCard, link: '/resources/identity' },
        { label: 'API resources', value: clients.apiResources, hint: 'Protected APIs', icon: LucideBoxes, link: '/resources/api' },
        { label: 'API scopes', value: clients.apiScopes, hint: 'Permissions clients can ask for', icon: LucideKeyRound, link: '/resources/scopes' }
      ] : [])
    ];
  });

  constructor() {
    this.reload();
    this._api.getNews(1, 3).pipe(catchError(() => of(null)), takeUntilDestroyed()).subscribe(news => this.news.set(news?.items ?? []));

    const logsAllowed$ = toObservable(this.canSeeLogs).pipe(filter(Boolean), take(1));
    combineLatest([logsAllowed$, toObservable(this.timeFrame)])
      .pipe(
        switchMap(([, timeFrame]) => {
          this.series.set(undefined);
          return this._api.getSignInSeries(timeFrame).pipe(map(result => result.items ?? []), catchError(() => of([])));
        }),
        takeUntilDestroyed()
      )
      .subscribe(series => this.series.set(series));
    logsAllowed$
      .pipe(
        switchMap(() => combineLatest([
          this.countSignIns({ succeeded: false, from: daysAgo(1) }),
          this.countSignIns({ markForReview: true }),
          this._api.getSignInLocations(SeriesTimeFrame.Last7Days).pipe(catchError(() => of(undefined)))
        ])),
        takeUntilDestroyed()
      )
      .subscribe(([failed, flagged, locations]) => {
        this.failedSignIns.set(failed);
        this.flaggedSignIns.set(flagged);
        this.locations.set(locations);
      });
  }

  protected reload(): void {
    this.loading.set(true);
    this._api.getDashboardMetrics().pipe(catchError(() => of(undefined))).subscribe(metrics => {
      this.metrics.set(metrics);
      this.loading.set(false);
    });
  }

  protected ratio(count: number | undefined): number {
    const total = this.users()?.total ?? 0;
    return total ? (count ?? 0) / total : 0;
  }

  /** Counts sign in log entries matching a filter: a page of size zero only returns the total. */
  private countSignIns(filter: { succeeded?: boolean; markForReview?: boolean; from?: Date }): Observable<number | undefined> {
    return this._api
      .getSignInLogs(1, 0, undefined, undefined, undefined, undefined, filter.markForReview, filter.succeeded, undefined, filter.from)
      .pipe(map(result => result.count ?? 0), catchError(() => of(undefined)));
  }
}

function daysAgo(days: number): Date {
  return new Date(Date.now() - days * 24 * 60 * 60 * 1000);
}

function isoDate(date: Date): string {
  return date.toISOString().slice(0, 10);
}
