import { DestroyRef, Injectable, effect, inject } from '@angular/core';

import { AuthGuestService } from '../auth/auth-guest.service';
import { ConversationsStore } from '../services/conversations.store';
import { WIDGET_ORIGINS } from './embed-mode';

/** Every widget message, in both directions: `{ dexWidget: 1, type, payload? }`. Anything else is not ours. */
export interface WidgetMessage {
  dexWidget: 1;
  type: string;
  payload?: unknown;
}

/** The record the hosting page is about (`?refid=&reftype=` on the frame address), if any. */
export interface WidgetTopic {
  refId: string | null;
  refType: string | null;
}

/** The chat the frame had open, kept so the next page of the hosting site resumes it. */
interface StoredChat extends WidgetTopic {
  id: string;
}

/** Per tab, like the guest credential the chat belongs to. */
const CHAT_STORAGE_KEY = 'dex.embed.chat';

/** How long a chat that starts by itself waits for the hosting page's `identify` before going ahead without a name. */
const CONFIGURE_TIMEOUT_MS = 500;

/**
 * The `/embed` frame's side of the widget: the `postMessage` channel with `widget.js` on the hosting page, and which
 * chat to resume when the visitor moves to another page of that site (the frame is created anew on every page).
 *
 * Trust: messages are accepted only from the parent window, only from an allowed origin (`WIDGET_ORIGINS` plus this
 * page's own), and only from the first such origin seen, which the frame then talks to exclusively.
 *
 * Constructed by `EmbedShellComponent` — the frame's first render, before the chat page reads the open conversation.
 */
@Injectable({ providedIn: 'root' })
export class EmbedBridgeService {
  private readonly guest = inject(AuthGuestService);
  private readonly store = inject(ConversationsStore);
  private readonly origins = [...new Set([window.location.origin, ...inject(WIDGET_ORIGINS)])];

  /** Whether the page runs inside a frame. `/embed` opened in a tab of its own has no widget to talk to. */
  readonly framed = window.parent !== window;

  /** The hosting page's topic, read before the chat page drops `refid`/`reftype` from the address. */
  readonly topic: WidgetTopic = readTopic();

  /** The parent origin this frame talks to, once known. */
  private parentOrigin: string | null = this.framed ? guessParentOrigin(this.origins) : null;
  /** What was last persisted, so a page without a topic keeps the topic the chat was started on. */
  private stored: StoredChat | null = readStoredChat();

  private markConfigured!: () => void;
  /**
   * Settles once the hosting page has said who the visitor is — its `identify` answer to `ready`, sent even when it
   * has no name. A chat that starts by itself waits for this, because the name only counts on the create that mints
   * the guest. Settles at once outside a frame, and after `CONFIGURE_TIMEOUT_MS` when the parent never answers.
   */
  readonly configured = new Promise<void>((resolve) => (this.markConfigured = resolve));

  constructor() {
    this.restoreChat();
    if (this.framed) {
      const timer = setTimeout(() => this.markConfigured(), CONFIGURE_TIMEOUT_MS);
      inject(DestroyRef).onDestroy(() => clearTimeout(timer));
    } else {
      this.markConfigured();
    }

    // Remember the open chat for the next page, with the topic it actually has — never this page's topic by default:
    // a chat picked from the recent list on order B's page is not about order B. A new, unsent chat (or a cleared
    // guest) forgets it.
    effect(() => {
      const id = this.store.activeId();
      if (!id) {
        this.persist(null);
        return;
      }
      this.persist({ id, ...this.topicOf(id) });
    });

    const onMessage = (event: MessageEvent) => this.receive(event);
    window.addEventListener('message', onMessage);
    inject(DestroyRef).onDestroy(() => window.removeEventListener('message', onMessage));

    this.post('ready');
  }

  /** Ask the hosting page to collapse the panel (the header × or Escape inside the frame). */
  close(): void {
    this.post('close');
  }

  /**
   * Resume the chat of the previous page, unless the guest it belonged to is gone or this page is about another record
   * (order B's page must not continue order A's thread). A page with no topic resumes whatever was open.
   */
  private restoreChat(): void {
    const stored = this.stored;
    if (!stored) {
      return;
    }
    const otherRecord =
      !!this.topic.refId && (this.topic.refId !== stored.refId || this.topic.refType !== stored.refType);
    if (!this.guest.isActive || otherRecord) {
      this.persist(null);
      return;
    }
    this.store.select(stored.id);
  }

  /**
   * The topic a chat really has, as far as the frame knows: the one it was stored with (a resumed chat), or the one
   * this page created it on. A chat opened from the recent list has none — its record is unknown here.
   */
  private topicOf(id: string): WidgetTopic {
    if (this.stored?.id === id) {
      return { refId: this.stored.refId, refType: this.stored.refType };
    }
    const created = this.store.topicOf(id);
    return { refId: created?.referenceId ?? null, refType: created?.referenceType ?? null };
  }

  private receive(event: MessageEvent): void {
    if (!this.framed || event.source !== window.parent || !isWidgetMessage(event.data)) {
      return;
    }
    const allowed = this.parentOrigin ? event.origin === this.parentOrigin : this.origins.includes(event.origin);
    if (!allowed) {
      return;
    }
    this.parentOrigin = event.origin;
    switch (event.data.type) {
      case 'identify':
        this.guest.displayName = (event.data.payload as { name?: string } | undefined)?.name;
        this.markConfigured();
        break;
    }
  }

  /** Post to the hosting page — to its pinned origin, or, until that is known, to each allowed one (only the real one delivers). */
  private post(type: string, payload?: unknown): void {
    if (!this.framed) {
      return;
    }
    const message: WidgetMessage = payload === undefined ? { dexWidget: 1, type } : { dexWidget: 1, type, payload };
    for (const origin of this.parentOrigin ? [this.parentOrigin] : this.origins) {
      window.parent.postMessage(message, origin);
    }
  }

  private persist(chat: StoredChat | null): void {
    if (chat?.id === this.stored?.id && chat?.refId === this.stored?.refId && chat?.refType === this.stored?.refType) {
      return;
    }
    this.stored = chat;
    try {
      if (chat) {
        sessionStorage.setItem(CHAT_STORAGE_KEY, JSON.stringify(chat));
      } else {
        sessionStorage.removeItem(CHAT_STORAGE_KEY);
      }
    } catch {
      // Storage unavailable (blocked site data): the widget still works, it just starts fresh on every page.
    }
  }
}

/** Whether `data` is a widget message envelope. */
export function isWidgetMessage(data: unknown): data is WidgetMessage {
  return (
    typeof data === 'object' &&
    data !== null &&
    (data as WidgetMessage).dexWidget === 1 &&
    typeof (data as WidgetMessage).type === 'string'
  );
}

/**
 * The parent's origin when the browser tells it — `location.ancestorOrigins` (Chromium, Safari) or the referrer of the
 * frame's first load — and it is an allowed one. `null` when unknown; the first message from the parent then pins it.
 */
function guessParentOrigin(allowed: readonly string[]): string | null {
  const candidates: string[] = [];
  const ancestors = (window.location as Location & { ancestorOrigins?: DOMStringList }).ancestorOrigins;
  if (ancestors?.length) {
    candidates.push(ancestors[0]);
  }
  try {
    if (document.referrer) {
      candidates.push(new URL(document.referrer).origin);
    }
  } catch {
    // A malformed referrer tells us nothing.
  }
  return candidates.find((origin) => allowed.includes(origin)) ?? null;
}

function readTopic(): WidgetTopic {
  const params = new URLSearchParams(window.location.search);
  return {
    refId: params.get('refid')?.trim() || null,
    refType: params.get('reftype')?.trim() || null,
  };
}

function readStoredChat(): StoredChat | null {
  try {
    const raw = sessionStorage.getItem(CHAT_STORAGE_KEY);
    const parsed = raw ? (JSON.parse(raw) as Partial<StoredChat>) : null;
    return typeof parsed?.id === 'string' && parsed.id
      ? { id: parsed.id, refId: parsed.refId ?? null, refType: parsed.refType ?? null }
      : null;
  } catch {
    return null;
  }
}
