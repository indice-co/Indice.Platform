import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';
import {
  LucideBookOpen, LucideChevronsUpDown, LucideDynamicIcon, LucideExternalLink, LucideHeart, LucideLogOut, LucideMenu, LucideMonitor,
  LucideMoon, LucidePlus, LucideSun
} from '@lucide/angular';
import { settings } from '../core/models/settings';
import { AuthService } from '../core/services/auth.service';
import { ImgUserPictureDirective } from '../ui/avatar/user-picture.directive';
import { LogoComponent } from '../ui/logo/logo.component';
import { BreadcrumbsComponent } from './breadcrumbs.component';
import { NavService } from './nav.service';
import { ThemeMode, ThemeService } from './theme.service';

interface CreateAction {
  label: string;
  link: string;
  state?: Record<string, string>;
}

/** Authenticated application chrome: sidebar navigation, top bar and the routed page. */
@Component({
  selector: 'app-shell',
  imports: [
    RouterOutlet, RouterLink, RouterLinkActive, ImgUserPictureDirective, LogoComponent, BreadcrumbsComponent, LucideDynamicIcon,
    LucideMenu, LucideSun, LucideMoon, LucideMonitor, LucideLogOut, LucideChevronsUpDown, LucideHeart, LucidePlus, LucideBookOpen,
    LucideExternalLink
  ],
  styles: `
    .nav-link {
      position: relative;
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
    .nav-link.active::before {
      content: '';
      position: absolute;
      left: -0.75rem;
      top: 0.5rem;
      bottom: 0.5rem;
      width: 3px;
      border-radius: 0 3px 3px 0;
      background: var(--app-sidebar-indicator);
    }
  `,
  template: `
    <div class="drawer lg:drawer-open">
      <input id="app-drawer" type="checkbox" class="drawer-toggle" [checked]="drawerOpen()" (change)="drawerOpen.set($any($event.target).checked)" />

      <div class="drawer-content flex min-h-dvh min-w-0 flex-col">
        <header class="sticky top-0 z-30 flex h-14 items-center gap-2 border-b border-base-300 bg-base-100/85 px-3 backdrop-blur sm:px-4 lg:px-8">
          <label for="app-drawer" class="btn btn-ghost btn-sm btn-square lg:hidden" aria-label="Open menu">
            <svg lucideMenu class="size-5"></svg>
          </label>
          <app-breadcrumbs class="min-w-0 flex-1" />

          @if (createActions().length > 0) {
            <div class="dropdown dropdown-end">
              <button type="button" tabindex="0" class="btn btn-primary btn-sm gap-1.5">
                <svg lucidePlus class="size-4"></svg><span class="hidden sm:inline">Create</span>
              </button>
              <ul tabindex="0" class="dropdown-content menu z-40 mt-2 w-52 rounded-box border border-base-300 bg-base-100 p-1 shadow-lg">
                @for (action of createActions(); track action.label) {
                  <li><a [routerLink]="action.link" [state]="action.state" (click)="blur()">{{ action.label }}</a></li>
                }
              </ul>
            </div>
          }

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
          <a routerLink="/dashboard" class="flex h-16 items-center px-5" style="color: var(--app-sidebar-content-strong)">
            <app-logo [size]="30" ring="var(--app-sidebar-bg)" />
          </a>

          <nav class="flex-1 space-y-6 overflow-y-auto px-3 py-4" aria-label="Main">
            @for (section of nav.sections(); track $index) {
              <div>
                @if (section.label) {
                  <p class="mb-1.5 px-3 text-[0.7rem] font-semibold tracking-wider uppercase opacity-55">{{ section.label }}</p>
                }
                <ul class="space-y-0.5">
                  @for (item of section.items; track item.link) {
                    <li>
                      <a class="nav-link" [routerLink]="item.link" routerLinkActive="active"
                        [routerLinkActiveOptions]="{ paths: 'subset', queryParams: 'ignored', fragment: 'ignored', matrixParams: 'ignored' }">
                        <svg [lucideIcon]="item.icon" class="size-[1.125rem] shrink-0"></svg>
                        <span class="truncate">{{ item.label }}</span>
                      </a>
                    </li>
                  }
                </ul>
              </div>
            }
            <div>
              <p class="mb-1.5 px-3 text-[0.7rem] font-semibold tracking-wider uppercase opacity-55">Developers</p>
              <ul class="space-y-0.5">
                <li>
                  <a class="nav-link" [href]="apiDocsUrl" target="_blank" rel="noopener">
                    <svg lucideBookOpen class="size-[1.125rem] shrink-0"></svg>
                    <span class="flex-1 truncate">API docs</span>
                    <svg lucideExternalLink class="size-3.5 opacity-50"></svg>
                  </a>
                </li>
                <li>
                  <a class="nav-link" [href]="discoveryUrl" target="_blank" rel="noopener">
                    <svg lucideExternalLink class="size-[1.125rem] shrink-0"></svg>
                    <span class="flex-1 truncate">Discovery document</span>
                  </a>
                </li>
              </ul>
            </div>
          </nav>

          <div class="border-t px-3 py-3" style="border-color: var(--app-sidebar-border)">
            <div class="dropdown dropdown-top w-full">
              <button type="button" tabindex="0" class="nav-link w-full text-left">
                <span class="avatar">
                  <span class="size-8 overflow-hidden rounded-full bg-white/10">
                    @if (userId()) {
                      <img [userPicture]="userId()!" [displayName]="userName()" [size]="64" alt="" />
                    }
                  </span>
                </span>
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
              <svg lucideHeart class="size-3 fill-current text-error"></svg>
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
  protected readonly nav = inject(NavService);
  protected readonly theme = inject(ThemeService);
  protected readonly version = settings.version;
  protected readonly apiDocsUrl = settings.api_docs;
  protected readonly discoveryUrl = `${settings.auth_settings.authority}/.well-known/openid-configuration`;
  protected readonly drawerOpen = signal(false);
  protected readonly themeOptions: { mode: ThemeMode; label: string }[] = [
    { mode: 'light', label: 'Light' },
    { mode: 'dark', label: 'Dark' },
    { mode: 'system', label: 'System' }
  ];

  protected readonly userId = computed(() => this._auth.currentUser()?.profile?.sub);
  protected readonly userName = computed(() => {
    this._auth.currentUser();
    return this._auth.getDisplayName();
  });
  protected readonly userEmail = computed(() => this._auth.currentUser()?.profile?.email ?? '');
  protected readonly createActions = computed<CreateAction[]>(() => {
    this._auth.currentUser();
    const users = this._auth.isAdminUIUsersWriter();
    const clients = this._auth.isAdminUIClientsWriter();
    return [
      ...(users ? [{ label: 'User', link: '/users/add' }, { label: 'Role', link: '/roles/add' }] : []),
      ...(clients ? [
        { label: 'Client', link: '/clients/add' },
        { label: 'Identity resource', link: '/resources/add', state: { origin: 'identity' } },
        { label: 'API resource', link: '/resources/add', state: { origin: 'api' } }
      ] : []),
      ...(users || clients ? [{ label: 'Claim type', link: '/claim-types/add' }] : [])
    ];
  });

  constructor() {
    // close the mobile drawer after navigating
    inject(Router).events
      .pipe(filter(e => e instanceof NavigationEnd), map(() => false), takeUntilDestroyed())
      .subscribe(open => this.drawerOpen.set(open));
  }

  /** Closes a focus-driven daisyUI dropdown. */
  protected blur(): void {
    (document.activeElement as HTMLElement | null)?.blur();
  }
}
