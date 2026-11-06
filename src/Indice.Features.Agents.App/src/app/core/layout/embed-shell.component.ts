import { ChangeDetectionStrategy, Component, effect, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthService } from '@indice/ng-auth';
import { firstValueFrom } from 'rxjs';

import { AuthGuestService } from '../auth/auth-guest.service';
import { injectSignedIn } from '../auth/auth-state';
import { signinPopup } from '../auth/popup-signin';
import { isEmbedded, postToHost } from '../embed/embed';
import { assistantName, brandLogo } from '../models/brand';
import { ConversationsStore } from '../services/conversations.store';

/** Where the panel remembers its open conversation, so it survives the host page navigating (the iframe reloads). */
const CONVERSATION_KEY = 'dex.widget.conversation';

/**
 * Compact shell for the embeddable widget's panel (`/embed`, framed by `public/widget.js`): a slim header — brand,
 * new chat, sign in, close — over the routed chat page. No conversation rail and no profile page: the panel is a
 * helper beside someone else's UI; history and the profile stay in the full app.
 */
@Component({
  selector: 'app-embed-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet],
  host: {
    '(document:keydown.escape)': 'onEscape()',
  },
  template: `
    <div class="flex h-screen flex-col bg-base-200">
      <header class="flex h-12 shrink-0 items-center gap-2 border-b border-base-300 bg-base-100 px-3">
        <img [src]="logo()" [alt]="assistantName" class="size-7 shrink-0 rounded-full" />
        <span class="min-w-0 truncate text-sm font-semibold tracking-tight text-base-content">{{ assistantName }}</span>

        <div class="ml-auto flex items-center gap-1">
          @if (store.activeId()) {
            <button
              type="button"
              class="btn btn-ghost btn-sm btn-circle text-base-content/70"
              (click)="newChat()"
              aria-label="New chat"
              title="New chat"
            >
              <svg viewBox="0 0 24 24" fill="none" class="size-4" aria-hidden="true">
                <path d="M12 5v14M5 12h14" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
              </svg>
            </button>
          }
          @if (signedIn() === false) {
            <button
              type="button"
              class="btn btn-primary btn-xs rounded-full"
              [disabled]="signingIn()"
              (click)="signIn()"
            >
              Sign in
            </button>
          }
          @if (embedded) {
            <button
              type="button"
              class="btn btn-ghost btn-sm btn-circle text-base-content/70"
              (click)="close()"
              aria-label="Close chat"
              title="Close"
            >
              <svg viewBox="0 0 24 24" fill="none" class="size-4" aria-hidden="true">
                <path d="M6 6l12 12M18 6L6 18" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
              </svg>
            </button>
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
  /** Whether a host page frames the panel — the close button only means something then. */
  protected readonly embedded = isEmbedded;

  private readonly auth = inject(AuthService);
  private readonly guest = inject(AuthGuestService);

  /** The open conversation — the chat page renders it; the panel only remembers it. */
  protected readonly store = inject(ConversationsStore);
  /** `false` for guests and anonymous visitors; `undefined` until the OIDC user has been read. */
  protected readonly signedIn = injectSignedIn();
  /** True while the sign-in popup is open. */
  protected readonly signingIn = signal(false);

  constructor() {
    // Reopen the conversation the panel showed before the host page navigated — while there is still a
    // credential that owns it (a guest's expires with the guest token).
    const saved = readConversation();
    if (saved && (this.auth.getAuthorizationHeaderValue() || this.guest.isActive)) {
      this.store.select(saved);
    }
    effect(() => writeConversation(this.store.activeId()));
    postToHost('ready');
  }

  protected newChat(): void {
    this.store.startNew();
  }

  protected async signIn(): Promise<void> {
    this.signingIn.set(true);
    try {
      await signinPopup();
      await firstValueFrom(this.auth.loadUser());
      // Whatever the guest started belongs to the guest — the user starts clean (clearing it also resets the store).
      this.guest.clear();
      this.store.startNew();
      this.store.refresh();
    } catch {
      // Popup closed or blocked — the visitor simply stays a guest.
    } finally {
      this.signingIn.set(false);
    }
  }

  protected close(): void {
    postToHost('close');
  }

  /** Escape closes the panel — unless a modal (the delete confirmation) is claiming the key. */
  protected onEscape(): void {
    if (!document.querySelector('dialog[open]')) {
      this.close();
    }
  }
}

function readConversation(): string | null {
  try {
    return sessionStorage.getItem(CONVERSATION_KEY);
  } catch {
    return null;
  }
}

function writeConversation(id: string | null): void {
  try {
    if (id) {
      sessionStorage.setItem(CONVERSATION_KEY, id);
    } else {
      sessionStorage.removeItem(CONVERSATION_KEY);
    }
  } catch {
    // Storage refused (private browsing, blocked site data) — the panel just starts fresh after a navigation.
  }
}
