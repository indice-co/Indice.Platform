import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LogoComponent } from '../ui/logo/logo.component';

/** Not-found / forbidden page. `code`, `title` and `message` are bound from route data. */
@Component({
  selector: 'app-status-page',
  imports: [RouterLink, TranslatePipe, LogoComponent],
  host: { class: 'grid min-h-dvh place-items-center bg-base-200 px-6' },
  template: `
    <div class="flex max-w-md flex-col items-center gap-6 text-center">
      <app-logo [size]="40" />
      <p class="docket text-7xl font-bold tracking-tight text-base-content/15">{{ code() }}</p>
      <div class="space-y-2">
        <h1 class="text-2xl font-bold">{{ title() | translate }}</h1>
        <p class="text-base-content/70">{{ message() | translate }}</p>
      </div>
      <a routerLink="/dashboard" class="btn btn-primary">{{ 'status.backToDashboard' | translate }}</a>
    </div>
  `
})
export class StatusPageComponent {
  public readonly code = input('404');
  public readonly title = input('status.notFound.title');
  public readonly message = input('status.notFound.message');
}
