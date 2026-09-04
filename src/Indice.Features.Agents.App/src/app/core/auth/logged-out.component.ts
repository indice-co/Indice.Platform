import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '@indice/ng-auth';

/** Landing page shown after sign-out (post_logout_redirect_uri). */
@Component({
  selector: 'app-logged-out',
  imports: [RouterLink],
  template: `
    <div class="flex min-h-screen flex-col items-center justify-center gap-6 bg-base-200 p-6 text-center">
      <div>
        <h1 class="text-2xl font-semibold text-base-content">You've been signed out</h1>
        <p class="mt-2 text-base-content/60">Sign in again to continue chatting with Dex.</p>
      </div>
      <div class="flex flex-col items-center gap-3 sm:flex-row sm:justify-center">
        <button type="button" class="btn btn-primary rounded-full" (click)="signIn()">Sign in</button>
        <a routerLink="/" class="btn btn-ghost rounded-full text-base-content/60">Continue as guest</a>
      </div>

    </div>
  `,
})
export class LoggedOutComponent {
  private readonly auth = inject(AuthService);

  signIn(): void {
    this.auth.signinRedirect({ location: '/' });
  }
}
