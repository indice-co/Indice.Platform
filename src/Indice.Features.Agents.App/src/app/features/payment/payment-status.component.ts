import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Subscription, switchMap, takeWhile, timer } from 'rxjs';

import { PaymentAuthStatus, PaymentService } from '../../core/services/payment.service';

const POLL_INTERVAL_MS = 2000;
const MAX_POLLS = 30;

/**
 * Confirmation page reached after the card is confirmed. The client result alone is not trusted:
 * the server status only flips to `Succeeded` once the Stripe webhook lands (which may be late),
 * so this page polls the current payment attempt until it reaches a terminal status.
 */
@Component({
  selector: 'app-payment-status',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  template: `
    <main class="mx-auto flex w-full max-w-md flex-col items-center gap-3 px-4 py-12 text-center">
      @switch (status()) {
        @case ('Succeeded') {
          <div class="text-4xl text-success" aria-hidden="true">✓</div>
          <p class="text-lg font-semibold">Payment authorized</p>
          <a class="btn btn-primary btn-sm" [routerLink]="['/']" [queryParams]="{ chatId }">Back to chat</a>
        }
        @case ('Failed') {
          <div class="text-4xl font-bold text-error" aria-hidden="true">!</div>
          <p class="text-lg font-semibold">The payment failed</p>
          <a class="btn btn-outline btn-sm" [routerLink]="['/payment', chatId]">Try again</a>
        }
        @case ('Cancelled') {
          <p class="text-lg font-semibold">The payment was cancelled</p>
          <a class="btn btn-outline btn-sm" [routerLink]="['/payment', chatId]">Try again</a>
        }
        @default {
          @if (timedOut()) {
            <p class="text-sm text-base-content/70">
              We are still waiting for the payment confirmation. Please check again in a moment.
            </p>
            <button type="button" class="btn btn-outline btn-sm" (click)="poll()">Check again</button>
          } @else {
            <span class="loading loading-spinner loading-lg" aria-hidden="true"></span>
            <p class="text-base-content/70">Confirming payment…</p>
          }
        }
      }
    </main>
  `,
})
export class PaymentStatusComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly payments = inject(PaymentService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly chatId = this.route.snapshot.paramMap.get('chatId') ?? '';
  protected readonly status = signal<PaymentAuthStatus | null>(null);
  protected readonly timedOut = signal(false);
  private subscription: Subscription | null = null;

  constructor() {
    this.poll();
  }

  protected poll(): void {
    this.subscription?.unsubscribe();
    this.timedOut.set(false);
    let polls = 0;
    this.subscription = timer(0, POLL_INTERVAL_MS)
      .pipe(
        takeWhile(() => polls++ < MAX_POLLS),
        switchMap(() => this.payments.getCurrentPaymentAttempt(this.chatId)),
        takeWhile((session) => session.paymentAuthorizationStatus === 'Created', true),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (session) => this.status.set(session.paymentAuthorizationStatus ?? null),
        error: () => this.timedOut.set(true),
        complete: () => {
          if (this.status() === 'Created' || this.status() === null) {
            this.timedOut.set(true);
          }
        },
      });
  }
}
