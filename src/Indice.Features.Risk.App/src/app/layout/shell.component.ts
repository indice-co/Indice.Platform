import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '@indice/ng-auth';
import { filter } from 'rxjs';
import {
  LucideActivity, LucideChevronsUpDown, LucideDynamicIcon, LucideGauge, LucideHeart, LucideIcon, LucideListChecks, LucideLogOut, LucideMenu,
  LucideMonitor, LucideMoon, LucideSun
} from '@lucide/angular';
import { settings } from '../core/models/settings';
import { LogoComponent } from '../ui/logo/logo.component';
import { ThemeMode, ThemeService } from './theme.service';

interface NavItem {
  label: string;
  hint: string;
  link: string;
  icon: LucideIcon;
}

/** Authenticated application chrome: sidebar navigation, top bar and the routed page. */
@Component({
  selector: 'app-shell',
  imports: [
    RouterOutlet, RouterLink, RouterLinkActive, LogoComponent, LucideDynamicIcon, LucideMenu, LucideSun, LucideMoon, LucideMonitor,
    LucideLogOut, LucideChevronsUpDown, LucideHeart
  ],
  styles: `
    .nav-link {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      border-radius: var(--radius-field);
      padding: 0.5rem 0.75rem;
      font-size: 0.875rem;
      font-weight: 500;
      color: var(--app-sidebar-content);
      transition: background-color 120ms, color 120ms;
    }
    .nav-link:hover { background: var(--app-sidebar-hover); color: var(--app-sidebar-content-strong); }
    .nav-link:focus-visible { outline: 2px solid var(--color-primary); outline-offset: 1px; }
    .nav-link.active { background: var(--app-sidebar-active-bg); color: var(--app-sidebar-active-content); font-weight: 600; }
  `,
  template: `
    <div class="drawer lg:drawer-open">
      <input id="app-drawer" type="checkbox" class="drawer-toggle" [checked]="drawerOpen()" (change)="drawerOpen.set($any($event.target).checked)" />

      <div class="drawer-content flex min-h-dvh min-w-0 flex-col">
        <header class="sticky top-0 z-30 flex h-14 items-center gap-2 border-b border-base-300 bg-base-100/85 px-3 backdrop-blur sm:px-4 lg:px-8">
          <label for="app-drawer" class="btn btn-ghost btn-sm btn-square lg:hidden" aria-label="Open menu">
            <svg lucideMenu class="size-5"></svg>
          </label>
          <app-logo class="lg:hidden" [size]="26" />
          <div class="flex-1"></div>

          <div class="dropdown dropdown-end">
            <button type="button" tabindex="0" class="btn btn-ghost btn-sm btn-square" aria-label="Theme">
              @if (theme.isDark()) { <svg lucideMoon class="size-4"></svg> } @else { <svg lucideSun class="size-4"></svg> }
            </button>
            <ul tabindex="0" class="dropdown-content menu z-40 mt-2 w-40 rounded-box border border-base-300 bg-base-100 p-1 shadow-lg">
              @for (option of themeOptions; track option.mode) {
                <li>
                  <button type="button" [class.menu-active]="theme.mode() === option.mode" (click)="theme.setMode(option.mode); blur()">
                    @switch (option.mode) {
                      @case ('light') { <svg lucideSun class="size-4"></svg> }
                      @case ('dark') { <svg lucideMoon class="size-4"></svg> }
                      @default { <svg lucideMonitor class="size-4"></svg> }
                    }
                    {{ option.label }}
                  </button>
                </li>
              }
            </ul>
          </div>
        </header>

        <main class="mx-auto w-full max-w-[96rem] flex-1 px-4 py-6 sm:px-6 lg:px-8 lg:py-8">
          <router-outlet />
        </main>
      </div>

      <div class="drawer-side z-40">
        <label for="app-drawer" class="drawer-overlay" aria-label="Close menu"></label>
        <aside class="flex min-h-full flex-col border-r"
          style="width: var(--app-sidebar-width); background: var(--app-sidebar-bg); color: var(--app-sidebar-content); border-color: var(--app-sidebar-border)">
          <a routerLink="/risk-events" class="flex h-16 items-center px-5" style="color: var(--app-sidebar-content-strong)">
            <app-logo [size]="30" ring="var(--app-sidebar-bg)" />
          </a>

          <nav class="flex-1 overflow-y-auto px-3 py-4" aria-label="Main">
            <p class="mb-1.5 px-3 text-[0.7rem] font-semibold tracking-wider uppercase opacity-60">Monitoring</p>
            <ul class="space-y-0.5">
              @for (item of nav; track item.link) {
                <li>
                  <a class="nav-link" [routerLink]="item.link" routerLinkActive="active" [title]="item.hint"
                    [routerLinkActiveOptions]="{ paths: 'subset', queryParams: 'ignored', fragment: 'ignored', matrixParams: 'ignored' }">
                    <svg [lucideIcon]="item.icon" class="size-[1.125rem] shrink-0"></svg>
                    <span class="truncate">{{ item.label }}</span>
                  </a>
                </li>
              }
            </ul>
          </nav>

          <div class="border-t px-3 py-3" style="border-color: var(--app-sidebar-border)">
            <div class="dropdown dropdown-top w-full">
              <button type="button" tabindex="0" class="nav-link w-full text-left">
                <span class="grid size-8 shrink-0 place-items-center rounded-full bg-primary text-xs font-semibold text-primary-content">{{ initials() }}</span>
                <span class="min-w-0 flex-1">
                  <span class="block truncate font-semibold" style="color: var(--app-sidebar-content-strong)">{{ userName() }}</span>
                  <span class="block truncate text-xs opacity-70">{{ userEmail() }}</span>
                </span>
                <svg lucideChevronsUpDown class="size-4 shrink-0 opacity-60"></svg>
              </button>
              <ul tabindex="0" class="dropdown-content menu z-50 mb-2 w-full rounded-box border border-base-300 bg-base-100 p-1 text-base-content shadow-lg">
                <li>
                  <a routerLink="/logout"><svg lucideLogOut class="size-4"></svg>Sign out</a>
                </li>
              </ul>
            </div>
            <p class="mt-3 flex items-center gap-1 px-3 text-xs opacity-60">
              Powered by <a class="link link-hover" href="https://www.indice.gr" target="_blank" rel="noopener">Indice</a>
              <svg lucideHeart class="size-3 fill-current text-accent"></svg>
              <span class="tabular ml-auto">v{{ version }}</span>
            </p>
          </div>
        </aside>
      </div>
    </div>
  `
})
export class ShellComponent {
  private readonly _auth = inject(AuthService);
  protected readonly theme = inject(ThemeService);
  protected readonly version = settings.version;
  protected readonly drawerOpen = signal(false);
  protected readonly nav: NavItem[] = [
    { label: 'Risk events', hint: 'Transactions and actions reported for evaluation', link: '/risk-events', icon: LucideActivity },
    { label: 'Risk results', hint: 'Outcomes of rule evaluations', link: '/risk-results', icon: LucideGauge },
    { label: 'Rules', hint: 'Registered rules and their options', link: '/rules', icon: LucideListChecks }
  ];
  protected readonly themeOptions: { mode: ThemeMode; label: string }[] = [
    { mode: 'light', label: 'Light' },
    { mode: 'dark', label: 'Dark' },
    { mode: 'system', label: 'System' }
  ];

  private readonly _user = toSignal(this._auth.user$, { initialValue: this._auth.getCurrentUser() });
  protected readonly userName = computed(() => {
    const profile = this._user()?.profile;
    return profile?.given_name ? `${profile.given_name} ${profile.family_name ?? ''}`.trim() : profile?.name ?? profile?.email ?? '';
  });
  protected readonly userEmail = computed(() => this._user()?.profile?.email ?? '');
  protected readonly initials = computed(() =>
    this.userName().split(/\s+/).filter(Boolean).slice(0, 2).map(part => part[0].toUpperCase()).join('') || '?');

  constructor() {
    // close the mobile drawer after navigating
    inject(Router).events
      .pipe(filter(e => e instanceof NavigationEnd), takeUntilDestroyed())
      .subscribe(() => this.drawerOpen.set(false));
  }

  /** Closes a focus-driven daisyUI dropdown. */
  protected blur(): void {
    (document.activeElement as HTMLElement | null)?.blur();
  }
}
