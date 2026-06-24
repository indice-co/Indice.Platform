import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '@indice/ng-auth';

import { NAV_ITEMS } from './nav';

/** Authenticated app shell: a top navigation bar (brand + links + user menu) over a routed page. */
@Component({
  selector: 'app-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  template: `
    <div class="flex h-screen flex-col bg-base-200">
      <header
        class="flex h-14 shrink-0 items-center justify-between gap-4 border-b border-base-300
               bg-base-100 px-4 sm:px-6"
      >
        <!-- Brand -->
        <a routerLink="/" class="flex items-center gap-2.5">
          <img src="dex-logo.png" alt="Dex" class="size-8 rounded-full" />
          <span class="leading-tight">
            <span class="block text-lg font-semibold tracking-tight text-base-content">Dex</span>
            <span
              class="-mt-0.5 hidden font-mono text-[0.6rem] uppercase tracking-[0.22em]
                     text-base-content/45 sm:block"
            >
              knowledge assistant
            </span>
          </span>
        </a>

        <!-- Primary nav -->
        <nav class="hidden flex-1 items-center gap-1 md:flex">
          @for (item of navItems; track item.path) {
            <a
              [routerLink]="item.path"
              routerLinkActive="bg-primary/10 text-primary"
              [routerLinkActiveOptions]="{ exact: item.exact }"
              class="inline-flex items-center gap-2 rounded-field px-3 py-1.5 text-sm font-medium
                     text-base-content/65 transition hover:bg-base-200 hover:text-base-content"
            >
              <svg viewBox="0 0 24 24" fill="none" class="size-[1.05rem]" aria-hidden="true">
                <path
                  [attr.d]="item.icon"
                  stroke="currentColor"
                  stroke-width="1.7"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                />
              </svg>
              {{ item.label }}
            </a>
          }
        </nav>

        <!-- User menu -->
        <div class="dropdown dropdown-end">
          <div
            tabindex="0"
            role="button"
            class="flex items-center gap-2 rounded-full py-1 pl-1 pr-1 transition hover:bg-base-200"
          >
            <span
              class="grid size-9 place-items-center rounded-full bg-primary/10 text-sm font-semibold
                     text-primary"
            >
              {{ initials }}
            </span>
            <span class="hidden max-w-32 truncate pr-1 text-sm font-medium sm:block">
              {{ displayName }}
            </span>
          </div>
          <ul
            tabindex="0"
            class="dropdown-content menu z-10 mt-2 w-56 rounded-box border border-base-300
                   bg-base-100 p-2 shadow-lg"
          >
            <li class="menu-title">
              <span class="truncate text-base-content/60">{{ displayName }}</span>
            </li>
            <li>
              <button type="button" (click)="logout()">
                <svg viewBox="0 0 24 24" fill="none" class="size-4" aria-hidden="true">
                  <path
                    d="M15 17l5-5-5-5M20 12H9M12 3H6a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h6"
                    stroke="currentColor"
                    stroke-width="1.8"
                    stroke-linecap="round"
                    stroke-linejoin="round"
                  />
                </svg>
                Sign out
              </button>
            </li>
          </ul>
        </div>
      </header>

      <main class="min-h-0 flex-1">
        <router-outlet />
      </main>
    </div>
  `,
})
export class ShellComponent {
  private readonly auth = inject(AuthService);

  protected readonly navItems = NAV_ITEMS;
  protected readonly displayName = this.auth.getDisplayName() || 'You';
  protected readonly initials = this.computeInitials(this.displayName);

  protected logout(): void {
    this.auth.signoutRedirect();
  }

  private computeInitials(name: string): string {
    const parts = name.trim().split(/\s+/).filter(Boolean);
    if (parts.length === 0) {
      return '?';
    }
    if (parts.length === 1) {
      return parts[0].slice(0, 1).toUpperCase();
    }
    return (parts[0].slice(0, 1) + parts[parts.length - 1].slice(0, 1)).toUpperCase();
  }
}
