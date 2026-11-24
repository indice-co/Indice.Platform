import { Injectable, inject, signal } from '@angular/core';
import { AuthService } from '@indice/ng-auth';
import { filter } from 'rxjs';

import { EMBED_MODE } from '../embed/embed-mode';

/**
 * The credentials the server mints for an anonymous caller, delivered on the SSE `start` frame of
 * the turn that created the conversation (`POST /my/chats/stream`). Declared here because the
 * streaming frames are hand-written in `ChatStreamService`.
 */
export interface GuestSessionPayload {
  accessToken: string;
  tokenType?: string;
  expiresIn?: number;
  subject?: string;
}

/** The persisted form. `expiresIn` is resolved to an instant — a lifetime means nothing after a reload. */
export interface GuestSessionState {
  accessToken: string;
  tokenType: string;
  subject: string | null;
  /** Epoch ms, or `null` when the server gave no lifetime (the server's 401 is then the only expiry). */
  expiresAt: number | null;
}

/** Where the guest credential is kept. Per-tab: it survives a reload, not a tab close. */
const STORAGE_KEY = 'dex.guest.session';
/**
 * The widget frame's own key: a same-origin frame shares session storage with the tab around it, and the widget's guest
 * must neither overwrite nor adopt the full UI's.
 */
const EMBED_STORAGE_KEY = 'dex.embed.guest.session';
/** Longest display name sent as `authorName` (the server accepts up to 250). */
const MAX_DISPLAY_NAME_LENGTH = 100;
/** Treat a token as dead this long before it is, so an in-flight request never lands on an expired one. */
const EXPIRY_SKEW_MS = 30_000;

/**
 * The tab's guest credential. The `session` signal is the single source of truth; `sessionStorage` is a mirror
 * so it survives a reload. Consumers never read storage themselves.
 */
@Injectable({ providedIn: 'root' })
export class AuthGuestService {
  private readonly embed = inject(EMBED_MODE);
  private readonly storageKey = this.embed ? EMBED_STORAGE_KEY : STORAGE_KEY;
  private readonly state = signal<GuestSessionState | null>(readStored(this.storageKey));
  private name: string | null = null;

  /** The guest credential, or `null`. Changes on capture and on clear. */
  readonly session = this.state.asReadonly();

  constructor() {
    // A signed-in user never sends guest credentials: drop the guest session the moment one appears
    // (sign-in callback, silent renew, or a user restored from storage on load). The widget frame is the exception:
    // it is guests only, and the user it sees is the one signed in to a same-origin full UI tab, not its own.
    if (!this.embed) {
      inject(AuthService)
        .user$.pipe(filter((user) => !!user && !user.expired))
        .subscribe(() => this.clear());
    }
  }

  /**
   * The name the visitor goes by, when the page hosting the widget passed one (`DexWidget.identify`). Unverified and
   * for display only: it is sent as `authorName` on the anonymous create, so it only applies to a guest yet to be minted.
   */
  get displayName(): string | null {
    return this.name;
  }

  set displayName(value: string | null | undefined) {
    const name = typeof value === 'string' ? value.trim().slice(0, MAX_DISPLAY_NAME_LENGTH) : '';
    this.name = name || null;
  }

  /** The live credential — `null` when absent or expired (an expired one is cleared as a side effect). */
  get current(): GuestSessionState | null {
    const session = this.state();
    if (!session) {
      return null;
    }
    if (isExpired(session)) {
      this.clear();
      return null;
    }
    return session;
  }

  /** Whether there is a guest credential worth sending. */
  get isActive(): boolean {
    return this.current !== null;
  }

  /** `Bearer <token>` for the live credential, or `''`. */
  getAuthorizationHeaderValue(): string {
    const session = this.current;
    return session ? `${session.tokenType} ${session.accessToken}` : '';
  }

  /** Adopt the credential the server just minted. Ignores frames without one (signed-in creates). */
  capture(payload: GuestSessionPayload | null | undefined): void {
    if (!payload?.accessToken) {
      return;
    }
    const session: GuestSessionState = {
      accessToken: payload.accessToken,
      tokenType: payload.tokenType || 'Bearer',
      subject: payload.subject ?? null,
      expiresAt:
        payload.expiresIn && payload.expiresIn > 0 ? Date.now() + payload.expiresIn * 1000 : null,
    };
    this.state.set(session);
    try {
      sessionStorage.setItem(this.storageKey, JSON.stringify(session));
    } catch {
      // Private browsing can refuse storage — the session just won't survive the reload.
    }
  }

  /** Forget the credential. No-op when there is none, so readers see no spurious change. */
  clear(): void {
    try {
      sessionStorage.removeItem(this.storageKey);
    } catch {
      // Storage unavailable — nothing was persisted to begin with.
    }
    this.state.set(null);
  }
}

function isExpired(session: GuestSessionState): boolean {
  return session.expiresAt !== null && session.expiresAt - EXPIRY_SKEW_MS <= Date.now();
}

function readStored(storageKey: string): GuestSessionState | null {
  try {
    const raw = sessionStorage.getItem(storageKey);
    if (!raw) {
      return null;
    }
    const parsed = JSON.parse(raw) as GuestSessionState;
    if (!parsed.accessToken) {
      return null;
    }
    if (isExpired(parsed)) {
      sessionStorage.removeItem(storageKey);
      return null;
    }
    return parsed;
  }
  catch {
    return null;
  }
}
