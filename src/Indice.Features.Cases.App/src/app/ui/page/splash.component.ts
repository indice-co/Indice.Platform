import { Component, input } from '@angular/core';
import { LogoComponent } from '../logo/logo.component';

/** Full-page hand-off screen (signing in, signing out, error pages). */
@Component({
  selector: 'app-splash',
  imports: [LogoComponent],
  template: `
    <div class="flex w-full max-w-md flex-col items-center text-center">
      <app-logo [size]="40" class="text-xl" />
      @if (busy()) {
        <span class="loading loading-dots loading-md mt-10 text-primary" aria-hidden="true"></span>
      }
      @if (title()) {
        <h1 class="mt-6 text-2xl font-semibold tracking-[-0.02em]">{{ title() }}</h1>
      }
      @if (message()) {
        <p class="mt-2 text-quiet" role="status">{{ message() }}</p>
      }
      <div class="mt-6 empty:hidden"><ng-content /></div>
    </div>
  `,
  host: { class: 'flex min-h-dvh items-center justify-center bg-base-200 px-6' }
})
export class SplashComponent {
  public readonly title = input<string | null | undefined>();
  public readonly message = input<string | null | undefined>();
  public readonly busy = input(true);
}
