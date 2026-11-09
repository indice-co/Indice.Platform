import { signal } from '@angular/core';
import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { Subject, of } from 'rxjs';
import { ToastService } from 'src/app/ui/toast/toast.service';
import { InboxApiService } from './inbox-api.service';
import { InboxRealtimeService } from './inbox-realtime.service';
import { InboxMessage, InboxMessageNotification, InboxResultSet } from './inbox.models';
import { InboxStore } from './inbox.store';

describe('InboxStore', () => {
  let store: InboxStore;
  let api: jasmine.SpyObj<InboxApiService>;
  let notifications: Subject<InboxMessageNotification>;

  beforeEach(() => {
    notifications = new Subject();
    api = jasmine.createSpyObj('InboxApiService', ['getUnreadCount', 'getLatestUnread', 'markAllAsRead']);
    api.getUnreadCount.and.returnValue(of(2));
    api.markAllAsRead.and.returnValue(of(undefined));
    TestBed.configureTestingModule({
      providers: [
        { provide: InboxApiService, useValue: api },
        { provide: ToastService, useValue: { show: () => undefined } },
        { provide: InboxRealtimeService, useValue: {
          status: signal('connected'), messageCreated$: notifications,
          start: () => Promise.resolve(), stop: () => Promise.resolve()
        } }
      ]
    });
    spyOnProperty(document, 'visibilityState').and.returnValue('visible');
  });

  afterEach(() => store.stop());

  it('keeps polling the count while connected', fakeAsync(() => {
    store = TestBed.inject(InboxStore);
    store.start();
    TestBed.tick();
    tick();
    expect(api.getUnreadCount).toHaveBeenCalledTimes(1);
    tick(5 * 60_000);
    expect(api.getUnreadCount).toHaveBeenCalledTimes(2);
    store.stop();
  }));

  it('invalidates the count on notifications and cancels older requests', fakeAsync(() => {
    store = TestBed.inject(InboxStore);
    const stale = new Subject<number>();
    api.getUnreadCount.and.returnValues(stale, of(2));
    store.start();
    TestBed.tick();
    tick();
    expect(api.getUnreadCount).toHaveBeenCalledTimes(1);
    expect(notifications.observed).toBeTrue();
    notifications.next({ id: 'message-1' });
    expect(store.unreadCount()).toBe(2);
    expect(stale.observed).toBeFalse();
    stale.next(1);
    expect(store.unreadCount()).toBe(2);
    store.stop();
  }));

  it('emits delayed reconciliation after optimistic mark-all and cancels it on stop', fakeAsync(() => {
    store = TestBed.inject(InboxStore);
    store.start();
    TestBed.tick();
    tick();
    const changes: string[] = [];
    store.changes$.subscribe(change => changes.push(change.kind));
    store.markAllAsRead().subscribe();
    expect(store.unreadCount()).toBe(0);
    expect(changes).toEqual(['all-read']);
    tick(3_000);
    expect(changes).toEqual(['all-read', 'reconcile']);
    store.markAllAsRead().subscribe();
    store.stop();
    tick(3_000);
    expect(changes).toEqual(['all-read', 'reconcile', 'all-read']);
  }));

  it('continues reconciling filtered lists when bulk processing outlasts the initial delay', fakeAsync(() => {
    store = TestBed.inject(InboxStore);
    store.start();
    TestBed.tick();
    tick();
    const changes: string[] = [];
    store.changes$.subscribe(change => changes.push(change.kind));
    store.markAllAsRead().subscribe();
    tick(3_000);
    expect(store.unreadCount()).toBe(2);
    api.getUnreadCount.and.returnValue(of(0));
    tick(5 * 60_000);
    expect(store.unreadCount()).toBe(0);
    expect(changes).toEqual(['all-read', 'reconcile', 'reconcile']);
    store.stop();
  }));

  it('replaces an in-flight preview when a notification arrives', fakeAsync(() => {
    store = TestBed.inject(InboxStore);
    const stale = new Subject<InboxResultSet<InboxMessage>>();
    const message = { id: 'message-1', createdAt: '' };
    api.getLatestUnread.and.returnValues(stale, of({ count: 2, items: [message] }));
    store.start();
    TestBed.tick();
    tick();
    store.loadPreview();
    notifications.next({ id: 'message-1' });
    expect(stale.observed).toBeFalse();
    expect(store.latestUnread()).toEqual([message]);
    expect(store.previewLoaded()).toBeTrue();
    store.stop();
  }));
});
