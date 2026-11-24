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
import type {
  Appearance,
  Stripe,
  StripeElementLocale,
  StripeElements,
  StripePaymentElement,
} from '@stripe/stripe-js';

import { settings } from '../../../core/models/settings';
import { PaymentService, PaymentSession } from '../../../core/services/payment.service';
import { StripeService } from '../../../core/services/stripe.service';
import { StripePayment } from './part-contracts';

type PayStatus = 'initializing' | 'ready' | 'confirming' | 'verifying' | 'completed' | 'error';

const POLL_INTERVAL_MS = 2000;
const POLL_MAX_ATTEMPTS = 15;

/**
 * Inline Stripe Payment Element (card, Apple Pay, Google Pay) rendered in the chat for `application/vnd.indice.payment.stripe+json`.
 * The workflow creates the case's payment attempt server-side and ships it, with the publishable key, in the part,
 * so the card form mounts straight away. After confirming the payment the component polls the
 * case's current attempt (webhook-driven) until it is `Succeeded`, then emits `completed` to unfreeze the chat.
 * When the attempt is `Failed`/`Cancelled` (also after a reload), Try again asks the server for a replacement attempt
 * owned by the same user; the amount is copied server-side.
 */
@Component({
  selector: 'app-chat-stripe-payment',
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
            @if (holdAmountText(); as amount) {
              <p class="text-sm text-base-content/70">
                Amount to pay: <strong>{{ amount }}</strong>
              </p>
            }
            <div #cardElement class="block w-full"></div>
            @if (cardError()) {
              <p class="text-sm text-error">{{ cardError() }}</p>
            }
            <button
              type="button"
              class="btn btn-primary"
              [disabled]="disabled() || status() !== 'ready' || !elementComplete()"
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
  protected readonly elementComplete = signal(false);
  private readonly clientSecret = signal<string | null>(null);
  protected readonly holdAmountText = computed(() => {
    const session = this.payment()?.session;
    const amount = session?.amount;
    return amount != null && amount > 0
      ? new Intl.NumberFormat(undefined, { style: 'currency', currency: session?.currency ?? 'EUR' }).format(amount)
      : null;
  });

  private stripe: Stripe | null = null;
  private elements: StripeElements | null = null;
  private card: StripePaymentElement | null = null;
  private pollTimer: ReturnType<typeof setTimeout> | null = null;
  private destroyed = false;
  private started = false;
  private retrying = false;

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

    // The part payload is a snapshot from when the workflow emitted it (status Created). After a reload the case
    // may already be paid, so always read the current server state first (synced with Stripe server-side).
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
    this.card?.destroy();
    this.card = null;
    this.elements = null;
    this.elementComplete.set(false);
    this.status.set('initializing');
    this.errorMessage.set(null);
    this.cardError.set('');
    this.clientSecret.set(null);
    this.payments.retryPaymentAttempt(caseId).subscribe({
      next: (session) => {
        this.retrying = false;
        this.apply(session);
      },
      error: () => {
        this.retrying = false;
        this.fail(null);
      },
    });
  }

  protected async pay(): Promise<void> {
    const secret = this.clientSecret();
    if (this.disabled() || !this.stripe || !this.elements || !secret || this.status() !== 'ready') {
      return;
    }
    this.status.set('confirming');
    this.cardError.set('');
    const { error: submitError } = await this.elements.submit();
    if (submitError) {
      this.cardError.set(submitError.message ?? '');
      this.status.set('ready');
      return;
    }
    // Redirect methods are disabled server-side, so confirmation always resolves in place.
    const { error, paymentIntent } = await this.stripe.confirmPayment({
      elements: this.elements,
      clientSecret: secret,
      redirect: 'if_required',
      confirmParams: { return_url: window.location.href },
    });
    if (error) {
      this.cardError.set(error.message ?? '');
      this.status.set('ready');
      return;
    }
    if (paymentIntent?.status === 'succeeded' || paymentIntent?.status === 'processing') {
      // Do not trust the client result alone: wait for the webhook-driven server state.
      this.status.set('verifying');
      this.poll(0);
      return;
    }
    this.status.set('ready');
  }

  // Reads the case's current attempt (initial load, reload). Try again replaces a failed/cancelled attempt via the retry endpoint.
  private refresh(): void {
    const caseId = this.payment()?.caseId;
    if (!caseId) {
      this.fail(null);
      return;
    }
    this.payments.getCurrentPaymentAttempt(caseId).subscribe({
      next: (session) => this.apply(session),
      error: () => this.fail(null),
    });
  }

  // Only a server-reported Succeeded completes the payment; Failed/Cancelled keep Try again available.
  private apply(session: PaymentSession | null | undefined): void {
    const state = session?.paymentAuthorizationStatus;
    if (state === 'Succeeded') {
      this.complete();
    } else if (state === 'Failed') {
      this.fail('Your payment was declined. Please try again.');
    } else if (state === 'Cancelled') {
      this.fail('The payment was cancelled. Please try again.');
    } else if (!this.disabled() && session?.paymentSessionSecret) {
      this.status.set('ready');
      this.clientSecret.set(session.paymentSessionSecret);
    } else {
      this.fail('The payment is no longer available.');
    }
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
    const clientSecret = this.clientSecret();
    if (!clientSecret) return;
    this.elements = this.stripe.elements({ clientSecret, locale: this.locale, appearance: this.appearance(host) });
    const paymentMethodOrder = this.payment()?.paymentMethodOrder ?? undefined;
    this.card = this.elements.create('payment', {
      layout: 'tabs',
      paymentMethodOrder,
      wallets: { applePay: 'auto', googlePay: 'auto' },
    });
    this.card.mount(host);
    this.card.on('change', (event) => {
      this.elementComplete.set(event.complete);
      this.cardError.set('');
    });
  }

  // Maps the active DaisyUI theme onto Stripe's appearance so the iframe matches the chat.
  private appearance(host: HTMLElement): Appearance {
    const css = getComputedStyle(host);
    const color = (name: string) => css.getPropertyValue(name).trim() || undefined;
    return {
      theme: 'stripe',
      variables: {
        colorPrimary: color('--color-primary'),
        colorBackground: color('--color-base-100'),
        colorText: color('--color-base-content'),
        colorDanger: color('--color-error'),
        borderRadius: color('--radius-field'),
      },
    };
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
