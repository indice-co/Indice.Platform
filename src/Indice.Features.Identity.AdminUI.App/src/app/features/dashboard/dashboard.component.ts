import { Component, LOCALE_ID, computed, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe, NgTemplateOutlet, PercentPipe } from '@angular/common';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { Observable, catchError, combineLatest, filter, map, of, switchMap, take } from 'rxjs';
import {
  LucideAppWindow, LucideArrowDownRight, LucideArrowRight, LucideArrowUpRight, LucideBan, LucideBoxes, LucideClockAlert, LucideDynamicIcon,
  LucideExternalLink, LucideFlag, LucideIcon, LucideIdCard, LucideKeyRound, LucideLock, LucideLogIn, LucideMinus, LucideRefreshCw,
  LucideShieldAlert, LucideShieldCheck, LucideUserX, LucideUsers
} from '@lucide/angular';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
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
  { value: SeriesTimeFrame.Last7Days, label: 'Dashboard.Activity.Last7Days' },
  { value: SeriesTimeFrame.Last30Days, label: 'Dashboard.Activity.Last30Days' },
  { value: SeriesTimeFrame.Last90Days, label: 'Dashboard.Activity.Last90Days' }
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
    LucideDynamicIcon, TranslatePipe
  ],
  template: `
    <app-page-header [title]="greeting()" [subtitle]="'Dashboard.Subtitle' | translate">
      <div actions class="flex items-center gap-3">
        @if (metrics()?.calculatedAt; as calculatedAt) {
          <span class="text-xs text-base-content/60">{{ 'Dashboard.UpdatedAt' | translate: { time: (calculatedAt | date: 'shortTime') } }}</span>
        }
        <button type="button" class="btn btn-ghost btn-sm btn-square" (click)="reload()" [attr.aria-label]="'Common.Refresh' | translate" [disabled]="loading()">
          <svg lucideRefreshCw class="size-4" [class.animate-spin]="loading()"></svg>
        </button>
      </div>
    </app-page-header>

    <!-- headline figures -->
    <section class="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
      @if (users(); as users) {
        <app-stat-card [label]="'Routes.Users' | translate" [value]="users.total | number" [icon]="icons.users" tone="primary" link="/users" [loading]="loading()">
          <span hint>
            <span class="font-semibold text-success">+{{ users.newLast7Days | number }}</span> {{ 'Dashboard.Stats.NewUsersHint' | translate }}
          </span>
        </app-stat-card>
        <app-stat-card [label]="'Dashboard.Stats.SignedInToday' | translate" [value]="users.activeToday | number" [icon]="icons.activity" tone="secondary" [loading]="loading()">
          <span hint class="inline-flex items-center gap-1">
            @if (todayTrend() > 0) {
              <svg lucideArrowUpRight class="size-3.5 text-success"></svg><span class="font-semibold text-success">{{ todayTrend() | number }}</span>
            } @else if (todayTrend() < 0) {
              <svg lucideArrowDownRight class="size-3.5 text-error"></svg><span class="font-semibold text-error">{{ -todayTrend() | number }}</span>
            } @else {
              <svg lucideMinus class="size-3.5"></svg><span class="font-semibold">0</span>
            }
            {{ 'Dashboard.Stats.VsYesterday' | translate }}
          </span>
        </app-stat-card>
      }
      @if (clients(); as clients) {
        <app-stat-card [label]="'Dashboard.Stats.ClientApplications' | translate" [value]="clients.total | number" [icon]="icons.clients" tone="accent" link="/clients" [loading]="loading()">
          <span hint>{{ 'Dashboard.Stats.ClientsHint' | translate: { enabled: (clients.enabled | number), disabled: (clients.total - clients.enabled | number) } }}</span>
        </app-stat-card>
      }
      @if (canSeeLogs()) {
        <app-stat-card [label]="'Dashboard.Stats.FailedSignIns' | translate" [value]="failedSignIns() | number" [icon]="icons.failed" tone="error"
          link="/sign-in-logs" [queryParams]="failedQuery" [loading]="failedSignIns() === undefined">
          <span hint>{{ 'Dashboard.Stats.FailedSignInsHint' | translate }}</span>
        </app-stat-card>
      } @else if (users(); as users) {
        <app-stat-card [label]="'Routes.Roles' | translate" [value]="users.roles | number" [icon]="icons.roles" tone="info" link="/roles" [loading]="loading()">
          <span hint>{{ 'Dashboard.RolesHint' | translate }}</span>
        </app-stat-card>
      }
    </section>

    <div class="mt-6 grid gap-6 xl:grid-cols-3">
      <!-- sign in activity -->
      @if (canSeeLogs()) {
        <section class="panel p-5 xl:col-span-2">
          <header class="flex flex-wrap items-start justify-between gap-3">
            <div>
              <h2 class="font-semibold">{{ 'Dashboard.Activity.Title' | translate }}</h2>
              <p class="text-sm text-base-content/65">{{ 'Dashboard.Activity.Subtitle' | translate }}</p>
            </div>
            <div class="join">
              @for (option of timeFrames; track option.value) {
                <button type="button" class="join-item btn btn-xs" [class.btn-active]="timeFrame() === option.value"
                  (click)="timeFrame.set(option.value)">{{ option.label | translate }}</button>
              }
            </div>
          </header>
          <dl class="mt-4 grid grid-cols-2 gap-4 sm:grid-cols-4">
            <div>
              <dt class="text-xs text-base-content/60">{{ 'Dashboard.Activity.Attempts' | translate }}</dt>
              <dd class="text-xl font-semibold tabular">{{ seriesTotals().attempts | number }}</dd>
            </div>
            <div>
              <dt class="text-xs text-base-content/60">{{ 'Dashboard.Activity.FailureRate' | translate }}</dt>
              <dd class="text-xl font-semibold tabular" [class.text-error]="seriesTotals().failureRate > 0.1">{{ seriesTotals().failureRate | percent: '1.0-1' }}</dd>
            </div>
            <div>
              <dt class="text-xs text-base-content/60">{{ 'Dashboard.Activity.PeakDailyUsers' | translate }}</dt>
              <dd class="text-xl font-semibold tabular">{{ seriesTotals().peakUsers | number }}</dd>
            </div>
            <div>
              <dt class="flex items-center gap-1.5 text-xs text-base-content/60">{{ 'Dashboard.Activity.Legend' | translate }}</dt>
              <dd class="mt-1 flex flex-col gap-0.5 text-xs">
                <span class="inline-flex items-center gap-1.5"><span class="size-2 rounded-full" style="background: var(--app-chart-success)"></span>{{ 'Dashboard.Activity.Successful' | translate }}</span>
                <span class="inline-flex items-center gap-1.5"><span class="size-2 rounded-full" style="background: var(--app-chart-failure)"></span>{{ 'Dashboard.Activity.Failed' | translate }}</span>
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
          <h2 class="font-semibold">{{ 'Dashboard.Engagement.Title' | translate }}</h2>
          <p class="text-sm text-base-content/65">{{ 'Dashboard.Engagement.Subtitle' | translate }}</p>
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
            <span class="text-base-content/70">{{ 'Dashboard.Engagement.Stickiness' | translate }} <span class="text-xs">{{ 'Dashboard.Engagement.StickinessHint' | translate }}</span></span>
            <span class="font-semibold tabular">{{ stickiness() | percent: '1.0-1' }}</span>
          </div>
        </section>
      }
    </div>

    <div class="mt-6 grid gap-6 lg:grid-cols-2">
      <!-- security posture -->
      @if (users(); as users) {
        <section class="panel p-5">
          <h2 class="font-semibold">{{ 'Dashboard.Security.Title' | translate }}</h2>
          <p class="text-sm text-base-content/65">{{ 'Dashboard.Security.Subtitle' | translate }}</p>
          <div class="mt-5 flex flex-col gap-6 sm:flex-row sm:items-center">
            <div class="flex flex-col items-center gap-2">
              <div class="radial-progress text-primary" role="progressbar" [attr.aria-valuenow]="ratio(users.twoFactorEnabled) * 100"
                [style.--value]="ratio(users.twoFactorEnabled) * 100" style="--size: 7.5rem; --thickness: 0.6rem">
                <span class="text-center">
                  <span class="block text-2xl font-semibold text-base-content tabular">{{ ratio(users.twoFactorEnabled) | percent: '1.0-0' }}</span>
                  <span class="block text-[0.7rem] text-base-content/60">{{ 'Dashboard.Security.Mfa' | translate }}</span>
                </span>
              </div>
              <a class="link link-hover text-xs text-base-content/70" routerLink="/users" [queryParams]="{ twoFactorEnabled: 'false' }">
                {{ 'Dashboard.Security.WithoutMfa' | translate: { count: (users.total - users.twoFactorEnabled | number) } }}
              </a>
            </div>
            <ul class="flex-1 space-y-4">
              <li>
                <div class="mb-1.5 flex justify-between text-sm">
                  <a class="link link-hover" routerLink="/users" [queryParams]="{ emailConfirmed: 'false' }">{{ 'Dashboard.Security.VerifiedEmail' | translate }}</a>
                  <span class="tabular">{{ users.emailConfirmed | number }} <span class="text-xs text-base-content/60">{{ ratio(users.emailConfirmed) | percent: '1.0-0' }}</span></span>
                </div>
                <progress class="progress progress-success h-2 w-full" [value]="ratio(users.emailConfirmed) * 100" max="100"></progress>
              </li>
              <li>
                <div class="mb-1.5 flex justify-between text-sm">
                  <a class="link link-hover" routerLink="/users" [queryParams]="{ phoneNumberConfirmed: 'false' }">{{ 'Dashboard.Security.VerifiedPhone' | translate }}</a>
                  <span class="tabular">{{ users.phoneNumberConfirmed | number }} <span class="text-xs text-base-content/60">{{ ratio(users.phoneNumberConfirmed) | percent: '1.0-0' }}</span></span>
                </div>
                <progress class="progress progress-success h-2 w-full" [value]="ratio(users.phoneNumberConfirmed) * 100" max="100"></progress>
              </li>
              <li>
                <div class="mb-1.5 flex justify-between text-sm">
                  <span>{{ 'Dashboard.Security.SignedInOnce' | translate }}</span>
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
          <h2 class="font-semibold">{{ 'Dashboard.Attention.Title' | translate }}</h2>
          <p class="text-sm text-base-content/65">{{ 'Dashboard.Attention.Subtitle' | translate }}</p>
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
        <h2 class="mb-3 font-semibold">{{ 'Dashboard.Inventory.Title' | translate }}</h2>
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
        <h2 class="font-semibold">{{ 'Dashboard.Locations.Title' | translate }}</h2>
        <p class="mb-5 text-sm text-base-content/65">{{ 'Dashboard.Locations.Subtitle' | translate: { count: (locations()!.count | number) } }}</p>
        <app-sign-in-locations-map [locations]="locations()" />
      </section>
    }

    <!-- news -->
    @if (news().length > 0) {
      <section class="mt-6">
        <h2 class="mb-3 font-semibold">{{ 'Dashboard.News.Title' | translate }}</h2>
        <div class="grid gap-4 md:grid-cols-3">
          @for (post of news(); track post.link) {
            <a class="panel group flex flex-col gap-2 p-5 transition-colors hover:border-primary/40" [href]="post.link" target="_blank" rel="noopener">
              <span class="text-xs text-base-content/60">{{ post.publishDate | date: 'mediumDate' }}</span>
              <span class="font-semibold group-hover:text-primary">{{ post.title }}</span>
              <span class="line-clamp-3 text-sm text-base-content/70">{{ post.description }}</span>
              <span class="mt-auto inline-flex items-center gap-1 pt-1 text-xs font-medium text-primary">{{ 'Dashboard.News.ReadMore' | translate }} <svg lucideExternalLink class="size-3"></svg></span>
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
  private readonly _translate = inject(TranslateService);
  private readonly _date = new DatePipe(inject(LOCALE_ID));

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
    const part = this._translate.instant(hour < 12 ? 'Dashboard.Greeting.Morning' : hour < 18 ? 'Dashboard.Greeting.Afternoon' : 'Dashboard.Greeting.Evening');
    return name ? `${part}, ${name}` : part;
  });

  protected readonly todayTrend = computed(() => (this.users()?.activeToday ?? 0) - (this.users()?.activeYesterday ?? 0));
  protected readonly engagement = computed(() => {
    const users = this.users();
    return [
      { label: this._translate.instant('Dashboard.Engagement.Today'), count: users?.activeToday ?? 0, ratio: this.ratio(users?.activeToday) },
      { label: this._translate.instant('Dashboard.Engagement.Last7Days'), count: users?.activeLast7Days ?? 0, ratio: this.ratio(users?.activeLast7Days) },
      { label: this._translate.instant('Dashboard.Engagement.Last30Days'), count: users?.activeLast30Days ?? 0, ratio: this.ratio(users?.activeLast30Days) }
    ];
  });
  protected readonly stickiness = computed(() => {
    const users = this.users();
    return users?.activeLast30Days ? (users.activeToday ?? 0) / users.activeLast30Days : 0;
  });

  protected readonly seriesLabels = computed(() => (this.series() ?? []).map(point => this._date.transform(point.date, 'MMM d', 'UTC') ?? ''));
  protected readonly seriesData = computed<LineSeries[]>(() => {
    const series = this.series() ?? [];
    // the first series is drawn at the bottom of the stack: failures sit below the successful attempts
    return [
      { label: this._translate.instant('Dashboard.Activity.Failed'), data: series.map(point => point.failed), color: 'var(--app-chart-failure)' },
      { label: this._translate.instant('Dashboard.Activity.Successful'), data: series.map(point => point.succeeded), color: 'var(--app-chart-success)' }
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
        {
          label: this._translate.instant('Dashboard.Attention.LockedOut'), hint: this._translate.instant('Dashboard.Attention.LockedOutHint'),
          value: users.lockedOut, icon: LucideLock, tone: 'bg-warning/15 text-warning', link: '/users', queryParams: { lockedOut: 'true' }
        },
        {
          label: this._translate.instant('Dashboard.Attention.Blocked'), hint: this._translate.instant('Dashboard.Attention.BlockedHint'),
          value: users.blocked, icon: LucideBan, tone: 'bg-error/10 text-error', link: '/users', queryParams: { blocked: 'true' }
        },
        {
          label: this._translate.instant('Dashboard.Attention.ExpiredPasswords'), hint: this._translate.instant('Dashboard.Attention.ExpiredPasswordsHint'),
          value: users.passwordExpired, icon: LucideClockAlert, tone: 'bg-info/10 text-info'
        },
        {
          label: this._translate.instant('Dashboard.Attention.NeverSignedIn'), hint: this._translate.instant('Dashboard.Attention.NeverSignedInHint'),
          value: users.neverSignedIn, icon: LucideUserX, tone: 'bg-base-200 text-base-content/70'
        }
      );
    }
    if (this.canSeeLogs()) {
      items.push({
        label: this._translate.instant('Dashboard.Attention.Flagged'), hint: this._translate.instant('Dashboard.Attention.FlaggedHint'),
        value: this.flaggedSignIns(), icon: LucideFlag,
        tone: 'bg-accent/25 text-accent-content dark:text-accent', link: '/sign-in-logs', queryParams: { review: 'true' }
      });
    }
    return items;
  });

  protected readonly inventory = computed(() => {
    const users = this.users();
    const clients = this.clients();
    return [
      ...(users ? [{
        label: this._translate.instant('Routes.Roles'), value: users.roles, hint: this._translate.instant('Dashboard.RolesHint'),
        icon: LucideShieldCheck, link: '/roles'
      }] : []),
      ...(clients ? [
        {
          label: this._translate.instant('Routes.IdentityResources'), value: clients.identityResources, hint: this._translate.instant('Dashboard.Inventory.IdentityResourcesHint'),
          icon: LucideIdCard, link: '/resources/identity'
        },
        {
          label: this._translate.instant('Routes.ApiResources'), value: clients.apiResources, hint: this._translate.instant('Dashboard.Inventory.ApiResourcesHint'),
          icon: LucideBoxes, link: '/resources/api'
        },
        {
          label: this._translate.instant('Routes.ApiScopes'), value: clients.apiScopes, hint: this._translate.instant('Dashboard.Inventory.ApiScopesHint'),
          icon: LucideKeyRound, link: '/resources/scopes'
        }
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
