import { Component, OnInit, inject } from '@angular/core';
import { AuthService } from '@indice/ng-auth';

import { isSigninPopup, signinPopupCallback } from './popup-signin';

/**
 * Invisible silent-renew endpoint loaded in a hidden iframe by oidc-client-ts. It doubles as the return URI of
 * the widget's popup sign-in (see `popup-signin.ts`): a popup is a top-level window with an opener, the renew
 * iframe never is — so either response is told apart without reading any stored state.
 */
@Component({
  selector: 'app-auth-renew',
  template: '',
})
export class AuthRenewComponent implements OnInit {
  private readonly auth = inject(AuthService);

  ngOnInit(): void {
    if (isSigninPopup()) {
      signinPopupCallback().catch(() => window.close());
      return;
    }
    this.auth.signinSilentCallback().subscribe({ error: () => undefined });
  }
}
