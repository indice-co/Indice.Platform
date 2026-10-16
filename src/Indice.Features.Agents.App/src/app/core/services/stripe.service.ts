import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { loadStripe, Stripe } from '@stripe/stripe-js';
import { firstValueFrom } from 'rxjs';

import { settings } from '../models/settings';

interface StripePublishableKeyResponse {
  publishableKey?: string;
}

/**
 * Loads Stripe.js: fetches the publishable key from the API, then `loadStripe` (which injects
 * the js.stripe.com script). The result is memoized so the Stripe instance and its remote
 * script are loaded once per app session; a failure clears the memo so a later call retries.
 *
 * The key endpoint is anonymous, so this works before the user is authenticated.
 */
@Injectable({ providedIn: 'root' })
export class StripeService {
  private readonly http = inject(HttpClient);
  private stripePromise: Promise<Stripe | null> | null = null;

  getStripe(): Promise<Stripe | null> {
    if (!this.stripePromise) {
      this.stripePromise = this.initStripe();
    }
    return this.stripePromise;
  }

  private async initStripe(): Promise<Stripe | null> {
    try {
      const response = await firstValueFrom(
        this.http.get<StripePublishableKeyResponse>(`${settings.api_url}/payment-provider/public-key`),
      );
      if (!response?.publishableKey) {
        console.error('Stripe publishable key not configured');
        // Drop the memoized failure so a later retry can attempt the fetch again.
        this.stripePromise = null;
        return null;
      }
      return loadStripe(response.publishableKey);
    } catch (error) {
      console.error('Failed to initialize Stripe', error);
      this.stripePromise = null;
      return null;
    }
  }
}
