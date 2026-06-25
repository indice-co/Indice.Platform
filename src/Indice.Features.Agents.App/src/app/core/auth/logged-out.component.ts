import { Component, inject } from '@angular/core';
import { AuthService } from '@indice/ng-auth';

/** Landing page shown after sign-out (post_logout_redirect_uri). */
@Component({
  selector: 'app-logged-out',
  template: `
    <div class="flex min-h-screen flex-col items-center justify-center gap-6 bg-base-200 p-6 text-center">
      <div>
        <h1 class="text-2xl font-semibold text-base-content">You've been signed out</h1>
        <p class="mt-2 text-base-content/60">Sign in again to continue chatting with Dex.</p>
      </div>
      <button type="button" class="btn btn-primary" (click)="signIn()">Sign in</button>
    </div>
  `,
})
export class LoggedOutComponent {
  private readonly auth = inject(AuthService);

  signIn(): void {
    this.auth.signinRedirect({ location: '/' });
  }
}
