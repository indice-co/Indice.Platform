import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  afterRenderEffect,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { AuthGuestService } from '../auth/auth-guest.service';
import { AppSidebarComponent } from '../layout/app-sidebar.component';
import { assistantName, brandLogo } from '../models/brand';
import { ConversationsStore } from '../services/conversations.store';
import { EmbedBridgeService } from './embed-bridge.service';

/**
 * The chat widget's frame (`/embed`): the full UI's mobile layout — a bar with the burger, brand and new chat, and the
 * conversation rail as a drawer — plus a close button. Guests only, so there is no sign-in and no account. Closing asks
 * `widget.js` on the hosting page to collapse the panel; the frame itself stays alive.
 */
@Component({
  selector: 'app-embed-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, AppSidebarComponent],
  host: {
    '(document:keydown.escape)': 'onEscape()',
  },
  template: `
    <div class="flex h-dvh flex-col bg-base-200">
      <!-- The full UI's mobile bar, plus the widget's close button. -->
      <header
        class="flex h-14 shrink-0 items-center gap-3 border-b border-base-300 bg-base-100 px-3"
        [attr.inert]="drawerOpen() ? '' : null"
      >
        <button
          #burger
          type="button"
          class="btn btn-ghost btn-sm btn-circle text-base-content/70"
          (click)="drawerOpen.set(true)"
          aria-label="Open conversations"
          [attr.aria-expanded]="drawerOpen()"
        >
          <svg viewBox="0 0 24 24" fill="none" class="size-5" aria-hidden="true">
            <path d="M4 7h16M4 12h16M4 17h16" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
          </svg>
        </button>

        <span class="flex min-w-0 items-center gap-2">
          <img [src]="logo()" alt="" class="size-7 shrink-0 rounded-full" />
          <span class="truncate text-base font-semibold tracking-tight text-base-content">{{ assistantName }}</span>
        </span>

        <div class="ml-auto flex shrink-0 items-center gap-1.5">
          <button
            type="button"
            class="btn btn-primary btn-sm btn-circle shadow-sm"
            (click)="newChat()"
            aria-label="New chat"
            title="New chat"
          >
            <svg viewBox="0 0 24 24" fill="none" class="size-4" aria-hidden="true">
              <path d="M12 5v14M5 12h14" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
            </svg>
          </button>
          @if (bridge.framed) {
            <button
              type="button"
              class="btn btn-ghost btn-sm btn-circle text-base-content/60"
              (click)="bridge.close()"
              aria-label="Close chat"
              title="Close"
            >
              <svg viewBox="0 0 24 24" fill="none" class="size-5" aria-hidden="true">
                <path d="M6 6l12 12M18 6 6 18" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
              </svg>
            </button>
          }
        </div>
      </header>

      <main class="min-h-0 flex-1" [attr.inert]="drawerOpen() ? '' : null">
        <router-outlet />
      </main>

      <!-- Conversations drawer: the full UI's rail, without the account anchor. -->
      @if (drawerOpen()) {
        <div class="fixed inset-0 z-40 bg-black/40" (click)="closeDrawer()" aria-hidden="true"></div>
      }
      <div
        #drawer
        class="fixed inset-y-0 left-0 z-50 w-72 max-w-[85vw] border-r border-base-300 shadow-xl
               transition-transform duration-200"
        [class.-translate-x-full]="!drawerOpen()"
        [attr.inert]="drawerOpen() ? null : ''"
        [attr.aria-modal]="drawerOpen() ? 'true' : null"
        tabindex="-1"
        role="dialog"
        aria-label="Conversations"
      >
        <app-sidebar
          [sessions]="store.sessions()"
          [activeId]="store.activeId()"
          [loading]="store.loading()"
          [error]="store.error()"
          [collapsed]="false"
          [embed]="true"
          (select)="store.select($event)"
          (removed)="store.remove($event)"
          (create)="store.startNew()"
          (dismissError)="store.clearError()"
          (navigated)="closeDrawer()"
        />
      </div>
    </div>
  `,
})
export class EmbedShellComponent {
  /** Brand mark of the active theme. */
  protected readonly logo = brandLogo;
  /** Display name of the assistant. */
  protected readonly assistantName = assistantName;

  /** Constructed here, ahead of the chat page, so the previous page's chat is open before the page reads it. */
  protected readonly bridge = inject(EmbedBridgeService);
  protected readonly store = inject(ConversationsStore);

  /** Whether the conversations drawer is showing. */
  protected readonly drawerOpen = signal(false);

  private readonly drawer = viewChild.required<ElementRef<HTMLElement>>('drawer');
  private readonly burger = viewChild.required<ElementRef<HTMLButtonElement>>('burger');
  /** Guards the restore so the first render doesn't yank focus to the burger. */
  private drawerWasOpen = false;

  constructor() {
    // afterRender, not effect: the `inert` attribute must be off the drawer before we focus into it.
    afterRenderEffect(() => {
      const open = this.drawerOpen();
      if (open) {
        this.drawer().nativeElement.focus();
      } else if (this.drawerWasOpen) {
        this.burger().nativeElement.focus();
      }
      this.drawerWasOpen = open;
    });

    // A visitor who has not chatted yet has no list (and no credential to fetch one); the chat page refreshes the
    // store once the first turn has minted the guest.
    if (inject(AuthGuestService).isActive) {
      this.store.refresh();
    }
  }

  protected closeDrawer(): void {
    this.drawerOpen.set(false);
  }

  protected newChat(): void {
    this.store.startNew();
    this.closeDrawer();
  }

  /** Escape closes the drawer first, then the widget — unless a modal (a delete confirmation) is claiming the key. */
  protected onEscape(): void {
    if (document.querySelector('dialog[open]')) {
      return;
    }
    if (this.drawerOpen()) {
      this.closeDrawer();
      return;
    }
    this.bridge.close();
  }
}
