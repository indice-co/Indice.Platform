import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, Subject, Subscription, catchError, finalize, fromEvent, merge, of, tap, timer } from 'rxjs';
import { filter, switchMap } from 'rxjs/operators';
import { toObservable } from '@angular/core/rxjs-interop';
import { ToastService, ToastType } from 'src/app/ui/toast/toast.service';
import { InboxApiService } from './inbox-api.service';
import { InboxMessage, InboxMessageNotification } from './inbox.models';
import { InboxRealtimeService } from './inbox-realtime.service';

/** Number of unread messages shown in the bell preview. */
export const INBOX_PREVIEW_SIZE = 5;
/** Polling interval when real time notifications are not available. */
const POLLING_INTERVAL = 60_000;
/** Safety polling interval while real time notifications are connected. */
const REALTIME_POLLING_INTERVAL = 5 * 60_000;
/** Mark all as read is processed asynchronously by the server, so give it some time before refreshing. */
const MARK_ALL_REFRESH_DELAY = 3_000;

export type InboxChange =
  | { kind: 'created'; notification?: InboxMessageNotification }
  | { kind: 'read' | 'unread' | 'deleted'; messageId: string }
  | { kind: 'all-read' };

/**
 * Application wide state of the current user's inbox: the unread count and a preview of the latest unread messages.
 * Keeps itself up to date through {@link InboxRealtimeService} (SignalR) with a polling fallback.
 */
@Injectable({ providedIn: 'root' })
export class InboxStore {
  private readonly _api = inject(InboxApiService);
  private readonly _realtime = inject(InboxRealtimeService);
  private readonly _toasts = inject(ToastService);
  private readonly _refresh$ = new Subject<void>();
  private readonly _changes$ = new Subject<InboxChange>();
  private readonly _realtimeStatus$ = toObservable(this._realtime.status);
  private _subscription?: Subscription;

  public readonly unreadCount = signal(0);
  public readonly latestUnread = signal<InboxMessage[]>([]);
  public readonly loading = signal(false);
  public readonly loaded = signal(false);
  public readonly hasUnread = computed(() => this.unreadCount() > 0);
  public readonly realtimeStatus = this._realtime.status;
  /** Emits whenever the inbox changes (new message, read state etc.) so that views can refresh. */
  public readonly changes$: Observable<InboxChange> = this._changes$.asObservable();

  /** Starts tracking the inbox. Safe to call multiple times. */
  public start(): void {
    if (this._subscription) {
      return;
    }
    const polling$ = this._realtimeStatus$.pipe(
      switchMap(status => timer(0, status === 'connected' ? REALTIME_POLLING_INTERVAL : POLLING_INTERVAL))
    );
    const visible$ = fromEvent(document, 'visibilitychange').pipe(filter(() => document.visibilityState === 'visible'));
    this._subscription = merge(polling$, visible$, this._refresh$)
      .pipe(
        filter(() => document.visibilityState !== 'hidden' || !this.loaded()),
        tap(() => this.loading.set(true)),
        switchMap(() => this._api.getUnread(INBOX_PREVIEW_SIZE).pipe(
          catchError(() => of(null)),
          finalize(() => this.loading.set(false))
        ))
      )
      .subscribe(result => {
        if (result) {
          this.unreadCount.set(result.count);
          this.latestUnread.set(result.items);
          this.loaded.set(true);
        }
      });
    this._subscription.add(this._realtime.messageCreated$.subscribe(notification => {
      this._toasts.show(ToastType.Info, 'Inbox.NewMessage', notification.title ?? undefined);
      this.refresh();
      this._changes$.next({ kind: 'created', notification });
    }));
    void this._realtime.start();
  }

  public stop(): void {
    this._subscription?.unsubscribe();
    this._subscription = undefined;
    void this._realtime.stop();
  }

  public refresh(): void {
    this._refresh$.next();
  }

  public markAsRead(message: InboxMessage): Observable<void> {
    return this._api.markAsRead(message.id).pipe(tap(() => {
      message.isRead = true;
      this.removeFromPreview(message.id);
      this._changes$.next({ kind: 'read', messageId: message.id });
      this.refresh();
    }));
  }

  public markAsUnread(message: InboxMessage): Observable<void> {
    return this._api.markAsUnread(message.id).pipe(tap(() => {
      message.isRead = false;
      this._changes$.next({ kind: 'unread', messageId: message.id });
      this.refresh();
    }));
  }

  public delete(message: InboxMessage): Observable<void> {
    return this._api.deleteMessage(message.id).pipe(tap(() => {
      if (!message.isRead) {
        this.removeFromPreview(message.id);
      }
      this._changes$.next({ kind: 'deleted', messageId: message.id });
      this.refresh();
    }));
  }

  public markAllAsRead(): Observable<void> {
    return this._api.markAllAsRead().pipe(tap(() => {
      this.unreadCount.set(0);
      this.latestUnread.set([]);
      this._changes$.next({ kind: 'all-read' });
      timer(MARK_ALL_REFRESH_DELAY).subscribe(() => this.refresh());
    }));
  }

  private removeFromPreview(messageId: string): void {
    const before = this.latestUnread();
    const after = before.filter(m => m.id !== messageId);
    if (after.length !== before.length) {
      this.latestUnread.set(after);
      this.unreadCount.update(count => Math.max(0, count - 1));
    }
  }
}
