import { Component, OnInit, inject, input } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { LucideArrowRight, LucideKeyRound, LucideShieldCheck, LucideUsers } from '@lucide/angular';
import { AuthService } from '../core/services/auth.service';
import { LogoComponent } from '../ui/logo/logo.component';

/** Full screen brand splash used while authentication redirects are in flight. */
@Component({
  selector: 'app-splash',
  imports: [LogoComponent],
  host: { class: 'grid min-h-dvh place-items-center bg-base-200 px-6' },
  template: `
    <div class="flex flex-col items-center gap-6 text-center">
      <app-logo [size]="48" ring="var(--color-base-200)" />
      <div class="flex items-center gap-3 text-base-content/75">
        <span class="loading loading-dots loading-sm text-primary"></span>
        <span>{{ message() }}</span>
      </div>
    </div>
  `
})
export class SplashComponent {
  public readonly message = input('Please wait…');
}

/** Public landing page: signed-in users go straight to the dashboard, everyone else gets a sign in call to action. */
@Component({
  selector: 'app-home',
  imports: [LogoComponent, LucideArrowRight, LucideKeyRound, LucideShieldCheck, LucideUsers],
  host: { class: 'relative grid min-h-dvh place-items-center overflow-hidden bg-neutral px-6 text-neutral-content' },
  template: `
    <div class="pointer-events-none absolute inset-0 opacity-60"
      style="background: radial-gradient(60rem 30rem at 85% -10%, color-mix(in oklab, var(--color-primary) 45%, transparent), transparent),
                         radial-gradient(40rem 24rem at -10% 110%, color-mix(in oklab, var(--color-accent) 25%, transparent), transparent)">
    </div>
    <div class="relative flex w-full max-w-md flex-col items-center gap-8 text-center">
      <app-logo [size]="56" ring="var(--color-neutral)" class="text-white" />
      <div class="space-y-3">
        <h1 class="text-3xl font-semibold tracking-tight text-balance text-white sm:text-4xl">Identity administration</h1>
        <p class="text-neutral-content/75">Manage users, roles, client applications and the resources they can reach, all in one place.</p>
      </div>
      <ul class="flex flex-wrap justify-center gap-2 text-sm">
        <li class="badge border-white/10 bg-white/5 py-3 text-neutral-content"><svg lucideUsers class="size-3.5"></svg> Users & roles</li>
        <li class="badge border-white/10 bg-white/5 py-3 text-neutral-content"><svg lucideKeyRound class="size-3.5"></svg> Clients & scopes</li>
        <li class="badge border-white/10 bg-white/5 py-3 text-neutral-content"><svg lucideShieldCheck class="size-3.5"></svg> Audit logs</li>
      </ul>
      <button type="button" class="btn btn-accent btn-wide gap-2" (click)="signIn()">
        Sign in <svg lucideArrowRight class="size-4"></svg>
      </button>
    </div>
  `
})
export class HomeComponent implements OnInit {
  private readonly _auth = inject(AuthService);
  private readonly _router = inject(Router);

  public ngOnInit(): void {
    this._auth.isLoggedIn().subscribe(isLoggedIn => {
      if (isLoggedIn) {
        this._router.navigate(['/dashboard']);
      }
    });
  }

  protected signIn(): void {
    this._auth.signinRedirect('/dashboard');
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
      if (user && this._auth.userHasAccess()) {
        this._router.navigateByUrl(toAppUrl(user.url_state as string | undefined));
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

  public ngOnInit(): void {
    this._auth.signinSilentCallback().subscribe(() => this._auth.userHasAccess());
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

/** Forbidden / not found page. `code`, `title` and `message` are bound from route data. */
@Component({
  selector: 'app-status-page',
  imports: [RouterLink, LogoComponent],
  host: { class: 'grid min-h-dvh place-items-center bg-base-200 px-6' },
  template: `
    <div class="flex max-w-md flex-col items-center gap-6 text-center">
      <app-logo [size]="40" ring="var(--color-base-200)" />
      <p class="tabular text-7xl font-bold tracking-tight text-base-content/15">{{ code() }}</p>
      <div class="space-y-2">
        <h1 class="text-2xl font-bold">{{ title() }}</h1>
        <p class="text-base-content/70">{{ message() }}</p>
      </div>
      <div class="flex gap-2">
        <a routerLink="/dashboard" class="btn btn-primary">Back to dashboard</a>
        @if (code() === '403') {
          <a routerLink="/logout" class="btn btn-ghost">Sign out</a>
        }
      </div>
    </div>
  `
})
export class StatusPageComponent {
  public readonly code = input('404');
  public readonly title = input('Page not found');
  public readonly message = input('The page you are looking for does not exist or has been moved.');
}

/** Sign in state urls saved by the previous UI started with `/app/`; strip that so old links still land correctly. */
function toAppUrl(url: string | undefined): string {
  if (!url || url === '/' || url.startsWith('/home')) {
    return '/dashboard';
  }
  return url.replace(/^\/?app(?=\/|$)/, '') || '/dashboard';
}
