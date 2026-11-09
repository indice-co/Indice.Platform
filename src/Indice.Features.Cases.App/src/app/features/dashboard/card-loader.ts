import { DestroyRef, signal } from '@angular/core';
import { Observable, Subscription } from 'rxjs';

export type CardState<T> =
  | { status: 'loading' }
  | { status: 'ready'; value: T }
  | { status: 'error' }
  /** The user is not allowed to see this data: the card is not rendered at all. */
  | { status: 'hidden' };

/**
 * Loads the data of one dashboard card independently, so a failing call only affects its own card.
 * 401 / 403 responses hide the card instead of showing an error.
 */
export class CardLoader<T> {
  public readonly state = signal<CardState<T>>({ status: 'loading' });
  private _subscription?: Subscription;

  constructor(private readonly _factory: () => Observable<T>, destroyRef: DestroyRef) {
    destroyRef.onDestroy(() => this._subscription?.unsubscribe());
    this.reload();
  }

  public reload(): void {
    this._subscription?.unsubscribe();
    this.state.set({ status: 'loading' });
    this._subscription = this._factory().subscribe({
      next: value => this.state.set({ status: 'ready', value }),
      error: err => this.state.set({ status: err?.status === 401 || err?.status === 403 ? 'hidden' : 'error' })
    });
  }
}
