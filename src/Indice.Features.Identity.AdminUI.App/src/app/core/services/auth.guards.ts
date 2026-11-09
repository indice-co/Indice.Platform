import { inject } from '@angular/core';
import { CanActivateFn, CanMatchFn, Router } from '@angular/router';
import { map, tap } from 'rxjs';
import { Features } from '../models/features';
import { AuthService } from './auth.service';
import { UiFeaturesService } from './ui-features.service';

/** Requires a signed-in user; otherwise starts the sign-in redirect and comes back to the requested url. */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  return auth.isLoggedIn().pipe(
    tap(isLoggedIn => {
      if (!isLoggedIn) {
        auth.signinRedirect(state.url);
      }
    })
  );
};

export type Permission = 'usersReader' | 'usersWriter' | 'clientsReader' | 'clientsWriter' | 'admin';

/** Checks one of the Admin UI permissions of the current user. */
export function hasPermission(auth: AuthService, permission: Permission): boolean {
  switch (permission) {
    case 'usersReader': return auth.isAdminUIUsersReader();
    case 'usersWriter': return auth.isAdminUIUsersWriter();
    case 'clientsReader': return auth.isAdminUIClientsReader();
    case 'clientsWriter': return auth.isAdminUIClientsWriter();
    default: return auth.isAdmin();
  }
}

/** Lets the route through when the user holds any of the given permissions, otherwise shows the forbidden page. */
export function permissionGuard(...permissions: Permission[]): CanActivateFn {
  return () => {
    const auth = inject(AuthService);
    return permissions.some(permission => hasPermission(auth, permission)) || inject(Router).parseUrl('/forbidden');
  };
}

/** Matches the route only when the given UI feature is enabled on the server. */
export function featureGuard(feature: Features): CanMatchFn {
  return () => inject(UiFeaturesService).isEnabled(feature);
}

/** Same as {@link featureGuard} for child routes that are already matched (e.g. tabs). */
export function featureActivateGuard(feature: Features): CanActivateFn {
  return () => {
    const router = inject(Router);
    return inject(UiFeaturesService).isEnabled(feature).pipe(map(enabled => enabled || router.parseUrl('/forbidden')));
  };
}
