import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { AuthService } from '@indice/ng-auth';
import { NEVER, of } from 'rxjs';

import { AuthGuestService } from './auth-guest.service';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  let http: HttpClient;
  let backend: HttpTestingController;
  let guest: AuthGuestService;

  const auth = {
    userHeader: '',
    user$: NEVER,
    getAuthorizationHeaderValue: () => auth.userHeader,
    removeUser: jasmine.createSpy('removeUser').and.returnValue(of(void 0)),
    signoutRedirect: jasmine.createSpy('signoutRedirect'),
  };

  beforeEach(() => {
    sessionStorage.removeItem('dex.guest.session');
    auth.userHeader = '';
    auth.removeUser.calls.reset();
    auth.signoutRedirect.calls.reset();
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: auth },
      ],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
    guest = TestBed.inject(AuthGuestService);
  });

  afterEach(() => {
    backend.verify();
    sessionStorage.removeItem('dex.guest.session');
  });

  it('sends the signed-in user token, even when a guest token is present', () => {
    auth.userHeader = 'Bearer user';
    guest.capture({ accessToken: 'guest', expiresIn: 3600 });

    http.get('/api/x').subscribe();

    expect(backend.expectOne('/api/x').request.headers.get('Authorization')).toBe('Bearer user');
  });

  it('falls back to the guest token when nobody is signed in', () => {
    guest.capture({ accessToken: 'guest', expiresIn: 3600 });

    http.get('/api/x').subscribe();

    expect(backend.expectOne('/api/x').request.headers.get('Authorization')).toBe('Bearer guest');
  });

  it('sends no Authorization header when there is neither', () => {
    http.get('/api/x').subscribe();

    expect(backend.expectOne('/api/x').request.headers.has('Authorization')).toBeFalse();
  });

  it('a 401 as guest clears the guest session and does not sign anyone out', () => {
    guest.capture({ accessToken: 'guest', expiresIn: 3600 });
    let failed = false;

    http.get('/api/x').subscribe({ error: () => (failed = true) });
    backend.expectOne('/api/x').flush('', { status: 401, statusText: 'Unauthorized' });

    expect(failed).withContext('error still reaches the caller').toBeTrue();
    expect(guest.isActive).toBeFalse();
    expect(auth.signoutRedirect).not.toHaveBeenCalled();
  });

  it('a 401 while signed in signs the user out (package behaviour kept)', () => {
    auth.userHeader = 'Bearer user';

    http.get('/api/x').subscribe({ error: () => undefined });
    backend.expectOne('/api/x').flush('', { status: 401, statusText: 'Unauthorized' });

    expect(auth.removeUser).toHaveBeenCalled();
    expect(auth.signoutRedirect).toHaveBeenCalled();
  });
});
