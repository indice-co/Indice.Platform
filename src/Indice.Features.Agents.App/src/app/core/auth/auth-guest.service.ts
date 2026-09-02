import { Injectable, inject } from '@angular/core';
import { AuthService } from '@indice/ng-auth';
import { BehaviorSubject, filter } from 'rxjs';

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
/** Treat a token as dead this long before it is, so an in-flight request never lands on an expired one. */
const EXPIRY_SKEW_MS = 30_000;

/**
 * The tab's guest credential. `session$` is the single source of truth; `sessionStorage` is a mirror
 * so it survives a reload. Consumers never read storage themselves.
 */
@Injectable({ providedIn: 'root' })
export class AuthGuestService {
  private readonly state = new BehaviorSubject<GuestSessionState | null>(readStored());

  /** The guest credential, or `null`. Emits on capture and on clear. */
  readonly session$ = this.state.asObservable();

  constructor() {
    // A signed-in user never sends guest credentials: drop the guest session the moment one appears
    // (sign-in callback, silent renew, or a user restored from storage on load).
    inject(AuthService)
      .user$.pipe(filter((user) => !!user && !user.expired))
      .subscribe(() => this.clear());
  }

  /** The live credential — `null` when absent or expired (an expired one is cleared as a side effect). */
  get current(): GuestSessionState | null {
    const session = this.state.getValue();
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
    this.state.next(session);
    try {
      sessionStorage.setItem(STORAGE_KEY, JSON.stringify(session));
    } catch {
      // Private browsing can refuse storage — the session just won't survive the reload.
    }
  }

  /** Forget the credential. No-op when there is none, so subscribers see no spurious `null`. */
  clear(): void {
    try {
      sessionStorage.removeItem(STORAGE_KEY);
    } catch {
      // Storage unavailable — nothing was persisted to begin with.
    }
    if (this.state.getValue() !== null) {
      this.state.next(null);
    }
  }
}

function isExpired(session: GuestSessionState): boolean {
  return session.expiresAt !== null && session.expiresAt - EXPIRY_SKEW_MS <= Date.now();
}

function readStored(): GuestSessionState | null {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return null;
    }
    const parsed = JSON.parse(raw) as GuestSessionState;
    if (!parsed.accessToken) {
      return null;
    }
    if (isExpired(parsed)) {
      sessionStorage.removeItem(STORAGE_KEY);
      return null;
    }
    return parsed;
  }
  catch {
    return null;
  }
}
