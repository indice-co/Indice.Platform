import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService, ImgUserPictureDirective } from '@indice/ng-auth';
import { filter, map } from 'rxjs';
import {
  LucideChevronsUpDown, LucideDynamicIcon, LucideHeart, LucideLanguages, LucideLogOut, LucideMenu, LucideMonitor, LucideMoon, LucideSun
} from '@lucide/angular';
import { settings } from '../core/models/settings';
import { AppLanguagesService } from '../shared/services/app-languages.service';
import { LogoComponent } from '../ui/logo/logo.component';
import { BreadcrumbsComponent } from './breadcrumbs.component';
import { NavService } from './nav.service';
import { ThemeMode, ThemeService } from './theme.service';

/** Authenticated application chrome: sidebar navigation, top bar and the routed page. */
@Component({
  selector: 'app-shell',
  imports: [
    RouterOutlet, RouterLink, RouterLinkActive, TranslatePipe, ImgUserPictureDirective, LogoComponent, BreadcrumbsComponent,
    LucideDynamicIcon, LucideMenu, LucideSun, LucideMoon, LucideMonitor, LucideLanguages, LucideLogOut, LucideChevronsUpDown, LucideHeart
  ],
  styles: `
    .nav-link {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      border-radius: var(--radius-field);
      padding: 0.5rem 0.75rem;
      font-size: 0.875rem;
      color: var(--app-sidebar-content);
      position: relative;
      transition: background-color 120ms, color 120ms;
    }
    .nav-link:hover { background: var(--app-sidebar-hover); color: var(--app-sidebar-content-strong); }
    .nav-link:focus-visible { outline: 2px solid var(--app-highlight); outline-offset: 1px; }
    .nav-link.active { background: var(--app-sidebar-hover); color: var(--app-sidebar-content-strong); font-weight: 600; }
    /* the active item is marked with a highlighter stroke, like a line marked on a case file */
    .nav-link.active::before {
      content: '';
      position: absolute;
      left: -0.75rem;
      top: 0.45rem;
      bottom: 0.45rem;
      width: 3px;
      border-radius: 0 3px 3px 0;
      background: var(--app-highlight);
    }
  `,
  template: `
    <div class="drawer lg:drawer-open">
      <input id="app-drawer" type="checkbox" class="drawer-toggle" [checked]="drawerOpen()" (change)="drawerOpen.set($any($event.target).checked)" />

      <div class="drawer-content flex min-h-dvh min-w-0 flex-col">
        <header class="sticky top-0 z-30 flex h-14 items-center gap-2 border-b border-base-300 bg-base-100/90 px-3 backdrop-blur sm:px-4 lg:px-8">
          <label for="app-drawer" class="btn btn-ghost btn-sm btn-square lg:hidden" [attr.aria-label]="'nav.openMenu' | translate">
            <svg lucideMenu class="size-5"></svg>
          </label>
          <app-breadcrumbs class="min-w-0 flex-1" />

          <div class="dropdown dropdown-end">
            <button type="button" tabindex="0" class="btn btn-ghost btn-sm gap-1.5" [attr.aria-label]="'nav.language' | translate">
              <svg lucideLanguages class="size-4"></svg>
              <span class="text-xs font-semibold">{{ languages.selected() }}</span>
            </button>
            <ul tabindex="0" class="dropdown-content menu z-40 mt-2 w-44 rounded-box border border-base-300 bg-base-100 p-1 shadow-lg">
              @for (language of languages.languages(); track language.code) {
                <li>
                  <button type="button" [class.menu-active]="language.code === languages.selected()" (click)="languages.setSelected(language.code); blur()">
                    <span class="w-6 text-xs font-semibold text-base-content/60">{{ language.code }}</span>{{ language.name }}
                  </button>
                </li>
              }
            </ul>
          </div>

          <div class="dropdown dropdown-end">
            <button type="button" tabindex="0" class="btn btn-ghost btn-sm btn-square" [attr.aria-label]="'nav.theme' | translate">
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
                    {{ option.label | translate }}
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
        <label for="app-drawer" class="drawer-overlay" [attr.aria-label]="'nav.closeMenu' | translate"></label>
        <aside class="flex min-h-full flex-col" style="width: var(--app-sidebar-width); background: var(--app-sidebar-bg); color: var(--app-sidebar-content)">
          <a routerLink="/dashboard" class="flex h-16 items-center px-5" style="color: var(--app-sidebar-content-strong)">
            <app-logo [size]="30" />
          </a>

          <nav class="flex-1 space-y-6 overflow-y-auto px-3 py-4" [attr.aria-label]="'nav.main' | translate">
            @for (section of nav.sections(); track $index) {
              <div>
                @if (section.label) {
                  <p class="mb-1.5 px-3 text-xs font-medium opacity-60">{{ section.label | translate }}</p>
                }
                <ul class="space-y-0.5">
                  @for (item of section.items; track item.link) {
                    <li>
                      <a class="nav-link" [routerLink]="item.link" [queryParams]="item.queryParams" routerLinkActive="active"
                        [routerLinkActiveOptions]="{ paths: 'subset', queryParams: 'ignored', fragment: 'ignored', matrixParams: 'ignored' }">
                        <svg [lucideIcon]="item.icon" class="size-[1.125rem] shrink-0 opacity-90"></svg>
                        <span class="truncate">{{ item.translate ? (item.label | translate) : item.label }}</span>
                      </a>
                    </li>
                  }
                </ul>
              </div>
            }
          </nav>

          <div class="border-t px-3 py-3" style="border-color: var(--app-sidebar-hover)">
            <div class="dropdown dropdown-top w-full">
              <button type="button" tabindex="0" class="nav-link w-full text-left">
                <span class="avatar">
                  <span class="size-8 overflow-hidden rounded-full bg-base-300">
                    @if (userId()) {
                      <img [userPicture]="userId()!" [displayName]="userName()" [size]="64" alt="" />
                    }
                  </span>
                </span>
                <span class="min-w-0 flex-1">
                  <span class="block truncate font-medium" style="color: var(--app-sidebar-content-strong)" title="{{userName()}}">{{ userName() }}</span>
                  <span class="block truncate text-xs opacity-70">{{ userEmail() }}</span>
                </span>
                <svg lucideChevronsUpDown class="size-4 shrink-0 opacity-60"></svg>
              </button>
              <ul tabindex="0" class="dropdown-content menu z-50 mb-2 w-full rounded-box border border-base-300 bg-base-100 p-1 text-base-content shadow-lg">
                <li>
                  <a routerLink="/logout"><svg lucideLogOut class="size-4"></svg>{{ 'nav.logout' | translate }}</a>
                </li>
              </ul>
            </div>
            <p class="mt-3 flex items-center gap-1 px-3 text-xs opacity-55">
              Powered by <a class="link link-hover" href="https://www.indice.gr" target="_blank" rel="noopener">Indice</a>
              <svg lucideHeart class="size-3 fill-current text-error"></svg>
              <span class="ml-auto docket">v{{ version }}</span>
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
  protected readonly languages = inject(AppLanguagesService);
  protected readonly theme = inject(ThemeService);
  protected readonly version = settings.version;
  protected readonly drawerOpen = signal(false);
  protected readonly themeOptions: { mode: ThemeMode; label: string }[] = [
    { mode: 'light', label: 'theme.light' },
    { mode: 'dark', label: 'theme.dark' },
    { mode: 'system', label: 'theme.system' }
  ];

  private readonly _user = toSignal(this._auth.user$, { initialValue: this._auth.getCurrentUser() });
  protected readonly userId = () => this._user()?.profile?.sub;
  protected readonly userName = () => this._user()?.profile?.given_name ? `${this._user()?.profile?.given_name} ${this._user()?.profile?.family_name ?? ''}`.trim() :
                                        this._user()?.profile?.name ?? this._user()?.profile?.email ?? '';
  protected readonly userEmail = () => this._user()?.profile?.email ?? '';

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
