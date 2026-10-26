import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  afterRenderEffect,
  computed,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { DatePipe, NgTemplateOutlet } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';

import { appName } from '../models/brand';
import { LiveRefreshService, REFRESH_INTERVALS } from '../services/live-refresh.service';
import { StatsStore } from '../services/stats.store';
import { ToastHostComponent } from '../../shared/toast-host.component';
import { AppSidebarComponent, NavBadges } from './app-sidebar.component';

/** Where the desktop rail remembers whether it was left collapsed. */
const COLLAPSED_KEY = 'workers.rail.collapsed';

/**
 * App shell: a navigation rail on the left over a routed page, with the Hangfire-style live control
 * (pause / interval / refresh now) in the top bar. The rail collapses to an icon strip from `md` up,
 * and slides in as a drawer below it.
 *
 * The shell starts the app-wide `/stats` poll, so the nav badges stay current on every page.
 */
@Component({
  selector: 'app-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    NgTemplateOutlet,
    RouterLink,
    RouterOutlet,
    AppSidebarComponent,
    ToastHostComponent,
  ],
  host: {
    '(document:keydown.escape)': 'onEscape()',
  },
  template: `
    <div class="flex h-screen bg-base-200">
      <!-- Desktop rail. Inert behind an open drawer, so "modal" is true and Tab can't reach it. -->
      <div
        class="hidden shrink-0 border-r border-base-300 transition-[width] duration-200 md:block"
        [class.w-16]="collapsed()"
        [class.w-64]="!collapsed()"
        [attr.inert]="drawerOpen() ? '' : null"
      >
        <app-sidebar
          [collapsed]="collapsed()"
          [badges]="badges()"
          (toggle)="toggleCollapsed()"
        />
      </div>

      <div class="flex min-w-0 flex-1 flex-col" [attr.inert]="drawerOpen() ? '' : null">
        <!-- Mobile bar: the rail is off-canvas below md, so the burger is the only way in. -->
        <header
          class="flex h-14 shrink-0 items-center gap-3 border-b border-base-300 bg-base-100 px-3
                 md:hidden"
        >
          <button
            #burger
            type="button"
            class="btn btn-ghost btn-sm btn-circle text-base-content/70"
            (click)="drawerOpen.set(true)"
            aria-label="Open navigation"
            [attr.aria-expanded]="drawerOpen()"
          >
            <svg viewBox="0 0 24 24" fill="none" class="size-5" aria-hidden="true">
              <path
                d="M4 7h16M4 12h16M4 17h16"
                stroke="currentColor"
                stroke-width="2"
                stroke-linecap="round"
              />
            </svg>
          </button>

          <a routerLink="/" class="flex min-w-0 items-center gap-2">
            <svg viewBox="0 0 32 32" class="size-7 shrink-0 text-primary" aria-hidden="true">
              <rect width="32" height="32" rx="9" fill="currentColor" />
              <path
                d="M9 11h14M9 16h10M9 21h6"
                stroke="white"
                stroke-width="2.5"
                stroke-linecap="round"
              />
            </svg>
            <span class="text-base font-semibold tracking-tight text-base-content">{{ appName }}</span>
          </a>

          <div class="ml-auto">
            <ng-container *ngTemplateOutlet="liveControl" />
          </div>
        </header>

        <!-- Desktop top bar: live control + freshness. -->
        <div
          class="hidden h-12 shrink-0 items-center justify-end gap-4 border-b border-base-300
                 bg-base-100 px-6 md:flex"
        >
          <span class="font-mono text-xs text-base-content/50">
            @if (agoSeconds() !== null) {
              Updated {{ agoSeconds() }} s ago
            } @else {
              Not updated yet
            }
            @if (serverTime(); as time) {
              · Server {{ time | date: 'HH:mm:ss' : 'UTC' }} UTC
            }
          </span>
          <ng-container *ngTemplateOutlet="liveControl" />
        </div>

        <main class="app-scroll min-h-0 flex-1 overflow-y-auto">
          <router-outlet />
        </main>
      </div>

      <!-- Mobile drawer -->
      @if (drawerOpen()) {
        <div
          class="fixed inset-0 z-40 bg-black/40 md:hidden"
          (click)="closeDrawer()"
          aria-hidden="true"
        ></div>
      }
      <!--
        aria-modal is claimed only while the drawer is open — and only then is it true, because the
        rail and main column above are inert. Closed, it is inert rather than aria-hidden:
        aria-hidden over focusable content is a WCAG anti-pattern, and inert covers both trees.
      -->
      <div
        #drawer
        class="fixed inset-y-0 left-0 z-50 w-64 max-w-[85vw] border-r border-base-300 shadow-xl
               transition-transform duration-200 md:hidden"
        [class.-translate-x-full]="!drawerOpen()"
        [attr.inert]="drawerOpen() ? null : ''"
        [attr.aria-modal]="drawerOpen() ? 'true' : null"
        tabindex="-1"
        role="dialog"
        aria-label="Navigation"
      >
        <app-sidebar [collapsed]="false" [badges]="badges()" (navigated)="closeDrawer()" />
      </div>
    </div>

    <app-toast-host />

    <!-- Live control (Hangfire's auto-refresh): rendered in the mobile bar and the desktop top bar. -->
    <ng-template #liveControl>
      <div class="join">
        <button
          type="button"
          class="btn btn-sm join-item gap-2"
          (click)="live.setEnabled(!live.enabled())"
          [attr.aria-pressed]="live.enabled()"
          [title]="live.enabled() ? 'Pause auto-refresh' : 'Resume auto-refresh'"
        >
          @if (live.enabled()) {
            <span class="status status-success animate-pulse" aria-hidden="true"></span>
            Live
          } @else {
            <span aria-hidden="true">❚❚</span>
            Paused
          }
        </button>
        <select
          class="select select-sm join-item w-auto"
          aria-label="Refresh interval"
          [value]="live.intervalMs()"
          (change)="onInterval($event)"
        >
          @for (ms of intervals; track ms) {
            <option [value]="ms" [selected]="ms === live.intervalMs()">{{ ms / 1000 }} s</option>
          }
        </select>
        <button
          type="button"
          class="btn btn-sm join-item"
          (click)="live.refreshNow()"
          aria-label="Refresh now"
          title="Refresh now"
        >
          <svg viewBox="0 0 24 24" fill="none" class="size-4" aria-hidden="true">
            <path
              d="M20 11a8 8 0 1 0-2.3 5.7M20 5v6h-6"
              stroke="currentColor"
              stroke-width="2"
              stroke-linecap="round"
              stroke-linejoin="round"
            />
          </svg>
        </button>
      </div>
    </ng-template>
  `,
})
export class ShellComponent {
  protected readonly appName = appName;
  protected readonly intervals = REFRESH_INTERVALS;

  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly live = inject(LiveRefreshService);
  private readonly statsStore = inject(StatsStore);

  /** Desktop icon-rail state, remembered across reloads. */
  protected readonly collapsed = signal(readCollapsed());
  /** Whether the mobile off-canvas rail is showing. */
  protected readonly drawerOpen = signal(false);

  protected readonly badges = computed<NavBadges | null>(() => {
    const stats = this.statsStore.stats();
    return stats ? { poison: stats.poison, failingTasks: stats.failingTasks } : null;
  });
  protected readonly serverTime = computed(() => this.statsStore.stats()?.serverTimeUtc ?? null);

  /** Ticks every second so "Updated n s ago" counts up between polls. */
  private readonly now = signal(Date.now());
  protected readonly agoSeconds = computed(() => {
    const last = this.live.lastUpdated();
    return last ? Math.max(0, Math.floor((this.now() - last.getTime()) / 1000)) : null;
  });

  private readonly drawer = viewChild.required<ElementRef<HTMLElement>>('drawer');
  private readonly burger = viewChild.required<ElementRef<HTMLButtonElement>>('burger');
  /** Guards the restore so the first render doesn't yank focus to the burger. */
  private drawerWasOpen = false;

  constructor() {
    // afterRender, not effect: the `inert` attribute must be off the drawer before we focus into
    // it — focusing inside a still-inert subtree is a no-op.
    afterRenderEffect(() => {
      const open = this.drawerOpen();
      if (open) {
        this.drawer().nativeElement.focus();
      } else if (this.drawerWasOpen) {
        this.burger().nativeElement.focus();
      }
      this.drawerWasOpen = open;
    });

    this.statsStore.start(this.destroyRef);

    const clock = setInterval(() => this.now.set(Date.now()), 1000);
    this.destroyRef.onDestroy(() => clearInterval(clock));

    // A back gesture must not leave the drawer covering the page it navigated to.
    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => this.drawerOpen.set(false));

    const desktopBreakpoint = window.matchMedia('(min-width: 48rem)');
    const closeDrawerOnDesktop = (event: MediaQueryListEvent) => {
      if (event.matches) {
        this.closeDrawer();
      }
    };
    desktopBreakpoint.addEventListener('change', closeDrawerOnDesktop);
    this.destroyRef.onDestroy(() =>
      desktopBreakpoint.removeEventListener('change', closeDrawerOnDesktop),
    );
  }

  protected toggleCollapsed(): void {
    const next = !this.collapsed();
    this.collapsed.set(next);
    try {
      localStorage.setItem(COLLAPSED_KEY, String(next));
    } catch {
      // Private browsing can refuse storage — the preference just won't survive the reload.
    }
  }

  protected closeDrawer(): void {
    this.drawerOpen.set(false);
  }

  protected onInterval(event: Event): void {
    this.live.setInterval(Number((event.target as HTMLSelectElement).value));
  }

  /** Escape closes the drawer — unless a modal (a confirmation) is claiming the key. */
  protected onEscape(): void {
    if (!document.querySelector('dialog[open]')) {
      this.closeDrawer();
    }
  }
}

/** Read the remembered rail state; defaults to expanded when storage is unavailable or unset. */
function readCollapsed(): boolean {
  try {
    return localStorage.getItem(COLLAPSED_KEY) === 'true';
  } catch {
    return false;
  }
}
