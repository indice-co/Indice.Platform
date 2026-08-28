import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';

import { ConversationsStore } from '../services/conversations.store';
import { AppSidebarComponent } from './app-sidebar.component';

/** Where the desktop rail remembers whether it was left collapsed. */
const COLLAPSED_KEY = 'dex.rail.collapsed';

/**
 * Authenticated app shell: a conversation rail on the left over a routed page. The rail frames
 * every route, collapses to an icon strip from `md` up, and slides in as a drawer below it.
 */
@Component({
  selector: 'app-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, RouterOutlet, AppSidebarComponent],
  host: {
    '(document:keydown.escape)': 'onEscape()',
  },
  template: `
    <div class="flex h-screen bg-base-200">
      <!-- Desktop rail -->
      <div
        class="hidden shrink-0 border-r border-base-300 transition-[width] duration-200 md:block"
        [class.w-16]="collapsed()"
        [class.w-72]="!collapsed()"
        [class.lg:w-80]="!collapsed()"
      >
        <app-sidebar
          [sessions]="store.sessions()"
          [activeId]="store.activeId()"
          [loading]="store.loading()"
          [error]="store.error()"
          [collapsed]="collapsed()"
          (select)="openConversation($event)"
          (removed)="removeConversation($event)"
          (create)="newChat()"
          (toggle)="toggleCollapsed()"
          (dismissError)="store.clearError()"
        />
      </div>

      <div class="flex min-w-0 flex-1 flex-col">
        <!-- Mobile bar: the rail is off-canvas below md, so the burger is the only way in. -->
        <header
          class="flex h-14 shrink-0 items-center gap-3 border-b border-base-300 bg-base-100 px-3
                 md:hidden"
        >
          <button
            type="button"
            class="btn btn-ghost btn-sm btn-circle text-base-content/70"
            (click)="drawerOpen.set(true)"
            aria-label="Open conversations"
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
            <img src="dex-logo.png" alt="Dex" class="size-7 shrink-0 rounded-full" />
            <span class="text-base font-semibold tracking-tight text-base-content">Dex</span>
          </a>

          <button
            type="button"
            class="btn btn-primary btn-sm btn-circle ml-auto shadow-sm"
            (click)="newChat()"
            aria-label="New chat"
          >
            <svg viewBox="0 0 24 24" fill="none" class="size-4" aria-hidden="true">
              <path
                d="M12 5v14M5 12h14"
                stroke="currentColor"
                stroke-width="2"
                stroke-linecap="round"
              />
            </svg>
          </button>
        </header>

        <main class="min-h-0 flex-1">
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
      <div
        class="fixed inset-y-0 left-0 z-50 w-72 max-w-[85vw] border-r border-base-300 shadow-xl
               transition-transform duration-200 md:hidden"
        [class.-translate-x-full]="!drawerOpen()"
        [attr.inert]="drawerOpen() ? null : ''"
        role="dialog"
        aria-modal="true"
        aria-label="Conversations"
      >
        <app-sidebar
          [sessions]="store.sessions()"
          [activeId]="store.activeId()"
          [loading]="store.loading()"
          [error]="store.error()"
          [collapsed]="false"
          (select)="openConversation($event)"
          (removed)="removeConversation($event)"
          (create)="newChat()"
          (dismissError)="store.clearError()"
          (navigated)="closeDrawer()"
        />
      </div>
    </div>
  `,
})
export class ShellComponent {
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  /** The shared conversation list — the rail reads it, the chat page renders the active thread. */
  protected readonly store = inject(ConversationsStore);
  /** Desktop icon-rail state, remembered across reloads. */
  protected readonly collapsed = signal(readCollapsed());
  /** Whether the mobile off-canvas rail is showing. */
  protected readonly drawerOpen = signal(false);

  constructor() {
    this.store.refresh();
    // A back gesture must not leave the drawer covering the page it navigated to.
    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => this.drawerOpen.set(false));
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

  protected openConversation(id: string): void {
    this.store.select(id);
    this.router.navigate(['/']);
  }

  protected newChat(): void {
    this.store.startNew();
    this.router.navigate(['/']);
    this.closeDrawer();
  }

  protected removeConversation(id: string): void {
    this.store.remove(id);
  }

  /** Escape closes the drawer — unless a modal (the delete confirmation) is claiming the key. */
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
