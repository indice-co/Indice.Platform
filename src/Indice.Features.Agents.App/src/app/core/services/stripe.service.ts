import { Injectable } from '@angular/core';
import { loadStripe, Stripe } from '@stripe/stripe-js';

/**
 * Loads Stripe.js with the publishable key shipped in the chat payment part. The result is memoized so the Stripe
 * instance and its remote script are loaded once per app session; a failure clears the memo so a later call retries.
 */
@Injectable({ providedIn: 'root' })
export class StripeService {
  private stripePromise: Promise<Stripe | null> | null = null;

  getStripe(publishableKey?: string | null): Promise<Stripe | null> {
    if (!publishableKey) {
      console.error('Stripe publishable key not provided');
      return Promise.resolve(null);
    }
    if (!this.stripePromise) {
      this.stripePromise = loadStripe(publishableKey).catch((error) => {
        console.error('Failed to initialize Stripe', error);
        this.stripePromise = null;
        return null;
      });
    }
    return this.stripePromise;
  }
}
