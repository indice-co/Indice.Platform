import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

import { appName } from '../models/brand';
import { PoweredByComponent } from './powered-by.component';
import { SidebarAccountComponent } from './sidebar-account.component';

/** Attention counts the shell feeds into the nav (from `StatsStore`). */
export interface NavBadges {
  /** Poison messages across all queues. */
  poison: number;
  /** Scheduled tasks whose last run failed. */
  failingTasks: number;
}

interface NavItem {
  label: string;
  route: string;
  exact: boolean;
  badge?: keyof NavBadges;
}

const NAV: NavItem[] = [
  { label: 'Dashboard', route: '/', exact: true },
  { label: 'Queues', route: '/queues', exact: false, badge: 'poison' },
  { label: 'Scheduled tasks', route: '/scheduled', exact: false, badge: 'failingTasks' },
  { label: 'Servers & locks', route: '/servers', exact: false },
];

/**
 * The application rail: brand, the page navigation and the account anchor at the bottom.
 * Presentational — the shell owns the data.
 *
 * Two shapes share one component: `collapsed` renders the desktop icon strip, and the same
 * expanded markup serves both the wide desktop rail and the mobile drawer.
 */
@Component({
  selector: 'app-sidebar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, RouterLinkActive, PoweredByComponent, SidebarAccountComponent],
  template: `
    <aside class="flex h-full w-full flex-col overflow-hidden bg-base-100">
      <!-- Brand + collapse control -->
      <div class="flex shrink-0 flex-col gap-1 px-3 pt-3">
        <div class="flex items-center gap-2.5" [class.justify-center]="collapsed()">
          <a routerLink="/" class="flex min-w-0 items-center gap-2.5" (click)="navigated.emit()">
            <svg viewBox="0 0 32 32" class="size-8 shrink-0 text-primary" aria-hidden="true">
              <rect width="32" height="32" rx="9" fill="currentColor" />
              <path
                d="M9 11h14M9 16h10M9 21h6"
                stroke="white"
                stroke-width="2.5"
                stroke-linecap="round"
              />
            </svg>
            @if (!collapsed()) {
              <span class="min-w-0 leading-tight">
                <span class="block text-lg font-semibold tracking-tight text-base-content">
                  {{ appName }}
                </span>
                <span
                  class="-mt-0.5 block truncate font-mono text-[0.6rem] uppercase
                         tracking-[0.22em] text-base-content/45"
                >
                  background jobs
                </span>
              </span>
            } @else {
              <span class="sr-only">{{ appName }}</span>
            }
          </a>
          @if (!collapsed()) {
            <button
              type="button"
              class="btn btn-ghost btn-sm btn-circle ml-auto hidden text-base-content/50
                     md:inline-flex"
              (click)="toggle.emit()"
              aria-label="Collapse sidebar"
              aria-expanded="true"
              title="Collapse sidebar"
            >
              <svg viewBox="0 0 24 24" fill="none" class="size-5" aria-hidden="true">
                <path
                  d="M15 6l-6 6 6 6"
                  stroke="currentColor"
                  stroke-width="2"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                />
              </svg>
            </button>
          }
        </div>
        @if (collapsed()) {
          <button
            type="button"
            class="btn btn-ghost btn-sm btn-circle mx-auto text-base-content/50"
            (click)="toggle.emit()"
            aria-label="Expand sidebar"
            aria-expanded="false"
            title="Expand sidebar"
          >
            <svg viewBox="0 0 24 24" fill="none" class="size-5" aria-hidden="true">
              <path
                d="M9 6l6 6-6 6"
                stroke="currentColor"
                stroke-width="2"
                stroke-linecap="round"
                stroke-linejoin="round"
              />
            </svg>
          </button>
        }
      </div>

      <!-- Navigation -->
      <nav class="app-scroll flex-1 overflow-x-hidden overflow-y-auto pt-3 pb-3" aria-label="Main">
        <ul class="menu w-full" [class.items-center]="collapsed()" [class.px-1]="collapsed()">
          @for (item of nav; track item.route) {
            <li
              [class.tooltip]="collapsed()"
              [class.tooltip-right]="collapsed()"
              [attr.data-tip]="collapsed() ? item.label : null"
            >
              <a
                [routerLink]="item.route"
                routerLinkActive="menu-active"
                [routerLinkActiveOptions]="{ exact: item.exact }"
                [class.justify-center]="collapsed()"
                [class.relative]="collapsed()"
                [attr.aria-label]="collapsed() ? item.label : null"
                (click)="navigated.emit()"
              >
                <svg
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  stroke-width="1.8"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  class="size-5 shrink-0"
                  aria-hidden="true"
                >
                  @switch (item.route) {
                    @case ('/') {
                      <path
                        d="M4 5a1 1 0 0 1 1-1h5v7H4zM14 4h5a1 1 0 0 1 1 1v4h-6zM14 13h6v6a1 1 0 0 1-1 1h-5zM4 15h6v5H5a1 1 0 0 1-1-1z"
                      />
                    }
                    @case ('/queues') {
                      <path d="M4 6h16M4 12h16M4 18h10" />
                    }
                    @case ('/scheduled') {
                      <circle cx="12" cy="12" r="8" />
                      <path d="M12 8v4l3 2" />
                    }
                    @case ('/servers') {
                      <path d="M4 5h16v5H4zM4 14h16v5H4zM8 7.5h.01M8 16.5h.01" />
                    }
                  }
                </svg>
                @if (!collapsed()) {
                  <span class="flex-1">{{ item.label }}</span>
                }
                @if (countOf(item); as count) {
                  @if (collapsed()) {
                    <span
                      class="status status-error absolute right-1.5 top-1.5"
                      [attr.aria-label]="count + ' need attention'"
                    ></span>
                  } @else {
                    <span class="badge badge-error badge-sm">{{ count }}</span>
                  }
                }
              </a>
            </li>
          }
        </ul>
      </nav>

      <!-- Account anchor -->
      <div class="shrink-0 border-t border-base-300 px-2 py-2">
        @if (!collapsed()) {
          <div class="pb-1.5">
            <app-powered-by />
          </div>
        }
        <app-sidebar-account [collapsed]="collapsed()" (navigated)="navigated.emit()" />
      </div>
    </aside>
  `,
})
export class AppSidebarComponent {
  protected readonly appName = appName;
  protected readonly nav = NAV;

  /** Desktop icon-rail mode; always `false` inside the mobile drawer. */
  readonly collapsed = input(false);
  /** Attention counts per nav item; `null` until the first `/stats` response. */
  readonly badges = input<NavBadges | null>(null);

  /** The collapse/expand control was used. */
  readonly toggle = output<void>();
  /** Something navigational happened — the shell closes the mobile drawer on it. */
  readonly navigated = output<void>();

  /** The item's badge count, or 0 (falsy, so nothing renders) when it has none. */
  protected countOf(item: NavItem): number {
    const badges = this.badges();
    return item.badge && badges ? badges[item.badge] : 0;
  }
}
