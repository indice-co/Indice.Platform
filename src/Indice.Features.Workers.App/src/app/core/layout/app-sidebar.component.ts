import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { LucideCalendarClock, LucideLayers, LucideLayoutDashboard, LucideServer } from '@lucide/angular';

import { LogoComponent } from '../../shared/logo.component';
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
  link: string;
  exact: boolean;
  icon: 'dashboard' | 'queues' | 'scheduled' | 'servers';
  badge?: keyof NavBadges;
}

interface NavSection {
  /** Section heading; omitted for the first section. */
  label?: string;
  items: NavItem[];
}

const NAV: NavSection[] = [
  {
    items: [{ label: 'Dashboard', link: '/', exact: true, icon: 'dashboard' }],
  },
  {
    label: 'Background work',
    items: [
      { label: 'Queues', link: '/queues', exact: false, icon: 'queues', badge: 'poison' },
      {
        label: 'Scheduled tasks',
        link: '/scheduled',
        exact: false,
        icon: 'scheduled',
        badge: 'failingTasks',
      },
    ],
  },
  {
    label: 'Infrastructure',
    items: [{ label: 'Servers & locks', link: '/servers', exact: false, icon: 'servers' }],
  },
];

/**
 * The application rail: brand, the page navigation and the account anchor at the bottom. It is pinned from `lg` up and
 * slides in as the shell's drawer below it. Presentational — the shell owns the data.
 */
@Component({
  selector: 'app-sidebar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    RouterLinkActive,
    LucideLayoutDashboard,
    LucideLayers,
    LucideCalendarClock,
    LucideServer,
    LogoComponent,
    PoweredByComponent,
    SidebarAccountComponent,
  ],
  host: { class: 'flex min-h-full' },
  template: `
    <aside
      class="flex min-h-full flex-col border-r"
      style="width: var(--app-sidebar-width); background: var(--app-sidebar-bg); color: var(--app-sidebar-content); border-color: var(--app-sidebar-border)"
    >
      <a
        routerLink="/"
        class="flex h-16 shrink-0 items-center px-5"
        style="color: var(--app-sidebar-content-strong)"
        [title]="appName"
        (click)="navigated.emit()"
      >
        <app-logo [size]="30" ring="var(--app-sidebar-bg)" />
      </a>

      <nav class="app-scroll flex-1 space-y-6 overflow-y-auto px-3 py-4" aria-label="Main">
        @for (section of nav; track $index) {
          <div>
            @if (section.label) {
              <p class="mb-1.5 px-3 text-[0.7rem] font-semibold tracking-wider uppercase opacity-55">
                {{ section.label }}
              </p>
            }
            <ul class="space-y-0.5">
              @for (item of section.items; track item.link) {
                <li>
                  <a
                    class="nav-link"
                    [routerLink]="item.link"
                    routerLinkActive="active"
                    [routerLinkActiveOptions]="{ exact: item.exact }"
                    (click)="navigated.emit()"
                  >
                    @switch (item.icon) {
                      @case ('dashboard') {
                        <svg lucideLayoutDashboard class="size-[1.125rem] shrink-0"></svg>
                      }
                      @case ('queues') {
                        <svg lucideLayers class="size-[1.125rem] shrink-0"></svg>
                      }
                      @case ('scheduled') {
                        <svg lucideCalendarClock class="size-[1.125rem] shrink-0"></svg>
                      }
                      @case ('servers') {
                        <svg lucideServer class="size-[1.125rem] shrink-0"></svg>
                      }
                    }
                    <span class="flex-1 truncate">{{ item.label }}</span>
                    @if (countOf(item); as count) {
                      <span class="badge badge-error badge-sm tabular-nums">
                        {{ count }}<span class="sr-only"> need attention</span>
                      </span>
                    }
                  </a>
                </li>
              }
            </ul>
          </div>
        }
      </nav>

      <div class="shrink-0 border-t px-3 py-3" style="border-color: var(--app-sidebar-border)">
        <app-sidebar-account (navigated)="navigated.emit()" />
        <app-powered-by class="mt-3 block" />
      </div>
    </aside>
  `,
})
export class AppSidebarComponent {
  protected readonly appName = appName;
  protected readonly nav = NAV;

  /** Attention counts per nav item; `null` until the first `/stats` response. */
  readonly badges = input<NavBadges | null>(null);

  /** Something navigational happened — the shell closes the mobile drawer on it. */
  readonly navigated = output<void>();

  /** The item's badge count, or 0 (falsy, so nothing renders) when it has none. */
  protected countOf(item: NavItem): number {
    const badges = this.badges();
    return item.badge && badges ? badges[item.badge] : 0;
  }
}
