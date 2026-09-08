import { inject } from '@angular/core';
import { CanActivateFn } from '@angular/router';
import { AuthService } from '@indice/ng-auth';
import { map } from 'rxjs';

/**
 * Holds navigation until `AuthService` has read the OIDC user from storage, then lets everyone
 * through — signed in or not. The shell used to get this wait for free from `AuthGuardService`; now
 * that guests may enter, it still must not construct (and call the API) before the user is known.
 */
export const authSettledGuard: CanActivateFn = () =>
  inject(AuthService).isLoggedIn().pipe(map(() => true));
