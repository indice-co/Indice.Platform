import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subscription } from 'rxjs';

import { ConversationListItem, DexApiService } from './dex-api.service';

/** How many sessions the rail fetches in one page — a generous single page covers the history. */
const PAGE_SIZE = 100;

/**
 * The caller's conversation list and which one is open, shared by the app rail (rendered by the
 * shell, on every route) and the chat page (which renders the thread for `activeId`).
 *
 * The store only *states* which conversation is active; loading the thread for it is the chat
 * page's job. `select` and `adopt` therefore do the same thing — they differ in intent, and the
 * chat page tells them apart by remembering the last id it actually fetched.
 */
@Injectable({ providedIn: 'root' })
export class ConversationsStore {
  private readonly dex = inject(DexApiService);
  private readonly destroyRef = inject(DestroyRef);

  /** The caller's sessions, most-recently-active first (server order). */
  readonly sessions = signal<ConversationListItem[]>([]);
  /** True while the list is being (re)fetched — the rail renders skeletons. */
  readonly loading = signal(false);
  /** The open conversation, or `null` for an unsent new chat. */
  readonly activeId = signal<string | null>(null);
  /** Last list-level failure (a delete that the server rejected); the rail shows and clears it. */
  readonly error = signal<string | null>(null);

  /** The open conversation's list entry, when it is present in the fetched page. */
  readonly active = computed(() => this.sessions().find((s) => s.id === this.activeId()) ?? null);

  /** The list fetch in flight, if any — at most one is ever allowed to land. */
  private refreshSub?: Subscription;

  /** (Re)fetch the session list — called on startup and after a turn settles. */
  refresh(): void {
    // Supersede anything still on the wire: a late response must not overwrite a newer one.
    this.cancelRefresh();
    this.loading.set(true);
    this.refreshSub = this.dex
      .list(1, PAGE_SIZE, null, null)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.sessions.set(result.items ?? []);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }

  /** Open an existing conversation — the chat page loads its thread. */
  select(id: string): void {
    this.activeId.set(id);
  }

  /** Start an unsent chat: no conversation is open until the first turn creates one. */
  startNew(): void {
    this.activeId.set(null);
  }

  /** Take ownership of a conversation the stream just created — no thread fetch follows. */
  adopt(id: string): void {
    this.activeId.set(id);
  }

  /** Delete a conversation, dropping the row immediately and restoring it if the server refuses. */
  remove(id: string): void {
    // A list response already on the wire predates this deletion and would resurrect the row.
    this.cancelRefresh();
    const previous = this.sessions();
    this.error.set(null);
    this.sessions.update((list) => list.filter((s) => s.id !== id));
    if (this.activeId() === id) {
      this.startNew();
    }
    this.dex
      .delete(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        error: () => {
          this.sessions.set(previous);
          this.error.set('Could not delete the conversation.');
        },
      });
  }

  /** Dismiss the last list-level error. */
  clearError(): void {
    this.error.set(null);
  }

  /** Abort the list fetch in flight. Clears `loading` — only `next`/`error` would have. */
  private cancelRefresh(): void {
    this.refreshSub?.unsubscribe();
    this.refreshSub = undefined;
    this.loading.set(false);
  }
}
