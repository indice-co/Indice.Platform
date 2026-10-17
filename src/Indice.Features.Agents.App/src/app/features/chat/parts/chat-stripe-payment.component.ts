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
import type { Stripe, StripeCardElement, StripeElementLocale } from '@stripe/stripe-js';

import { settings } from '../../../core/models/settings';
import { PaymentService } from '../../../core/services/payment.service';
import { StripeService } from '../../../core/services/stripe.service';
import { StripePayment } from './part-contracts';

type PayStatus = 'initializing' | 'ready' | 'confirming' | 'verifying' | 'completed' | 'error';

const POLL_INTERVAL_MS = 2000;
const POLL_MAX_ATTEMPTS = 15;

/**
 * Inline Stripe card payment rendered in the chat for `application/vnd.indice.payment.stripe+json`.
 * The workflow creates the case's payment attempt server-side and ships it, with the publishable key, in the part,
 * so the card form mounts straight away. After confirming the manual-capture pre-auth the component polls the
 * case's current attempt (webhook-driven) until it is `Succeeded`, then emits `completed` to unfreeze the chat.
 */
@Component({
  selector: 'app-chat-stripe-payment',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="card w-full max-w-md border border-base-300 bg-base-100 shadow-sm">
      <div class="card-body gap-3 p-4">
        <h3 class="card-title text-base">Card payment</h3>

        @switch (status()) {
          @case ('initializing') {
            <div class="flex items-center gap-2 text-sm text-base-content/70">
              <span class="loading loading-spinner loading-sm"></span> Preparing payment…
            </div>
          }
          @case ('completed') {
            <div role="alert" class="alert alert-success text-sm">Payment authorized. Thank you!</div>
          }
          @case ('error') {
            <div role="alert" class="alert alert-error text-sm">{{ errorMessage() }}</div>
            @if (!disabled()) {
              <button type="button" class="btn btn-outline btn-sm" (click)="retry()">Try again</button>
            }
          }
          @default {
            @if (holdAmountText(); as amount) {
              <p class="text-sm text-base-content/70">
                A temporary hold of <strong>{{ amount }}</strong> will be placed on your card. Only the final amount is charged.
              </p>
            }
            <div
              #cardElement
              class="block w-full rounded-field border border-base-300 bg-base-100 px-3 py-3 focus-within:border-primary"
            ></div>
            @if (cardError()) {
              <p class="text-sm text-error">{{ cardError() }}</p>
            }
            <button
              type="button"
              class="btn btn-primary"
              [disabled]="disabled() || status() !== 'ready'"
              (click)="pay()"
            >
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
export class ChatStripePaymentComponent {
  /** The payment part payload: case id, publishable key and the server-created attempt. */
  readonly payment = input<StripePayment | null>(null);
  /** Disabled when this part is not on the latest turn (spent) or while streaming. */
  readonly disabled = input(false);
  /** Emits once the server reports the payment as authorized. */
  readonly completed = output<void>();

  private readonly payments = inject(PaymentService);
  private readonly stripeService = inject(StripeService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly cardHost = viewChild<ElementRef<HTMLDivElement>>('cardElement');
  private readonly locale = (settings.culture || 'auto') as StripeElementLocale;

  protected readonly status = signal<PayStatus>('initializing');
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly cardError = signal('');
  private readonly clientSecret = signal<string | null>(null);
  protected readonly holdAmountText = computed(() => {
    const amount = this.payment()?.session?.firstCaptureAmount;
    return amount != null && amount > 0 ? `${amount.toFixed(2)}€` : null;
  });

  private stripe: Stripe | null = null;
  private card: StripeCardElement | null = null;
  private pollTimer: ReturnType<typeof setTimeout> | null = null;
  private destroyed = false;
  private started = false;

  constructor() {
    this.destroyRef.onDestroy(() => {
      this.destroyed = true;
      this.card?.destroy();
      if (this.pollTimer) clearTimeout(this.pollTimer);
    });

    effect(() => {
      const host = this.cardHost();
      const secret = this.clientSecret();
      if (host && secret) {
        void this.mountCard(host.nativeElement);
      }
    });

    // The interactive instance (latest turn, not streaming) uses the shipped attempt directly. Spent turns re-read the
    // server state once so a reloaded, already-paid case shows as completed rather than a stale form.
    effect(() => {
      const payment = this.payment();
      if (!payment || this.started) return;
      this.started = true;
      untracked(() => (this.disabled() ? this.refresh() : this.init(payment)));
    });
  }

  protected retry(): void {
    this.card?.destroy();
    this.card = null;
    this.status.set('initializing');
    this.errorMessage.set(null);
    this.cardError.set('');
    this.clientSecret.set(null);
    this.refresh();
  }

  protected async pay(): Promise<void> {
    const secret = this.clientSecret();
    if (this.disabled() || !this.stripe || !this.card || !secret || this.status() !== 'ready') {
      return;
    }
    this.status.set('confirming');
    this.cardError.set('');
    const { error, paymentIntent } = await this.stripe.confirmCardPayment(secret, {
      payment_method: { card: this.card },
    });
    if (error) {
      this.cardError.set(error.message ?? '');
      this.status.set('ready');
      return;
    }
    if (paymentIntent?.status === 'requires_capture' || paymentIntent?.status === 'succeeded') {
      // Do not trust the client result alone: wait for the webhook-driven server state.
      this.status.set('verifying');
      this.poll(0);
      return;
    }
    this.status.set('ready');
  }

  // Uses the attempt the workflow created server-side; no API round-trip needed.
  private init(payment: StripePayment): void {
    const session = payment.session;
    if (session?.paymentAuthorizationStatus === 'Succeeded') {
      this.complete();
      return;
    }
    if (!session?.paymentSessionSecret) {
      this.fail('The payment could not be initialized. Please try again.');
      return;
    }
    this.status.set('ready');
    this.clientSecret.set(session.paymentSessionSecret);
  }

  // Re-reads the case's attempt (retry / historical turn); new attempts are only created by the workflow.
  private refresh(): void {
    const caseId = this.payment()?.caseId;
    if (!caseId) {
      this.fail(null);
      return;
    }
    this.payments.getCurrentPaymentAttempt(caseId).subscribe({
      next: (session) => {
        if (session?.paymentAuthorizationStatus === 'Succeeded') {
          this.complete();
        } else if (!this.disabled() && session?.paymentSessionSecret) {
          this.status.set('ready');
          this.clientSecret.set(session.paymentSessionSecret);
        } else {
          this.fail('The payment is no longer available.');
        }
      },
      error: () => this.fail(null),
    });
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

  private async mountCard(host: HTMLDivElement): Promise<void> {
    if (this.card) return;
    if (!this.stripe) {
      this.stripe = await this.stripeService.getStripe(this.payment()?.publishableKey);
      if (!this.stripe) {
        this.fail('The payment provider is currently unavailable. Please try again later.');
        return;
      }
    }
    if (this.card) return;
    this.card = this.stripe.elements({ locale: this.locale }).create('card', { hidePostalCode: true });
    this.card.mount(host);
    this.card.on('change', (event) => this.cardError.set(event.error ? (event.error.message ?? '') : ''));
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
