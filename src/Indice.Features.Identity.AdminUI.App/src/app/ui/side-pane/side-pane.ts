import { Component, Injectable, inject, input } from '@angular/core';
import { Route, Router, UrlTree } from '@angular/router';
import { LucideX } from '@lucide/angular';
import { TranslatePipe } from '@ngx-translate/core';
import { authGuard } from 'src/app/core/services/auth.guards';

/** Name of the router outlet the shell renders as a right-hand slide-over pane. */
export const SIDE_PANE_OUTLET = 'rightpane';

export type SidePaneSize = 'sm' | 'md' | 'lg';

@Injectable({ providedIn: 'root' })
export class SidePaneService {
  private readonly _router = inject(Router);

  /**
   * Router link commands that open a pane route, e.g. `[routerLink]="pane.link('add-role')"` or
   * `pane.link(['client-secret', clientId])`. `params` become matrix params of the pane route (`add-claim;userId=X`),
   * bound to the pane component's inputs.
   */
  public link(path: string | (string | number)[], params?: Record<string, string>): UrlTree {
    const segments = Array.isArray(path) ? path.map(String) : [path];
    return this._router.createUrlTree(['/', { outlets: { [SIDE_PANE_OUTLET]: params ? [...segments, params] : segments } }], { queryParamsHandling: 'preserve' });
  }

  public open(path: string | (string | number)[], params?: Record<string, string>): Promise<boolean> {
    return this._router.navigateByUrl(this.link(path, params));
  }

  public close(): Promise<boolean> {
    return this._router.navigate(['/', { outlets: { [SIDE_PANE_OUTLET]: null } }], { queryParamsHandling: 'preserve' });
  }
}

/**
 * Layout for a component routed into the side pane: sticky header with title and close button, scrolling body,
 * optional sticky footer (`<div footer>…</div>`).
 */
@Component({
  selector: 'app-side-pane-layout',
  imports: [LucideX, TranslatePipe],
  host: { class: 'flex h-full flex-col' },
  template: `
    <header class="flex items-start justify-between gap-4 border-b border-base-300 px-6 py-5">
      <div class="min-w-0">
        <h2 class="text-lg font-semibold">{{ title() }}</h2>
        @if (subtitle()) {
          <p class="mt-0.5 text-sm text-base-content/70">{{ subtitle() }}</p>
        }
      </div>
      <button type="button" class="btn btn-ghost btn-sm btn-square" (click)="pane.close()" [attr.aria-label]="'Common.Close' | translate">
        <svg lucideX class="size-5"></svg>
      </button>
    </header>
    <div class="flex-1 overflow-y-auto px-6 py-5">
      <ng-content />
    </div>
    <ng-content select="[footer]" />
  `
})
export class SidePaneLayoutComponent {
  public readonly title = input.required<string>();
  public readonly subtitle = input<string>();
  protected readonly pane = inject(SidePaneService);
}

/**
 * Route for a component rendered in the side pane (`rightpane` outlet). Pane routes are top level routes that require
 * an authenticated user; `size` picks the pane width.
 */
export function paneRoute(path: string, loadComponent: LoadComponent, size: SidePaneSize = 'md'): Route {
  return {
    path,
    outlet: SIDE_PANE_OUTLET,
    canActivate: [authGuard],
    data: { paneSize: size },
    loadComponent
  };
}

type LoadComponent = NonNullable<Route['loadComponent']>;
