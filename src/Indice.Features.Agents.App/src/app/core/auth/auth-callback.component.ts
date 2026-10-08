import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '@indice/ng-auth';

/** Handles the OAuth redirect back from the identity server, then routes to the original page. */
@Component({
  selector: 'app-auth-callback',
  template: `
    <div class="flex min-h-screen flex-col items-center justify-center gap-4 bg-base-200">
      <span class="loading loading-dots loading-lg text-primary"></span>
      <p class="text-base-content/60">Signing you in…</p>
    </div>
  `,
})
export class AuthCallbackComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  ngOnInit(): void {
    this.auth.signinRedirectCallback().subscribe({
      next: (user) => this.router.navigateByUrl(((user?.url_state as string | undefined) ?? '/')),
      error: () => this.router.navigateByUrl('/'),
    });
  }
}
