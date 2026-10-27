import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { PaymentService, PaymentSession, VivaConfiguration } from '../../../core/services/payment.service';
import { VivaPayment } from './part-contracts';

type PayStatus = 'initializing' | 'ready' | 'confirming' | 'verifying' | 'completed' | 'error';

interface VivaCards {
  setup(options: { baseURL: string; authToken: string; installments?: (response: unknown) => void }): void;
  requestToken(options: { amount: number; authenticateCardholder?: boolean; [key: string]: unknown }): Promise<{ chargeToken?: string }>;
}

declare global {
  interface Window {
    VivaPayments?: { cards: VivaCards };
  }
}

const POLL_INTERVAL_MS = 2000;
const POLL_MAX_ATTEMPTS = 15;
const JQUERY_URL = 'https://code.jquery.com/jquery-3.7.1.min.js';
const scriptPromises = new Map<string, Promise<void>>();

function loadScript(url: string): Promise<void> {
  let promise = scriptPromises.get(url);
  if (!promise) {
    promise = new Promise<void>((resolve, reject) => {
      const script = document.createElement('script');
      script.src = url;
      script.async = true;
      script.onload = () => resolve();
      script.onerror = () => {
        scriptPromises.delete(url);
        reject(new Error(`Failed to load ${url}`));
      };
      document.head.appendChild(script);
    });
    scriptPromises.set(url, promise);
  }
  return promise;
}

/**
 * Inline Viva Wallet Native Checkout card form rendered in the chat for `application/vnd.indice.payment.viva+json`.
 * The card fields are plain inputs tagged with `data-vp`; Viva's script tokenizes them (handling 3-D Secure in the
 * `#viva3ds` container), the token is posted to the server which charges directly, and the component then polls the
 * case's current attempt until the server reports `Succeeded`.
 */
@Component({
  selector: 'app-chat-viva-payment',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="card w-full max-w-md border border-base-300 bg-base-100 shadow-sm">
      <div class="card-body gap-3 p-4">
        <h3 class="card-title text-base">Payment</h3>

        @switch (status()) {
          @case ('initializing') {
            <div class="flex items-center gap-2 text-sm text-base-content/70">
              <span class="loading loading-spinner loading-sm"></span> Preparing payment…
            </div>
          }
          @case ('completed') {
            Payment completed. Thank you!
          }
          @case ('error') {
            <div role="alert" class="alert alert-error text-sm">{{ errorMessage() }}</div>
            @if (!disabled()) {
              <button type="button" class="btn btn-outline btn-sm" (click)="retry()">Try again</button>
            }
          }
          @default {
            @if (amountText(); as amount) {
              <p class="text-sm text-base-content/70">
                Amount to pay: <strong>{{ amount }}</strong>
              </p>
            }
            <form #cardForm class="flex flex-col gap-2" (submit)="$event.preventDefault(); pay()">
              <input class="input input-bordered w-full" type="text" data-vp="cardholder" placeholder="Cardholder name" autocomplete="cc-name" (input)="cardError.set('')" />
              <input class="input input-bordered w-full" type="text" data-vp="cardnumber" placeholder="Card number" inputmode="numeric" autocomplete="cc-number" (input)="cardError.set('')" />
              <div class="flex gap-2">
                <input class="input input-bordered w-full" type="text" data-vp="month" placeholder="MM" inputmode="numeric" maxlength="2" autocomplete="cc-exp-month" />
                <input class="input input-bordered w-full" type="text" data-vp="year" placeholder="YY" inputmode="numeric" maxlength="4" autocomplete="cc-exp-year" />
                <input class="input input-bordered w-full" type="text" data-vp="cvv" placeholder="CVV" inputmode="numeric" maxlength="4" autocomplete="cc-csc" />
              </div>
            </form>
            <div id="viva3ds"></div>
            @if (cardError()) {
              <p class="text-sm text-error">{{ cardError() }}</p>
            }
            <button type="button" class="btn btn-primary" [disabled]="disabled() || status() !== 'ready'" (click)="pay()">
              @if (status() === 'confirming' || status() === 'verifying') {
                <span class="loading loading-spinner loading-sm"></span>
                {{ status() === 'verifying' ? 'Verifying payment…' : 'Processing…' }}
              } @else {
                Pay
              }
            </button>
          }
        }
      </div>
    </div>
  `,
})
export class ChatVivaPaymentComponent {
  /** The payment part payload: case id and the server-created attempt. */
  readonly payment = input<VivaPayment | null>(null);
  /** Disabled when this part is not on the latest turn (spent) or while streaming. */
  readonly disabled = input(false);
  /** Emits once the server reports the payment as charged. */
  readonly completed = output<void>();

  private readonly payments = inject(PaymentService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly cardForm = viewChild<ElementRef<HTMLFormElement>>('cardForm');

  protected readonly status = signal<PayStatus>('initializing');
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly cardError = signal('');
  private readonly session = signal<PaymentSession | null>(null);
  protected readonly amountText = computed(() => {
    const session = this.session() ?? this.payment()?.session;
    const amount = session?.amount;
    return amount != null && amount > 0
      ? new Intl.NumberFormat(undefined, { style: 'currency', currency: session?.currency ?? 'EUR' }).format(amount)
      : null;
  });

  private configuration: VivaConfiguration | null = null;
  private pollTimer: ReturnType<typeof setTimeout> | null = null;
  private destroyed = false;
  private started = false;
  private retrying = false;

  constructor() {
    this.destroyRef.onDestroy(() => {
      this.destroyed = true;
      if (this.pollTimer) clearTimeout(this.pollTimer);
    });

    // Always read the current server state first: after a reload the case may already be paid.
    effect(() => {
      const payment = this.payment();
      if (!payment || this.started) return;
      this.started = true;
      untracked(() => this.refresh());
    });
  }

  protected retry(): void {
    const caseId = this.payment()?.caseId;
    if (this.retrying || this.disabled() || !caseId) return;
    this.retrying = true;
    this.status.set('initializing');
    this.errorMessage.set(null);
    this.cardError.set('');
    this.payments.retryPaymentAttempt(caseId).subscribe({
      next: (session) => {
        this.retrying = false;
        void this.apply(session);
      },
      error: () => {
        this.retrying = false;
        this.fail(null);
      },
    });
  }

  protected async pay(): Promise<void> {
    const caseId = this.payment()?.caseId;
    const amount = this.session()?.amount;
    if (this.disabled() || !caseId || amount == null || this.status() !== 'ready' || !this.cardForm()) {
      return;
    }
    this.status.set('confirming');
    this.cardError.set('');
    let chargeToken: string | undefined;
    try {
      // The access token is short-lived: refresh the setup right before tokenizing.
      await this.setup(caseId, true);
      const response = await window.VivaPayments!.cards.requestToken({
        amount: Math.round(amount * 100),
        authenticateCardholder: true,
      });
      chargeToken = response?.chargeToken;
    } catch (error) {
      this.cardError.set(this.describe(error) ?? 'Please check your card details.');
      this.status.set('ready');
      return;
    }
    if (!chargeToken) {
      this.cardError.set('Please check your card details.');
      this.status.set('ready');
      return;
    }
    try {
      const session = await firstValueFrom(this.payments.chargeViva(caseId, chargeToken));
      this.session.set(session);
      const state = session?.paymentAuthorizationStatus;
      if (state === 'Succeeded') {
        this.complete();
      } else if (state === 'Failed' || state === 'Cancelled') {
        this.fail('Your payment was declined. Please try again.');
      } else {
        // Do not trust the client result alone: wait for the server state.
        this.status.set('verifying');
        this.poll(0);
      }
    } catch {
      this.status.set('verifying');
      this.poll(0);
    }
  }

  private refresh(): void {
    const caseId = this.payment()?.caseId;
    if (!caseId) {
      this.fail(null);
      return;
    }
    this.payments.getCurrentPaymentAttempt(caseId).subscribe({
      next: (session) => void this.apply(session),
      error: () => this.fail(null),
    });
  }

  private async apply(session: PaymentSession | null | undefined): Promise<void> {
    this.session.set(session ?? null);
    const state = session?.paymentAuthorizationStatus;
    if (state === 'Succeeded') {
      this.complete();
    } else if (state === 'Failed') {
      this.fail('Your payment was declined. Please try again.');
    } else if (state === 'Cancelled') {
      this.fail('The payment was cancelled. Please try again.');
    } else if (!this.disabled() && state === 'Created' && session?.caseId) {
      try {
        // Viva binds to the `data-vp` inputs during setup, so the form must be in the DOM first.
        this.status.set('ready');
        await new Promise((resolve) => setTimeout(resolve));
        await this.setup(session.caseId, false);
      } catch (error) {
        console.error('Viva Native Checkout setup failed', error);
        this.fail('The payment provider is currently unavailable. Please try again later.');
      }
    } else {
      this.fail('The payment is no longer available.');
    }
  }

  private async setup(caseId: string, refresh: boolean): Promise<void> {
    if (!this.configuration || refresh) {
      this.configuration = await firstValueFrom(this.payments.getVivaConfiguration(caseId));
    }
    // Native Checkout v2 depends on jQuery being present on the page.
    if (!(window as unknown as { jQuery?: unknown }).jQuery) {
      await loadScript(JQUERY_URL);
    }
    await loadScript(this.configuration.scriptUrl);
    if (!window.VivaPayments) {
      throw new Error('Viva Native Checkout is unavailable.');
    }
    window.VivaPayments.cards.setup({ baseURL: this.configuration.baseUrl, authToken: this.configuration.authToken });
  }

  private poll(attempt: number): void {
    const caseId = this.payment()?.caseId;
    if (this.destroyed || !caseId) return;
    this.payments.getCurrentPaymentAttempt(caseId).subscribe({
      next: (session) => {
        const state = session?.paymentAuthorizationStatus;
        if (state === 'Succeeded') {
          this.complete();
        } else if (state === 'Failed' || state === 'Cancelled') {
          this.fail('The payment was declined. Please try again.');
        } else if (attempt + 1 >= POLL_MAX_ATTEMPTS) {
          this.fail('We could not verify the payment yet. Please try again shortly.');
        } else {
          this.pollTimer = setTimeout(() => this.poll(attempt + 1), POLL_INTERVAL_MS);
        }
      },
      error: () => this.fail(null),
    });
  }

  private describe(error: unknown): string | null {
    if (error && typeof error === 'object') {
      const value = error as { Error?: { ErrorText?: string }; message?: string };
      return value.Error?.ErrorText ?? value.message ?? null;
    }
    return null;
  }

  private complete(): void {
    this.status.set('completed');
    this.completed.emit();
  }

  private fail(message: string | null): void {
    this.errorMessage.set(message ?? 'The payment could not be completed.');
    this.status.set('error');
  }
}
