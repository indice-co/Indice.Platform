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
  caseId?: string;
  conversationId?: string | null;
  /** The Stripe PaymentIntent client_secret the card is confirmed against. */
  paymentSessionSecret?: string | null;
  paymentAuthorizationStatus?: PaymentAuthStatus | null;
  /** The pre-auth (hold) amount, in major currency units. */
  amount?: number | null;
  currency?: string | null;
}

@Injectable({ providedIn: 'root' })
export class PaymentService {
  private readonly http = inject(HttpClient);

  /** The case's current payment attempt — polled until the webhook lands. */
  getCurrentPaymentAttempt(caseId: string): Observable<PaymentSession> {
    return this.withTransientRetry(
      this.http.get<PaymentSession>(
        `${settings.api_url}/my/cases/${encodeURIComponent(caseId)}/payment-attempts/current`,
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
