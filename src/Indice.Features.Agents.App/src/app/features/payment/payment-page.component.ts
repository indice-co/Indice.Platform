import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  computed,
  effect,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import type { Stripe, StripeCardElement, StripeElements, StripeElementLocale } from '@stripe/stripe-js';

import { settings } from '../../core/models/settings';
import { PaymentService, isUnauthenticated } from '../../core/services/payment.service';
import { StripeService } from '../../core/services/stripe.service';

type PayStatus = 'initializing' | 'ready' | 'confirming' | 'redirecting' | 'error';

const MESSAGES = {
  noSecret: 'The payment could not be initialized. Please try again.',
  stripeUnavailable: 'The payment provider is currently unavailable. Please try again later.',
  unauthenticated: 'Your session has expired. Please sign in again.',
  failed: 'The payment could not be completed.',
} as const;

/**
 * Payment step for a chat. Flow:
 *  1. `POST my/chats/{chatId}/payment-attempts` → returns the session with a Stripe
 *     `paymentSessionSecret` (the PaymentIntent client_secret). If the session is
 *     already authorized (`Succeeded`) we skip straight to the status page.
 *  2. Mount the Stripe card element (key + script via StripeService).
 *  3. On pay, `confirmCardPayment` confirms the manual-capture pre-auth (handling
 *     3DS inline). Success (`requires_capture`/`succeeded`) → `payment/status`,
 *     where the webhook-driven server state is observed.
 */
@Component({
  selector: 'app-payment-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './payment-page.component.html',
})
export class PaymentPageComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly payments = inject(PaymentService);
  private readonly stripeService = inject(StripeService);
  private readonly destroyRef = inject(DestroyRef);

  // The host div only exists once `status` reaches 'ready', so this must be a signal query:
  // the effect below mounts the card the render *after* the element appears.
  private readonly cardHost = viewChild<ElementRef<HTMLDivElement>>('cardElement');

  private readonly chatId = this.route.snapshot.paramMap.get('chatId') ?? '';
  private readonly locale = (settings.culture || 'auto') as StripeElementLocale;

  protected readonly status = signal<PayStatus>('initializing');
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly cardError = signal<string>('');
  private readonly clientSecret = signal<string | null>(null);

  // Pre-auth (hold) amount from the payment-attempt response, used in the hold disclaimer.
  private readonly holdAmount = signal<number | null>(null);
  protected readonly holdAmountText = computed(() => {
    const amount = this.holdAmount();
    return amount != null && amount > 0 ? `${amount.toFixed(2)}€` : null;
  });

  private stripe: Stripe | null = null;
  private elements: StripeElements | null = null;
  private card: StripeCardElement | null = null;

  constructor() {
    this.destroyRef.onDestroy(() => this.card?.destroy());

    // Mount as soon as both the client_secret and the host element exist (in whichever order).
    effect(() => {
      const host = this.cardHost();
      const secret = this.clientSecret();
      if (host && secret) {
        void this.mountCard(host.nativeElement);
      }
    });

    this.initAttempt();
  }

  retry(): void {
    this.status.set('initializing');
    this.errorMessage.set(null);
    this.cardError.set('');
    this.clientSecret.set(null);
    this.initAttempt();
  }

  // Step 3: confirm the manual-capture pre-auth. Stripe handles any 3DS inline; on success the
  // PaymentIntent is requires_capture (or succeeded) and the webhook drives the server side.
  async pay(): Promise<void> {
    const secret = this.clientSecret();
    if (!this.stripe || !this.card || !secret || this.status() === 'confirming') {
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
      this.goToStatus();
      return;
    }

    // Unexpected non-terminal state (e.g. still requires_action) — let the user retry.
    this.status.set('ready');
  }

  // Step 1: create/reuse the payment attempt and capture its client_secret.
  private initAttempt(): void {
    if (!this.chatId) {
      this.fail(MESSAGES.noSecret);
      return;
    }
    this.payments.createPaymentAttempt(this.chatId).subscribe({
      next: (session) => {
        // Already authorized (e.g. returning to this page): nothing to confirm.
        if (session.paymentAuthorizationStatus === 'Succeeded') {
          this.goToStatus();
          return;
        }
        if (!session.paymentSessionSecret) {
          this.fail(MESSAGES.noSecret);
          return;
        }
        // Render the card section first; the effect mounts once the host exists.
        this.holdAmount.set(session.firstCaptureAmount ?? null);
        this.status.set('ready');
        this.clientSecret.set(session.paymentSessionSecret);
      },
      error: (err) => this.fail(isUnauthenticated(err) ? MESSAGES.unauthenticated : null),
    });
  }

  // Step 2: load Stripe (key + script) lazily and mount the card element.
  private async mountCard(host: HTMLDivElement): Promise<void> {
    if (this.card) return;

    if (!this.stripe) {
      this.stripe = await this.stripeService.getStripe();
      if (!this.stripe) {
        this.fail(MESSAGES.stripeUnavailable);
        return;
      }
    }
    // Guard against a second effect run racing the await above.
    if (this.card) return;

    this.elements = this.stripe.elements({ locale: this.locale });
    this.card = this.elements.create('card', { hidePostalCode: true });
    this.card.mount(host);
    this.card.on('change', (event) =>
      this.cardError.set(event.error ? (event.error.message ?? '') : ''),
    );
  }

  private goToStatus(): void {
    this.status.set('redirecting');
    this.router.navigate(['/payment', this.chatId, 'status'], { queryParams: { justPaid: 1 } });
  }

  private fail(message: string | null): void {
    this.errorMessage.set(message ?? MESSAGES.failed);
    this.status.set('error');
  }
}
