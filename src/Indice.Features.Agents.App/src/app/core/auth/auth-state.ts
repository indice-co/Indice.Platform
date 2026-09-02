import { Signal, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { AuthService } from '@indice/ng-auth';
import { map } from 'rxjs';

/**
 * Whether a real (non-guest) user is signed in. `undefined` until `AuthService` has loaded the user,
 * so templates never flash guest UI at a signed-in user. Call from an injection context.
 */
export function injectSignedIn(): Signal<boolean | undefined> {
  return toSignal(
    inject(AuthService).user$.pipe(map((user) => !!user && !user.expired)),
    { initialValue: undefined },
  );
}
