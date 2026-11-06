import { UserManager, UserManagerSettings } from 'oidc-client-ts';

import { settings } from '../models/settings';

/**
 * Popup sign-in for the embedded panel. The identity server's login pages refuse to be framed
 * (`frame-ancestors 'none'`), so inside the widget the sign-in runs in a popup instead of a redirect.
 *
 * `@indice/ng-auth` exposes no popup flow, so this is a second `oidc-client-ts` `UserManager` over the same settings
 * — and therefore the same storage keys: the user it stores is the one `AuthService.loadUser()` reads back. Its own
 * renew and session monitoring stay off; `AuthService` already runs those.
 *
 * The popup comes back on the silent-renew URI (`auth-renew`), which the client already has registered —
 * `AuthRenewComponent` tells a popup from the hidden renew iframe and completes whichever it is.
 */
let manager: UserManager | null = null;

function userManager(): UserManager {
  manager ??= new UserManager({
    ...(settings.auth_settings as unknown as UserManagerSettings),
    popup_redirect_uri: settings.auth_settings.silent_redirect_uri,
    automaticSilentRenew: false,
    monitorSession: false,
  });
  return manager;
}

/** Signs the visitor in through a popup window; resolves once the user is stored, rejects when the popup is closed or blocked. */
export async function signinPopup(): Promise<void> {
  await userManager().signinPopup();
}

/** Completes a popup sign-in from inside the popup: hands the response URL to the opener and closes the window. */
export async function signinPopupCallback(): Promise<void> {
  await userManager().signinPopupCallback();
}

/** Whether this window is a sign-in popup: a top-level window that some other window opened. */
export function isSigninPopup(): boolean {
  return window.parent === window && !!window.opener;
}
