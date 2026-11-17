import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { AuthService } from '@indice/ng-auth';
import { User } from 'oidc-client-ts';
import { Subject } from 'rxjs';

import { EMBED_MODE } from '../embed/embed-mode';
import { AuthGuestService, GuestSessionState } from './auth-guest.service';

const STORAGE_KEY = 'dex.guest.session';
const EMBED_STORAGE_KEY = 'dex.embed.guest.session';

describe('AuthGuestService', () => {
  let user$: Subject<User | null>;

  function create(embed = false): AuthGuestService {
    user$ = new Subject<User | null>();
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        { provide: AuthService, useValue: { user$ } },
        { provide: EMBED_MODE, useValue: embed },
      ],
    });
    return TestBed.inject(AuthGuestService);
  }

  function stored(): GuestSessionState | null {
    const raw = sessionStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as GuestSessionState) : null;
  }

  beforeEach(() => {
    sessionStorage.removeItem(STORAGE_KEY);
    sessionStorage.removeItem(EMBED_STORAGE_KEY);
  });
  afterEach(() => {
    sessionStorage.removeItem(STORAGE_KEY);
    sessionStorage.removeItem(EMBED_STORAGE_KEY);
  });

  it('starts empty when nothing is stored', () => {
    const service = create();
    expect(service.isActive).toBeFalse();
    expect(service.getAuthorizationHeaderValue()).toBe('');
  });

  it('capture emits the session and mirrors it to sessionStorage', () => {
    const service = create();
    const seen: (GuestSessionState | null)[] = [];
    service.session$.subscribe((session) => seen.push(session));

    service.capture({ accessToken: 'tok', tokenType: 'Bearer', expiresIn: 3600, subject: 'g-1' });

    expect(service.isActive).toBeTrue();
    expect(service.getAuthorizationHeaderValue()).toBe('Bearer tok');
    expect(seen.length).withContext('initial null + capture').toBe(2);
    expect(seen[1]?.subject).toBe('g-1');
    expect(stored()?.accessToken).toBe('tok');
  });

  it('capture without a lifetime keeps the session alive (no client-side expiry)', () => {
    const service = create();
    service.capture({ accessToken: 'tok' });
    expect(service.current?.expiresAt).toBeNull();
    expect(service.isActive).toBeTrue();
  });

  it('capture ignores payloads without a token', () => {
    const service = create();
    service.capture(undefined);
    service.capture({ accessToken: '' });
    expect(service.isActive).toBeFalse();
    expect(stored()).toBeNull();
  });

  it('clear emits null exactly once and removes the key', () => {
    const service = create();
    service.capture({ accessToken: 'tok', expiresIn: 3600 });
    const seen: (GuestSessionState | null)[] = [];
    service.session$.subscribe((session) => seen.push(session));

    service.clear();
    service.clear();

    expect(seen).toEqual([jasmine.objectContaining({ accessToken: 'tok' }), null]);
    expect(stored()).toBeNull();
    expect(service.isActive).toBeFalse();
  });

  it('restores a stored session on construction', () => {
    sessionStorage.setItem(
      STORAGE_KEY,
      JSON.stringify({ accessToken: 'old', tokenType: 'Bearer', subject: null, expiresAt: Date.now() + 60_000 }),
    );
    expect(create().getAuthorizationHeaderValue()).toBe('Bearer old');
  });

  it('drops an expired stored session on construction', () => {
    sessionStorage.setItem(
      STORAGE_KEY,
      JSON.stringify({ accessToken: 'dead', tokenType: 'Bearer', subject: null, expiresAt: Date.now() - 1 }),
    );
    expect(create().isActive).toBeFalse();
    expect(stored()).toBeNull();
  });

  it('a token inside the expiry skew is treated as expired and cleared', () => {
    const service = create();
    service.capture({ accessToken: 'tok', expiresIn: 10 });
    expect(service.isActive).toBeFalse();
    expect(stored()).toBeNull();
  });

  it('a signed-in user clears the guest session', () => {
    const service = create();
    service.capture({ accessToken: 'tok', expiresIn: 3600 });

    user$.next(null);
    expect(service.isActive).withContext('no user yet').toBeTrue();

    user$.next({ expired: false } as User);
    expect(service.isActive).toBeFalse();
    expect(stored()).toBeNull();
  });

  it('displayName is trimmed, capped and empty means none', () => {
    const service = create();
    service.displayName = '  Maria  ';
    expect(service.displayName).toBe('Maria');
    service.displayName = 'x'.repeat(300);
    expect(service.displayName?.length).toBe(100);
    service.displayName = '   ';
    expect(service.displayName).toBeNull();
    service.displayName = 42 as unknown as string;
    expect(service.displayName).toBeNull();
  });

  describe('in the widget frame (embed mode)', () => {
    it('keeps its credential under its own key, apart from the full UI', () => {
      sessionStorage.setItem(
        STORAGE_KEY,
        JSON.stringify({ accessToken: 'full-ui', tokenType: 'Bearer', subject: null, expiresAt: null }),
      );
      const service = create(true);
      expect(service.isActive).withContext("the full UI's guest is not adopted").toBeFalse();

      service.capture({ accessToken: 'widget', expiresIn: 3600 });

      expect(JSON.parse(sessionStorage.getItem(EMBED_STORAGE_KEY)!).accessToken).toBe('widget');
      expect(stored()?.accessToken).withContext('the full UI key is untouched').toBe('full-ui');
    });

    it('is not cleared by a signed-in user (that user belongs to a same-origin tab)', () => {
      const service = create(true);
      service.capture({ accessToken: 'tok', expiresIn: 3600 });

      user$.next({ expired: false } as User);

      expect(service.isActive).toBeTrue();
    });
  });
});
