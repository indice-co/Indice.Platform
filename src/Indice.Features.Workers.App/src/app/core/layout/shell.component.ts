import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { LucideMenu, LucideMonitor, LucideMoon, LucidePause, LucideRefreshCw, LucideSun } from '@lucide/angular';
import { filter } from 'rxjs';

import { LiveRefreshService, REFRESH_INTERVALS, formatInterval } from '../services/live-refresh.service';
import { PermissionsStore } from '../services/permissions.store';
import { StatsStore } from '../services/stats.store';
import { ToastHostComponent } from '../../shared/toast-host.component';
import { AppSidebarComponent, NavBadges } from './app-sidebar.component';
import { ThemeMode, ThemeService } from './theme.service';

/**
 * Authenticated application chrome: the navigation rail on the left (pinned from `lg` up, a drawer below it), a top bar
 * with data freshness, the Hangfire-style live control (pause / interval / refresh now) and the theme switcher, and the
 * routed page.
 *
 * The shell starts the app-wide `/stats` poll, so the nav badges stay current on every page.
 */
@Component({
  selector: 'app-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    RouterOutlet,
    AppSidebarComponent,
    ToastHostComponent,
    LucideMenu,
    LucidePause,
    LucideRefreshCw,
    LucideSun,
    LucideMoon,
    LucideMonitor,
  ],
  host: {
    '(document:keydown.escape)': 'onEscape()',
  },
  template: `
    <div class="drawer lg:drawer-open">
      <input
        id="app-drawer"
        type="checkbox"
        class="drawer-toggle"
        [checked]="drawerOpen()"
        (change)="drawerOpen.set($any($event.target).checked)"
      />

      <div class="drawer-content flex min-h-dvh min-w-0 flex-col">
        <header
          class="sticky top-0 z-30 flex h-14 items-center gap-2 border-b border-base-300 bg-base-100/85 px-3
                 backdrop-blur sm:px-4 lg:px-8"
        >
          <button
            type="button"
            class="btn btn-ghost btn-sm btn-square lg:hidden"
            [attr.aria-label]="drawerOpen() ? 'Close navigation' : 'Open navigation'"
            [attr.aria-expanded]="drawerOpen()"
            aria-controls="app-drawer"
            (click)="drawerOpen.set(!drawerOpen())"
          >
            <svg lucideMenu class="size-5"></svg>
          </button>

          <p class="min-w-0 flex-1 truncate font-mono text-xs text-base-content/70">
            <span class="hidden sm:inline">
              @if (agoSeconds() !== null) {
                Updated {{ agoSeconds() }} s ago
              } @else {
                Not updated yet
              }
              @if (serverTime(); as time) {
                · Server {{ time | date: 'HH:mm:ss' : 'UTC' }} UTC
              }
            </span>
          </p>

          <!-- Live control (Hangfire's auto-refresh). -->
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
                <svg lucidePause class="size-3.5" aria-hidden="true"></svg>
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
                <option [value]="ms" [selected]="ms === live.intervalMs()">{{ label(ms) }}</option>
              }
            </select>
            <button
              type="button"
              class="btn btn-sm btn-square join-item"
              (click)="live.refreshNow()"
              aria-label="Refresh now"
              title="Refresh now"
            >
              <svg lucideRefreshCw class="size-4" aria-hidden="true"></svg>
            </button>
          </div>

          @if (theme.switchable) {
            <div class="dropdown dropdown-end">
              <button type="button" tabindex="0" class="btn btn-ghost btn-sm btn-square" aria-label="Theme">
                @if (theme.isDark()) {
                  <svg lucideMoon class="size-4"></svg>
                } @else {
                  <svg lucideSun class="size-4"></svg>
                }
              </button>
              <ul
                tabindex="0"
                class="dropdown-content menu z-40 mt-2 w-40 rounded-box border border-base-300 bg-base-100 p-1 shadow-lg"
              >
                @for (option of themeOptions; track option.mode) {
                  <li>
                    <button
                      type="button"
                      [class.menu-active]="theme.mode() === option.mode"
                      (click)="theme.setMode(option.mode); blur()"
                    >
                      @switch (option.mode) {
                        @case ('light') {
                          <svg lucideSun class="size-4"></svg>
                        }
                        @case ('dark') {
                          <svg lucideMoon class="size-4"></svg>
                        }
                        @default {
                          <svg lucideMonitor class="size-4"></svg>
                        }
                      }
                      {{ option.label }}
                    </button>
                  </li>
                }
              </ul>
            </div>
          }
        </header>

        <!-- Pages pad themselves; the shell caps the width and adds the extra gutter of wide screens. -->
        <main class="mx-auto w-full max-w-[96rem] flex-1 lg:p-2">
          <router-outlet />
        </main>
      </div>

      <div class="drawer-side z-40">
        <label for="app-drawer" class="drawer-overlay" aria-label="Close navigation"></label>
        <app-sidebar [badges]="badges()" (navigated)="closeDrawer()" />
      </div>
    </div>

    <app-toast-host />
  `,
})
export class ShellComponent {
  protected readonly intervals = REFRESH_INTERVALS;
  protected readonly label = formatInterval;

  private readonly destroyRef = inject(DestroyRef);

  protected readonly live = inject(LiveRefreshService);
  protected readonly theme = inject(ThemeService);
  private readonly statsStore = inject(StatsStore);

  /** Whether the off-canvas rail is showing (below `lg`). */
  protected readonly drawerOpen = signal(false);
  protected readonly themeOptions: { mode: ThemeMode; label: string }[] = [
    { mode: 'light', label: 'Light' },
    { mode: 'dark', label: 'Dark' },
    { mode: 'system', label: 'System' },
  ];

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

  constructor() {
    this.statsStore.start(this.destroyRef);
    inject(PermissionsStore).load();

    const clock = setInterval(() => this.now.set(Date.now()), 1000);
    this.destroyRef.onDestroy(() => clearInterval(clock));

    // A back gesture must not leave the drawer covering the page it navigated to.
    inject(Router)
      .events.pipe(
        filter((event) => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => this.closeDrawer());
  }

  protected closeDrawer(): void {
    this.drawerOpen.set(false);
  }

  protected onInterval(event: Event): void {
    this.live.setInterval(Number((event.target as HTMLSelectElement).value));
  }

  /** Closes a focus-driven daisyUI dropdown. */
  protected blur(): void {
    (document.activeElement as HTMLElement | null)?.blur();
  }

  /** Escape closes the drawer — unless a modal (a confirmation) is claiming the key. */
  protected onEscape(): void {
    if (!document.querySelector('dialog[open]')) {
      this.closeDrawer();
    }
  }
}
