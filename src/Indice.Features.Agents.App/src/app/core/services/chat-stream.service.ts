import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AuthService } from '@indice/ng-auth';

import { DEX_API_BASE_URL, IChatStreamEvent } from './dex-api.service';

/**
 * Streaming client for the Dex SSE chat endpoints.
 *
 * These endpoints are `POST` + `text/event-stream`, so the browser `EventSource` API can't be used
 * (no POST body, no Authorization header) and Angular's `HttpClient` (with `AuthHttpInterceptor`)
 * isn't involved either. We therefore use `fetch` + `ReadableStream` and attach the bearer token
 * manually from `AuthService`.
 */
@Injectable({ providedIn: 'root' })
export class ChatStreamService {
  private readonly auth = inject(AuthService);
  private readonly baseUrl = (inject(DEX_API_BASE_URL, { optional: true }) ?? 'https://localhost:2001').replace(
    /\/$/,
    '',
  );

  /** POST /my/chats/stream — create a session and stream the first turn. */
  streamCreate(text: string): Observable<IChatStreamEvent> {
    return this.stream(`${this.baseUrl}/my/chats/stream`, text);
  }

  /** POST /my/chats/{id}/messages/stream — stream a follow-up turn in an existing session. */
  streamMessage(sessionId: string, text: string): Observable<IChatStreamEvent> {
    return this.stream(`${this.baseUrl}/my/chats/${sessionId}/messages/stream`, text);
  }

  private stream(url: string, text: string): Observable<IChatStreamEvent> {
    return new Observable<IChatStreamEvent>((subscriber) => {
      const controller = new AbortController();

      void (async () => {
        let response: Response;
        try {
          response = await fetch(url, {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
              Accept: 'text/event-stream',
              Authorization: this.auth.getAuthorizationHeaderValue(),
            },
            body: JSON.stringify({ text }),
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
                subscriber.next(event);
              }
              sep = buffer.indexOf('\n\n');
            }
          }

          const tail = this.parseFrame(buffer);
          if (tail) {
            subscriber.next(tail);
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

  /** Extract the JSON payload from an SSE frame's `data:` line(s) and parse it into an event. */
  private parseFrame(frame: string): IChatStreamEvent | null {
    const payload = frame
      .split('\n')
      .filter((line) => line.startsWith('data:'))
      .map((line) => line.slice('data:'.length).trimStart())
      .join('\n');

    if (!payload || payload === '[DONE]') {
      return null;
    }
    try {
      return JSON.parse(payload) as IChatStreamEvent;
    } catch {
      return null;
    }
  }
}
