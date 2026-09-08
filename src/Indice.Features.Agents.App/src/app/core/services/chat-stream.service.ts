import { Injectable, inject } from '@angular/core';
import { Observable, Subscriber } from 'rxjs';
import { AuthService } from '@indice/ng-auth';

import { AuthGuestService, GuestSessionPayload } from '../auth/auth-guest.service';
import { DEX_API_BASE_URL, DexChatPatchOp } from './dex-api.service';

/** First frame of every stream; carries the session id. */
export interface ChatStreamStartFrame {
  type: 'start';
  conversationId?: string;
  /** Present only on an anonymous create — the credential for every later call. */
  guestSession?: GuestSessionPayload;
}

/** Pipeline progress label — ephemeral UI hint, never part of the document. */
export interface ChatStreamStatusFrame {
  type: 'status';
  value?: string;
}

/**
 * A document mutation (JSON Pointer patch). An omitted `path`/`op` inherits the previous delta's
 * effective value (frame compaction) — `JsonPointerPatch` handles the inflation.
 */
export interface ChatStreamDeltaFrame {
  type: 'delta';
  op?: DexChatPatchOp;
  path?: string;
  value?: unknown;
}

/** Terminal failure: safe generic reason; the assembled document is abandoned. */
export interface ChatStreamErrorFrame {
  type: 'error';
  reason?: string;
}

/** Terminal success — bare commit marker; everything already arrived as patches. */
export interface ChatStreamDoneFrame {
  type: 'done';
}

/** The streaming patch protocol v2 frame union. Dispatch on `type` alone. */
export type ChatStreamFrame =
  | ChatStreamStartFrame
  | ChatStreamStatusFrame
  | ChatStreamDeltaFrame
  | ChatStreamErrorFrame
  | ChatStreamDoneFrame;

const KNOWN_FRAME_TYPES: ReadonlySet<string> = new Set(['start', 'status', 'delta', 'error', 'done']);

/**
 * Streaming client for the Dex SSE chat endpoints.
 *
 * These endpoints are `POST` + `text/event-stream`, so the browser `EventSource` API can't be used
 * (no POST body, no Authorization header) and Angular's `HttpClient` (with `authInterceptor`)
 * isn't involved either. We therefore use `fetch` + `ReadableStream` and attach the bearer token
 * manually — the signed-in user's from `AuthService`, else the guest's from `AuthGuestService`,
 * else none (an anonymous create needs no token; the server mints one on the `start` frame).
 */
@Injectable({ providedIn: 'root' })
export class ChatStreamService {
  private readonly auth = inject(AuthService);
  private readonly guest = inject(AuthGuestService);
  private readonly baseUrl = (inject(DEX_API_BASE_URL, { optional: true }) ?? 'https://localhost:2001').replace(
    /\/$/,
    '',
  );

  /** POST /my/chats/stream — create a session and stream the first turn. */
  streamCreate(text: string, agentName?: string | null): Observable<ChatStreamFrame> {
    return this.stream(`${this.baseUrl}/my/chats/stream`, text, agentName);
  }

  /** POST /my/chats/{id}/messages/stream — stream a follow-up turn in an existing session. */
  streamMessage(sessionId: string, text: string, agentName?: string | null): Observable<ChatStreamFrame> {
    return this.stream(`${this.baseUrl}/my/chats/${sessionId}/messages/stream`, text, agentName);
  }

  private stream(url: string, text: string, agentName?: string | null): Observable<ChatStreamFrame> {
    return new Observable<ChatStreamFrame>((subscriber) => {
      const controller = new AbortController();

      void (async () => {
        const userHeader = this.auth.getAuthorizationHeaderValue();
        const authorization = userHeader || this.guest.getAuthorizationHeaderValue();
        const headers: Record<string, string> = {
          'Content-Type': 'application/json',
          Accept: 'text/event-stream',
        };
        if (authorization) {
          headers['Authorization'] = authorization;
        }

        let response: Response;
        try {
          response = await fetch(url, {
            method: 'POST',
            headers,
            // Omit agentName when unset so the server picks its default agent.
            body: JSON.stringify(agentName ? { text, agentName } : { text }),
            signal: controller.signal,
          });
        } catch (err) {
          if (!controller.signal.aborted) {
            subscriber.error(err);
          }
          return;
        }

        if (response.status === 404) {
          subscriber.error(new Error('Session not found.'));
          return;
        }
        if (response.status === 401 && !userHeader) {
          if (this.guest.isActive) {
            // The guest credential was rejected (expired or revoked) — forget it; the next send starts a new chat.
            this.guest.clear();
            subscriber.error(new Error('Your guest session has expired. Start a new chat to continue.'));
          } else {
            subscriber.error(new Error('Unauthorized.'));
          }
        }
        if (!response.ok || !response.body) {
          subscriber.error(new Error(`Streaming request failed (${response.status}).`));
          return;
        }

        const reader = response.body.getReader();
        const decoder = new TextDecoder();
        let buffer = '';

        try {
          for (;;) {
            const { done, value } = await reader.read();
            if (done) {
              break;
            }
            // Strip CR so frames split cleanly on the SSE blank-line separator.
            buffer += decoder.decode(value, { stream: true }).replace(/\r/g, '');

            let sep = buffer.indexOf('\n\n');
            while (sep !== -1) {
              const frame = buffer.slice(0, sep);
              buffer = buffer.slice(sep + 2);
              const event = this.parseFrame(frame);
              if (event) {
                this.emit(subscriber, event);
              }
              sep = buffer.indexOf('\n\n');
            }
          }

          const tail = this.parseFrame(buffer);
          if (tail) {
            this.emit(subscriber, tail);
          }
          subscriber.complete();
        } catch (err) {
          if (!controller.signal.aborted) {
            subscriber.error(err);
          }
        }
      })();

      return () => controller.abort();
    });
  }

  /** Publish a frame — adopting the guest credential a `start` frame may carry before anyone else sees it. */
  private emit(subscriber: Subscriber<ChatStreamFrame>, frame: ChatStreamFrame): void {
    if (frame.type === 'start') {
      this.guest.capture(frame.guestSession);
    }
    subscriber.next(frame);
  }

  /**
   * Extract the JSON payload from an SSE frame's `data:` line(s) and parse it into a protocol
   * frame. Payloads whose `type` is not part of the v2 grammar are dropped — the protocol's
   * forward-compatibility rule (new frame types MUST be ignored by clients).
   */
  private parseFrame(frame: string): ChatStreamFrame | null {
    const payload = frame
      .split('\n')
      .filter((line) => line.startsWith('data:'))
      .map((line) => line.slice('data:'.length).trimStart())
      .join('\n');

    if (!payload) {
      return null;
    }
    try {
      const parsed = JSON.parse(payload) as { type?: string };
      return typeof parsed?.type === 'string' && KNOWN_FRAME_TYPES.has(parsed.type)
        ? (parsed as ChatStreamFrame)
        : null;
    } catch {
      return null;
    }
  }
}
