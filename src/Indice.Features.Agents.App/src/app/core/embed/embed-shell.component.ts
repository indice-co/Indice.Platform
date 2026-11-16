import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { AuthGuestService } from '../auth/auth-guest.service';
import { assistantName, brandLogo } from '../models/brand';
import { ConversationsStore } from '../services/conversations.store';
import { EmbedBridgeService } from './embed-bridge.service';

/** How many recent chats the header menu lists. */
const RECENT_LIMIT = 20;

/**
 * The chat widget's frame (`/embed`): a compact header — brand, recent chats, new chat, close — over the chat page.
 * Guests only, so there is no sign-in, no account and no rail. Closing asks `widget.js` on the hosting page to collapse
 * the panel; the frame itself stays alive.
 */
@Component({
  selector: 'app-embed-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, RouterOutlet],
  host: {
    '(document:keydown.escape)': 'onEscape()',
    '(document:click)': 'onDocumentClick($event)',
  },
  template: `
    <div class="flex h-dvh flex-col bg-base-200">
      <header
        class="relative flex h-12 shrink-0 items-center gap-1 border-b border-base-300 bg-base-100 pl-3 pr-1.5"
      >
        <img [src]="logo()" alt="" class="size-7 shrink-0 rounded-full" />
        <span class="ml-1.5 min-w-0 truncate text-sm font-semibold tracking-tight text-base-content">
          {{ assistantName }}
        </span>

        <div class="ml-auto flex items-center" #menuHost>
          @if (recent().length > 0) {
            <button
              #menuButton
              type="button"
              class="btn btn-ghost btn-sm btn-circle text-base-content/60"
              (click)="toggleMenu()"
              aria-label="Recent chats"
              title="Recent chats"
              aria-haspopup="true"
              [attr.aria-expanded]="menuOpen()"
              aria-controls="dex-embed-recent"
            >
              <svg viewBox="0 0 24 24" fill="none" class="size-[1.15rem]" aria-hidden="true">
                <path
                  d="M3 12a9 9 0 1 0 3-6.7M3 4v4h4M12 7v5l3 2"
                  stroke="currentColor"
                  stroke-width="2"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                />
              </svg>
            </button>
          }
          <button
            type="button"
            class="btn btn-ghost btn-sm btn-circle text-base-content/60"
            (click)="newChat()"
            [disabled]="!store.activeId()"
            aria-label="New chat"
            title="New chat"
          >
            <svg viewBox="0 0 24 24" fill="none" class="size-[1.15rem]" aria-hidden="true">
              <path
                d="M12 20h9M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4Z"
                stroke="currentColor"
                stroke-width="2"
                stroke-linecap="round"
                stroke-linejoin="round"
              />
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

          @if (menuOpen()) {
            <div
              id="dex-embed-recent"
              class="absolute right-2 top-full z-30 mt-1 max-h-[60vh] w-72 max-w-[calc(100vw-1rem)] overflow-y-auto
                     rounded-box border border-base-300 bg-base-100 p-1.5 shadow-lg"
            >
              <p class="px-2.5 pb-1 pt-1.5 font-mono text-[0.6rem] uppercase tracking-[0.22em] text-base-content/45">
                Recent chats
              </p>
              <ul class="flex flex-col">
                @for (session of recent(); track session.id) {
                  <li>
                    <button
                      type="button"
                      class="flex w-full items-baseline gap-2 rounded-field px-2.5 py-2 text-left text-sm
                             transition hover:bg-base-200"
                      [class.bg-base-200]="session.id === store.activeId()"
                      [class.font-medium]="session.id === store.activeId()"
                      [attr.aria-current]="session.id === store.activeId() ? 'true' : null"
                      (click)="openConversation(session.id!)"
                    >
                      <span class="min-w-0 flex-1 truncate text-base-content">{{ session.title || 'New chat' }}</span>
                      <span class="shrink-0 text-xs text-base-content/45">
                        {{ session.lastActivityAt ?? session.createdAt | date: 'MMM d' }}
                      </span>
                    </button>
                  </li>
                }
              </ul>
            </div>
          }
        </div>
      </header>

      <main class="min-h-0 flex-1">
        <router-outlet />
      </main>
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

  /** Whether the recent chats menu is showing. */
  protected readonly menuOpen = signal(false);
  protected readonly recent = computed(() => this.store.sessions().slice(0, RECENT_LIMIT));

  private readonly menuHost = viewChild.required<ElementRef<HTMLElement>>('menuHost');
  private readonly menuButton = viewChild<ElementRef<HTMLButtonElement>>('menuButton');

  constructor() {
    // A visitor who has not chatted yet has no list (and no credential to fetch one); the chat page refreshes the
    // store once the first turn has minted the guest.
    if (inject(AuthGuestService).isActive) {
      this.store.refresh();
    }
  }

  protected toggleMenu(): void {
    this.menuOpen.update((open) => !open);
  }

  protected openConversation(id: string): void {
    this.store.select(id);
    this.menuOpen.set(false);
  }

  protected newChat(): void {
    this.store.startNew();
    this.menuOpen.set(false);
  }

  /** Escape closes the menu first, then the widget — unless a modal (a delete confirmation) is claiming the key. */
  protected onEscape(): void {
    if (this.menuOpen()) {
      this.menuOpen.set(false);
      this.menuButton()?.nativeElement.focus();
      return;
    }
    if (!document.querySelector('dialog[open]')) {
      this.bridge.close();
    }
  }

  protected onDocumentClick(event: MouseEvent): void {
    if (this.menuOpen() && !this.menuHost().nativeElement.contains(event.target as Node)) {
      this.menuOpen.set(false);
    }
  }
}
