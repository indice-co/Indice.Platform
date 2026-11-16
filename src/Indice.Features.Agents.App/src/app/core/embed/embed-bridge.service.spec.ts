import { provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { AuthGuestService } from '../auth/auth-guest.service';
import { ConversationsStore } from '../services/conversations.store';
import { EmbedBridgeService } from './embed-bridge.service';
import { WIDGET_ORIGINS } from './embed-mode';

const CHAT_KEY = 'dex.embed.chat';

describe('EmbedBridgeService', () => {
  let guest: { isActive: boolean; displayName: string | null };
  let store: { activeId: ReturnType<typeof signal<string | null>>; select: (id: string) => void };

  function create(search = '', guestActive = true): EmbedBridgeService {
    history.replaceState(null, '', `${location.pathname}${search}`);
    guest = { isActive: guestActive, displayName: null };
    const activeId = signal<string | null>(null);
    store = { activeId, select: (id: string) => activeId.set(id) };
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        { provide: AuthGuestService, useValue: guest },
        { provide: ConversationsStore, useValue: store },
        { provide: WIDGET_ORIGINS, useValue: ['https://shop.example'] },
      ],
    });
    return TestBed.inject(EmbedBridgeService);
  }

  function stored(): { id: string; refId: string | null; refType: string | null } | null {
    const raw = sessionStorage.getItem(CHAT_KEY);
    return raw ? JSON.parse(raw) : null;
  }

  function storeChat(id: string, refId: string | null = null, refType: string | null = null): void {
    sessionStorage.setItem(CHAT_KEY, JSON.stringify({ id, refId, refType }));
  }

  beforeEach(() => sessionStorage.removeItem(CHAT_KEY));
  afterEach(() => {
    sessionStorage.removeItem(CHAT_KEY);
    history.replaceState(null, '', location.pathname);
  });

  it('resumes the stored chat on a page without a topic', () => {
    storeChat('c-1', 'order-1', 'order');
    create();
    expect(store.activeId()).toBe('c-1');
  });

  it('resumes the stored chat on a page about the same record', () => {
    storeChat('c-1', 'order-1', 'order');
    create('?refid=order-1&reftype=order');
    expect(store.activeId()).toBe('c-1');
  });

  it('starts fresh on a page about another record, and forgets the old one', () => {
    storeChat('c-1', 'order-1', 'order');
    create('?refid=order-2&reftype=order');
    expect(store.activeId()).toBeNull();
    expect(stored()).toBeNull();
  });

  it('starts fresh when the guest the chat belonged to is gone', () => {
    storeChat('c-1');
    create('', false);
    expect(store.activeId()).toBeNull();
    expect(stored()).toBeNull();
  });

  it('remembers the open chat with the page topic, and forgets it on a new chat', () => {
    create('?refid=order-7&reftype=order');

    store.activeId.set('c-9');
    TestBed.tick();
    expect(stored()).toEqual({ id: 'c-9', refId: 'order-7', refType: 'order' });

    store.activeId.set(null);
    TestBed.tick();
    expect(stored()).toBeNull();
  });

  it('keeps the original topic when the chat is resumed on a page without one', () => {
    storeChat('c-1', 'order-1', 'order');
    create();
    TestBed.tick();
    expect(stored()).toEqual({ id: 'c-1', refId: 'order-1', refType: 'order' });
  });

  it('takes identify only from the parent window, at its origin, in the widget envelope', () => {
    const bridge = create();
    if (!bridge.framed) {
      pending('needs the spec runner to run in a frame');
      return;
    }
    const send = (origin: string, source: Window | null, data: unknown) =>
      window.dispatchEvent(new MessageEvent('message', { origin, source, data }));
    const identify = (name: string) => ({ dexWidget: 1, type: 'identify', payload: { name } });

    send('https://evil.example', window.parent, identify('Eve'));
    send(location.origin, window, identify('Not the parent'));
    send(location.origin, window.parent, { type: 'identify', payload: { name: 'No envelope' } });
    // Allowed, but the browser reports the real parent's origin (the runner's), which the frame has pinned.
    send('https://shop.example', window.parent, identify('Other allowed origin'));
    expect(guest.displayName).toBeNull();

    send(location.origin, window.parent, identify('Maria'));
    expect(guest.displayName).toBe('Maria');
  });
});
