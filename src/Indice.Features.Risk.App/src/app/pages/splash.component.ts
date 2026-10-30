import { Component, input } from '@angular/core';
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
