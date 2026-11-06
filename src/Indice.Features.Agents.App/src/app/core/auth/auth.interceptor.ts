import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '@indice/ng-auth';
import { catchError, switchMap, throwError } from 'rxjs';

import { isEmbedded } from '../embed/embed';
import { AuthGuestService } from './auth-guest.service';

/**
 * Attaches the caller's bearer token to every `HttpClient` request: the signed-in user's token when
 * there is one, else the guest credential, else nothing (the anonymous create endpoints need none).
 *
 * Replaces `@indice/ng-auth`'s `AuthHttpInterceptor`, which sets `Authorization` unconditionally (a
 * guest token could never get through) and redirects to sign-out on every 401 (a guest would be
 * bounced to the identity server's logout page). The signed-in 401 behaviour is kept verbatim; a
 * guest 401 just drops the dead guest credential.
 *
 * The SSE endpoints use `fetch`, not `HttpClient` — `ChatStreamService` applies the same rule itself.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const guest = inject(AuthGuestService);

  const userHeader = auth.getAuthorizationHeaderValue();
  const header = userHeader || guest.getAuthorizationHeaderValue();
  const authorized = header ? request.clone({ setHeaders: { Authorization: header } }) : request;

  return next(authorized).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401) {
        if (userHeader && isEmbedded) {
          // The widget's panel cannot navigate to the identity server (it refuses to be framed) — drop to guest instead.
          auth.removeUser().pipe(switchMap(() => auth.loadUser())).subscribe();
        } else if (userHeader) {
          // Same as the package: a rejected user token means the session is gone — sign out cleanly.
          auth.removeUser().subscribe(() => auth.signoutRedirect());
        } else {
          guest.clear();
        }
      }
      return throwError(() => error);
    }),
  );
};
