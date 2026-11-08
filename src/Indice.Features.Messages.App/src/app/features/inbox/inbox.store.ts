import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, Subject, Subscription, catchError, finalize, fromEvent, merge, of, tap, timer } from 'rxjs';
import { distinctUntilChanged, filter, map, switchMap } from 'rxjs/operators';
import { toObservable } from '@angular/core/rxjs-interop';
import { ToastService, ToastType } from 'src/app/ui/toast/toast.service';
import { InboxApiService } from './inbox-api.service';
import { InboxMessage, InboxMessageNotification } from './inbox.models';
import { InboxRealtimeService } from './inbox-realtime.service';

/** Number of unread messages shown in the bell preview. */
export const INBOX_PREVIEW_SIZE = 5;
/**
 * Rare fallback polling of the unread count, used only while real time notifications are not connected (not configured or unavailable).
 * The count is also refreshed whenever the page becomes visible again and after every change made through this store.
 */
const POLLING_INTERVAL = 5 * 60_000;
/** Mark all as read is processed asynchronously by the server, so give it some time before refreshing. */
const MARK_ALL_REFRESH_DELAY = 3_000;

export type InboxChange =
  | { kind: 'created'; notification?: InboxMessageNotification }
  | { kind: 'read' | 'unread' | 'deleted'; messageId: string }
  | { kind: 'all-read' };

/**
 * Application wide state of the current user's inbox: the unread count and, on demand, a preview of the latest unread messages.
 * The count is kept up to date through {@link InboxRealtimeService} (SignalR) and falls back to rare polling when that is not available.
 * All background requests are silent (they do not reveal the global progress bar).
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
  private _previewSubscription?: Subscription;

  public readonly unreadCount = signal(0);
  public readonly latestUnread = signal<InboxMessage[]>([]);
  /** Whether the latest unread preview is being loaded. */
  public readonly previewLoading = signal(false);
  /** Whether the latest unread preview has been loaded at least once. */
  public readonly previewLoaded = signal(false);
  public readonly hasUnread = computed(() => this.unreadCount() > 0);
  public readonly realtimeStatus = this._realtime.status;
  /** Emits whenever the inbox changes (new message, read state etc.) so that views can refresh. */
  public readonly changes$: Observable<InboxChange> = this._changes$.asObservable();

  /** Starts tracking the unread count. Safe to call multiple times. */
  public start(): void {
    if (this._subscription) {
      return;
    }
    // poll only while real time notifications are not connected; (re)connecting fetches the count once to catch up.
    const polling$ = this._realtimeStatus$.pipe(
      map(status => status === 'connected'),
      distinctUntilChanged(),
      switchMap(connected => connected ? of(0) : timer(0, POLLING_INTERVAL))
    );
    const visible$ = fromEvent(document, 'visibilitychange').pipe(filter(() => document.visibilityState === 'visible'));
    this._subscription = merge(
      polling$.pipe(filter(() => document.visibilityState !== 'hidden')),
      visible$,
      this._refresh$
    )
      .pipe(switchMap(() => this._api.getUnreadCount().pipe(catchError(() => of(null)))))
      .subscribe(count => {
        if (count !== null) {
          this.unreadCount.set(count);
        }
      });
    this._subscription.add(this._realtime.messageCreated$.subscribe(notification => {
      this._toasts.show(ToastType.Info, 'Inbox.NewMessage', notification.title ?? undefined);
      this.unreadCount.update(count => count + 1);
      this.previewLoaded.set(false);
      this._changes$.next({ kind: 'created', notification });
    }));
    void this._realtime.start();
  }

  public stop(): void {
    this._subscription?.unsubscribe();
    this._subscription = undefined;
    this._previewSubscription?.unsubscribe();
    void this._realtime.stop();
  }

  /** Loads the latest unread messages (and the unread count along with them). Meant to be called when the preview is opened. */
  public loadPreview(): void {
    this._previewSubscription?.unsubscribe();
    this.previewLoading.set(true);
    this._previewSubscription = this._api.getLatestUnread(INBOX_PREVIEW_SIZE)
      .pipe(catchError(() => of(null)), finalize(() => this.previewLoading.set(false)))
      .subscribe(result => {
        if (result) {
          this.unreadCount.set(result.count);
          this.latestUnread.set(result.items);
          this.previewLoaded.set(true);
        }
      });
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
    this.latestUnread.update(messages => messages.filter(m => m.id !== messageId));
  }
}
