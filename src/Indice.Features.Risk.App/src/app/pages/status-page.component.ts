import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LogoComponent } from '../ui/logo/logo.component';

/** Not found / forbidden / signed out page. `code`, `title`, `message` and `action` are bound from route data. */
@Component({
  selector: 'app-status-page',
  imports: [RouterLink, LogoComponent],
  host: { class: 'grid min-h-dvh place-items-center bg-base-200 px-6' },
  template: `
    <div class="flex max-w-md flex-col items-center gap-6 text-center">
      <app-logo [size]="40" />
      <p class="tabular text-7xl font-bold tracking-tight text-base-content/15">{{ code() }}</p>
      <div class="space-y-2">
        <h1 class="text-2xl font-semibold">{{ title() }}</h1>
        <p class="text-base-content/70">{{ message() }}</p>
      </div>
      <a routerLink="/home" class="btn btn-primary">{{ action() || 'Back to Risk UI' }}</a>
    </div>
  `
})
export class StatusPageComponent {
  public readonly code = input('404');
  public readonly title = input('Page not found');
  public readonly message = input('');
  public readonly action = input<string>();
}
