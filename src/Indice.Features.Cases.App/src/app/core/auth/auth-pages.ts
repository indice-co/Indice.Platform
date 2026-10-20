import { Component, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '@indice/ng-auth';
import { TranslatePipe } from '@ngx-translate/core';
import { map } from 'rxjs/operators';
import { SplashComponent } from 'src/app/ui/page/splash.component';

/** Entry point (`/home`): signed-in users go to the dashboard, everyone else to the identity provider. */
@Component({
  selector: 'app-home',
  imports: [SplashComponent, TranslatePipe],
  template: `<app-splash [title]="title" [message]="'home.pleaseWait' | translate" />`
})
export class HomeComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  protected readonly title = document.title;

  public ngOnInit(): void {
    this.authService.isLoggedIn()
      .pipe(map(isLoggedIn => {
        if (isLoggedIn) {
          this.router.navigate(['/dashboard']);
          return;
        }
        this.authService.signinRedirect({ location: '/dashboard' });
      }))
      .subscribe();
  }
}

/** Return leg of the sign-in redirect. */
@Component({
  selector: 'app-auth-callback',
  imports: [SplashComponent, TranslatePipe],
  template: `<app-splash [message]="'home.pleaseWait' | translate" />`
})
export class AuthCallbackComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  public ngOnInit(): void {
    this.authService.signinRedirectCallback().subscribe(user => {
      if (user) {
        this.router.navigateByUrl((user.url_state as string) || '/');
      }
    });
  }
}

/** Silent token renewal (runs in a hidden iframe). */
@Component({
  selector: 'app-auth-renew',
  template: ''
})
export class AuthRenewComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  public ngOnInit(): void {
    this.authService.signinSilentCallback().subscribe(user => {
      if (!user) {
        this.router.navigate(['/unauthorized']);
      }
    });
  }
}

/** Signs the user out at the identity provider. */
@Component({
  selector: 'app-logout',
  imports: [SplashComponent, TranslatePipe],
  template: `<app-splash [title]="title" [message]="'home.pleaseWait' | translate" />`
})
export class LogOutComponent implements OnInit {
  private readonly authService = inject(AuthService);
  protected readonly title = document.title;

  public ngOnInit(): void {
    this.authService.signoutRedirect();
  }
}
