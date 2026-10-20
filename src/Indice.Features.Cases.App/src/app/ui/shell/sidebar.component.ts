import { Component, inject, output } from '@angular/core';
import { IsActiveMatchOptions, RouterLink, RouterLinkActive } from '@angular/router';
import { NgIcon } from '@ng-icons/core';
import { TranslatePipe } from '@ngx-translate/core';
import { settings } from 'src/app/core/models/settings';
import { NavigationService } from 'src/app/core/navigation/navigation.service';
import { LogoComponent } from '../logo/logo.component';

/** Primary navigation on the frame tint; the active entry is a neutral pill (see `.nav-index`). */
@Component({
  selector: 'app-sidebar',
  imports: [RouterLink, RouterLinkActive, NgIcon, TranslatePipe, LogoComponent],
  template: `
    <div class="flex h-12 shrink-0 items-center px-4">
      <a routerLink="/dashboard" class="rounded-field" (click)="navigate.emit()">
        <app-logo />
      </a>
    </div>
    <nav class="flex-1 overflow-y-auto px-2.5 pt-2 pb-6" [attr.aria-label]="'nav.main' | translate">
      @for (group of navigation.groups(); track $index) {
        @if (group.label) {
          <p class="nav-index-label">{{ group.label | translate }}</p>
        }
        <ul class="nav-index">
          @for (item of group.items; track item.link) {
            <li>
              <a [routerLink]="item.link" routerLinkActive="is-active" [routerLinkActiveOptions]="{ exact: false }"
                 ariaCurrentWhenActive="page" (click)="navigate.emit()">
                @if (item.icon) {
                  <ng-icon [name]="item.icon" size="1rem" />
                }
                <span class="truncate">{{ item.translate ? (item.label | translate) : item.label }}</span>
              </a>
              @if (item.children?.length) {
                <ul class="nav-index mt-0.5 mb-1 ml-[1.1875rem] border-l border-[var(--cases-frame-active)] pl-2">
                  @for (child of item.children; track child.link) {
                    <li>
                      <a class="text-[0.8125rem]" [routerLink]="child.link" [queryParams]="child.queryParams"
                         routerLinkActive="is-active" [routerLinkActiveOptions]="childActiveOptions" ariaCurrentWhenActive="page"
                         (click)="navigate.emit()">
                        <span class="truncate" [title]="child.label">{{ child.label }}</span>
                      </a>
                    </li>
                  }
                </ul>
              }
            </li>
          }
        </ul>
      }
    </nav>
    <footer class="px-4 py-3 text-xs text-quiet">
      <p class="flex items-center gap-1">
        Powered by <a class="link link-hover text-base-content" href="https://www.indice.gr" target="_blank" rel="noopener">Indice</a>
        <ng-icon name="lucideHeart" size="0.75rem" class="heart-mark text-error" aria-label="love" />
      </p>
      <p class="ref mt-0.5">v{{ version }}</p>
    </footer>
  `,
  host: { class: 'flex h-full w-[var(--cases-sidebar-width)] flex-col bg-[var(--cases-frame)]' }
})
export class SidebarComponent {
  protected readonly navigation = inject(NavigationService);
  protected readonly version = settings.version;
  /** A case-type list stays highlighted while paging or filtering it. */
  protected readonly childActiveOptions: IsActiveMatchOptions = { paths: 'subset', queryParams: 'ignored', matrixParams: 'ignored', fragment: 'ignored' };
  /** Emitted when a link is followed, so the mobile drawer can close. */
  public readonly navigate = output<void>();
}
