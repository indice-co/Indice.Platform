import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '@indice/ng-auth';
import { SplashComponent } from './splash.component';

/** Entry point: sends signed-in users to the risk events, everyone else to the identity provider. */
@Component({
  selector: 'app-home',
  imports: [SplashComponent],
  template: `<app-splash message="Signing you in…" />`
})
export class HomeComponent implements OnInit {
  private readonly _auth = inject(AuthService);
  private readonly _router = inject(Router);

  public ngOnInit(): void {
    this._auth.isLoggedIn().subscribe(isLoggedIn => {
      if (isLoggedIn) {
        this._router.navigate(['/risk-events']);
      } else {
        this._auth.signinRedirect({ location: '/risk-events' });
      }
    });
  }
}

@Component({
  selector: 'app-auth-callback',
  imports: [SplashComponent],
  template: `<app-splash message="Signing you in…" />`
})
export class AuthCallbackComponent implements OnInit {
  private readonly _auth = inject(AuthService);
  private readonly _router = inject(Router);

  public ngOnInit(): void {
    this._auth.signinRedirectCallback().subscribe(user => {
      if (user) {
        this._router.navigateByUrl((user.url_state as string | undefined) || '/');
      }
    });
  }
}

@Component({
  selector: 'app-auth-renew',
  template: ``
})
export class AuthRenewComponent implements OnInit {
  private readonly _auth = inject(AuthService);
  private readonly _router = inject(Router);

  public ngOnInit(): void {
    this._auth.signinSilentCallback().subscribe(user => {
      if (!user) {
        this._router.navigate(['/forbidden']);
      }
    });
  }
}

@Component({
  selector: 'app-logout',
  imports: [SplashComponent],
  template: `<app-splash message="Signing you out…" />`
})
export class LogoutComponent implements OnInit {
  private readonly _auth = inject(AuthService);

  public ngOnInit(): void {
    this._auth.signoutRedirect();
  }
}
