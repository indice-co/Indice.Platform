import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '@indice/ng-auth';
import { catchError, throwError } from 'rxjs';

/**
 * Attaches the signed-in user's bearer token to every `HttpClient` request, and signs out cleanly
 * when the API rejects it with a 401.
 *
 * Replaces `@indice/ng-auth`'s `AuthHttpInterceptor`: that one is an NgModule/DI (class-based)
 * interceptor, while this app registers interceptors functionally with `withInterceptors`, where a
 * class interceptor is never picked up.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);

  const header = auth.getAuthorizationHeaderValue();
  const authorized = header ? request.clone({ setHeaders: { Authorization: header } }) : request;

  return next(authorized).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401) {
        // A rejected token means the session is gone — sign out cleanly, as the package does.
        auth.removeUser().subscribe(() => auth.signoutRedirect());
      }
      return throwError(() => error);
    }),
  );
};
