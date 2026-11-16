import { Component, inject } from '@angular/core';
import { AuthService } from '@indice/ng-auth';
import { LucideLogIn, LucideLogOut } from '@lucide/angular';

/** Landing page shown after sign-out (post_logout_redirect_uri). */
@Component({
  selector: 'app-logged-out',
  imports: [LucideLogIn, LucideLogOut],
  template: `
    <div class="flex min-h-screen flex-col items-center justify-center gap-6 bg-base-200 p-6 text-center">
      <span class="grid size-14 place-items-center rounded-full bg-base-content/6 text-base-content/60" aria-hidden="true">
        <svg lucideLogOut class="size-7"></svg>
      </span>
      <div>
        <h1 class="text-2xl font-semibold text-base-content">You've been signed out</h1>
        <p class="mt-2 text-base-content/70">Sign in again to view the workers dashboard.</p>
      </div>
      <button type="button" class="btn btn-primary gap-1.5" (click)="signIn()">
        <svg lucideLogIn class="size-4"></svg>Sign in
      </button>
    </div>
  `,
})
export class LoggedOutComponent {
  private readonly auth = inject(AuthService);

  signIn(): void {
    this.auth.signinRedirect({ location: '/' });
  }
}
