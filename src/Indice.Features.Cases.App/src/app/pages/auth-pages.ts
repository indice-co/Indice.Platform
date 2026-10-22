import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '@indice/ng-auth';
import { SplashComponent } from './splash.component';

/** Entry point: sends signed-in users to the dashboard, everyone else to the identity provider. */
@Component({
  selector: 'app-home',
  imports: [SplashComponent],
  template: `<app-splash message="home.signingIn" />`
})
export class HomeComponent implements OnInit {
  private readonly _auth = inject(AuthService);
  private readonly _router = inject(Router);

  public ngOnInit(): void {
    this._auth.isLoggedIn().subscribe(isLoggedIn => {
      if (isLoggedIn) {
        this._router.navigate(['/dashboard']);
      } else {
        this._auth.signinRedirect({ location: '/dashboard' });
      }
    });
  }
}

@Component({
  selector: 'app-auth-callback',
  imports: [SplashComponent],
  template: `<app-splash message="home.signingIn" />`
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
  template: `<app-splash message="home.signingOut" />`
})
export class LogoutComponent implements OnInit {
  private readonly _auth = inject(AuthService);

  public ngOnInit(): void {
    this._auth.signoutRedirect();
  }
}
