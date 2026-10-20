import { Component, computed, inject, output, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { AuthService, ImgUserPictureDirective } from '@indice/ng-auth';
import { NgIcon } from '@ng-icons/core';
import { TranslatePipe } from '@ngx-translate/core';
import { BreadcrumbService } from 'src/app/core/navigation/breadcrumb.service';
import { ThemePreference, ThemeService } from 'src/app/core/theme/theme.service';
import { AppLanguagesService } from 'src/app/shared/services/app-languages.service';
import { initialsOf, personTint } from '../people/person.component';

/** Top bar: breadcrumb trail on the left; language, theme and account menus on the right. */
@Component({
  selector: 'app-topbar',
  imports: [RouterLink, NgIcon, TranslatePipe, ImgUserPictureDirective],
  template: `
    <button type="button" class="btn btn-ghost btn-square btn-sm lg:hidden" (click)="menu.emit()" [attr.aria-label]="'shell.openMenu' | translate">
      <ng-icon name="lucideMenu" size="1.25rem" />
    </button>

    <nav class="breadcrumbs min-w-0 flex-1 py-0 text-[0.8125rem]" [attr.aria-label]="'shell.breadcrumb' | translate">
      <ul>
        @for (crumb of breadcrumbs.crumbs(); track crumb.url; let last = $last) {
          <li class="min-w-0">
            @if (last) {
              <span class="truncate font-medium" aria-current="page">{{ crumb.label | translate }}</span>
            } @else {
              <a class="truncate text-quiet" [routerLink]="crumb.url">{{ crumb.label | translate }}</a>
            }
          </li>
        }
      </ul>
    </nav>

    <div class="flex items-center gap-1">
      <div class="dropdown dropdown-end">
        <div tabindex="0" role="button" class="btn btn-ghost btn-sm gap-1.5 px-2" [attr.aria-label]="'shell.language' | translate">
          <ng-icon name="lucideLanguages" size="1.05rem" class="text-quiet" />
          <span class="kbd-ref text-xs font-semibold">{{ languages.selected() }}</span>
        </div>
        <ul tabindex="0" class="dropdown-content menu z-30 mt-2 w-44 rounded-box border border-base-300 bg-base-100 p-1.5 shadow-lg">
          @for (language of languages.languages(); track language.code) {
            <li>
              <button type="button" [class.menu-active]="language.code === languages.selected()" (click)="selectLanguage(language.code)">
                <span class="flex-1">{{ language.nativeName }}</span>
                <span class="kbd-ref text-xs opacity-70">{{ language.code }}</span>
              </button>
            </li>
          }
        </ul>
      </div>

      <div class="dropdown dropdown-end">
        <div tabindex="0" role="button" class="btn btn-ghost btn-sm h-auto gap-2 rounded-full py-1 pr-2 pl-1" [attr.aria-label]="'shell.account' | translate">
          @if (userId() && !pictureFailed()) {
            <div class="avatar">
              <div class="size-7 rounded-full" [style.background-color]="tint()">
                <img [userPicture]="userId()!" [displayName]="displayName()" [size]="64" alt="" (error)="pictureFailed.set(true)" />
              </div>
            </div>
          } @else {
            <span class="person-mark size-7" [style.--tint]="tint()" aria-hidden="true">{{ initials() }}</span>
          }
          <span class="hidden max-w-40 truncate text-[0.8125rem] font-medium sm:inline">{{ displayName() }}</span>
          <ng-icon name="lucideChevronDown" size="0.9rem" class="text-quiet" />
        </div>
        <div tabindex="0" class="dropdown-content z-30 mt-2 w-64 rounded-box border border-base-300 bg-base-100 shadow-lg">
          <div class="border-b border-base-300 px-4 py-3">
            <p class="truncate font-semibold">{{ displayName() }}</p>
            @if (email()) {
              <p class="truncate text-xs text-quiet">{{ email() }}</p>
            }
          </div>
          <div class="px-4 pt-3 pb-1 text-xs text-quiet">{{ 'shell.theme' | translate }}</div>
          <div class="px-3 pb-3">
            <div class="join w-full" role="radiogroup" [attr.aria-label]="'shell.theme' | translate">
              @for (option of themeOptions; track option.value) {
                <button type="button" role="radio" class="btn join-item btn-sm flex-1 gap-1 px-1"
                        [class.btn-active]="theme.preference() === option.value"
                        [attr.aria-checked]="theme.preference() === option.value"
                        (click)="theme.set(option.value)">
                  <ng-icon [name]="option.icon" size="0.95rem" />
                  <span class="text-xs">{{ option.label | translate }}</span>
                </button>
              }
            </div>
          </div>
          <ul class="menu w-full border-t border-base-300 p-1.5">
            <li>
              <a routerLink="/logout">
                <ng-icon name="lucideLogOut" size="1rem" />
                {{ 'nav.logout' | translate }}
              </a>
            </li>
          </ul>
        </div>
      </div>
    </div>
  `,
  host: {
    class: 'sticky top-0 z-20 flex h-[var(--cases-topbar-height)] items-center gap-3 bg-[var(--cases-frame)] px-3 lg:pr-[calc(var(--cases-sheet-inset)+0.5rem)] lg:pl-2'
  }
})
export class TopbarComponent {
  private readonly auth = inject(AuthService);
  protected readonly breadcrumbs = inject(BreadcrumbService);
  protected readonly languages = inject(AppLanguagesService);
  protected readonly theme = inject(ThemeService);
  /** Asks the shell to open the navigation drawer (small screens). */
  public readonly menu = output<void>();

  private readonly user = toSignal(this.auth.user$, { initialValue: null });
  protected readonly userId = computed(() => this.user()?.profile?.sub);
  protected readonly displayName = computed(() => {
    const profile = this.user()?.profile;
    return profile?.name ?? ([profile?.given_name, profile?.family_name].filter(Boolean).join(' ') || profile?.email || '');
  });
  protected readonly email = computed(() => this.user()?.profile?.email);
  /** The identity server has no picture for the user: show initials instead. */
  protected readonly pictureFailed = signal(false);
  protected readonly initials = computed(() => initialsOf(this.displayName()));
  protected readonly tint = computed(() => personTint(this.displayName()));

  protected readonly themeOptions: { value: ThemePreference; label: string; icon: string }[] = [
    { value: 'system', label: 'shell.themeSystem', icon: 'lucideMonitor' },
    { value: 'light', label: 'shell.themeLight', icon: 'lucideSun' },
    { value: 'dark', label: 'shell.themeDark', icon: 'lucideMoon' }
  ];

  protected selectLanguage(code: string): void {
    this.languages.setSelected(code);
    (document.activeElement as HTMLElement | null)?.blur();
  }
}
