import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, throwError, timer } from 'rxjs';
import { retry, timeout } from 'rxjs/operators';

import { settings } from '../models/settings';

// Retry transient failures automatically before surfacing an error. Kept short so the page
// still fails fast when the API genuinely can't be reached.
const REQUEST_TIMEOUT_MS = 8000;
const MAX_RETRIES = 2;
const RETRY_BASE_DELAY_MS = 500;

// Only transient failures are worth retrying. A 404 or an auth/config error (401/403) will
// never resolve by trying again.
function isTransientError(error: unknown): boolean {
  // RxJS `timeout` throws a TimeoutError; retry it.
  if (error instanceof Error && error.name === 'TimeoutError') {
    return true;
  }
  if (error instanceof HttpErrorResponse) {
    // status 0 == network error / CORS / connection refused.
    return (
      error.status === 0 ||
      error.status === 408 ||
      error.status === 500 ||
      error.status === 502 ||
      error.status === 503 ||
      error.status === 504
    );
  }
  return false;
}

/** True when a request failed because the caller is not authenticated. */
export function isUnauthenticated(error: unknown): boolean {
  return error instanceof HttpErrorResponse && error.status === 401;
}

/** Lifecycle of the manual-capture pre-authorization (Stripe PaymentIntent). */
export type PaymentAuthStatus = 'Created' | 'Succeeded' | 'Failed' | 'Cancelled';

/** A payment attempt (session) as returned by the API. */
export interface PaymentSession {
  id?: string;
  conversationId?: string;
  /** The Stripe PaymentIntent client_secret the card is confirmed against. */
  paymentSessionSecret?: string | null;
  paymentAuthorizationStatus?: PaymentAuthStatus | null;
  /** The pre-auth (hold) amount, in major currency units. */
  firstCaptureAmount?: number | null;
  currency?: string | null;
}

@Injectable({ providedIn: 'root' })
export class PaymentService {
  private readonly http = inject(HttpClient);

  /**
   * Initiate (or idempotently reuse) the payment authorization for the given chat. Returns the
   * session with its `paymentSessionSecret`. A chat that already has a live attempt
   * (Created/Succeeded) returns the previously issued secret rather than authorizing again.
   *
   * A 401 propagates so the caller can send the user to sign in.
   */
  createPaymentAttempt(chatId: string): Observable<PaymentSession> {
    return this.withTransientRetry(
      this.http.post<PaymentSession>(
        `${settings.api_url}/my/chats/${encodeURIComponent(chatId)}/payment-attempts`,
        null,
      ),
    );
  }

  /** The chat's current payment attempt — polled by the status page until the webhook lands. */
  getCurrentPaymentAttempt(chatId: string): Observable<PaymentSession> {
    return this.withTransientRetry(
      this.http.get<PaymentSession>(
        `${settings.api_url}/my/chats/${encodeURIComponent(chatId)}/payment-attempts/current`,
      ),
    );
  }

  // Fail a stalled request after REQUEST_TIMEOUT_MS, then retry only transient errors with
  // exponential backoff (500ms, 1000ms). Non-transient errors propagate on the first failure.
  private withTransientRetry<T>(source: Observable<T>): Observable<T> {
    return source.pipe(
      timeout(REQUEST_TIMEOUT_MS),
      retry({
        count: MAX_RETRIES,
        delay: (error, retryCount) => {
          if (!isTransientError(error)) {
            return throwError(() => error);
          }
          return timer(RETRY_BASE_DELAY_MS * 2 ** (retryCount - 1));
        },
      }),
    );
  }
}
