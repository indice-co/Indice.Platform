import { Component, Injectable, inject, input } from '@angular/core';
import { Route, Router, UrlTree } from '@angular/router';
import { AuthGuardService } from '@indice/ng-auth';
import { LucideX } from '@lucide/angular';

/** Name of the router outlet the app renders as a right-hand slide-over pane. */
export const SIDE_PANE_OUTLET = 'rightpane';

export type SidePaneSize = 'sm' | 'md' | 'lg';

@Injectable({ providedIn: 'root' })
export class SidePaneService {
  private readonly _router = inject(Router);

  /** Router link commands that open a pane route, e.g. `[routerLink]="pane.link(['event', item.id])"`. */
  public link(path: string | (string | number)[]): UrlTree {
    const segments = Array.isArray(path) ? path.map(String) : [path];
    return this._router.createUrlTree(['/', { outlets: { [SIDE_PANE_OUTLET]: segments } }], { queryParamsHandling: 'preserve' });
  }

  public open(path: string | (string | number)[]): Promise<boolean> {
    return this._router.navigateByUrl(this.link(path));
  }

  public close(): Promise<boolean> {
    return this._router.navigate(['/', { outlets: { [SIDE_PANE_OUTLET]: null } }], { queryParamsHandling: 'preserve' });
  }
}

/**
 * Layout for a component routed into the side pane: sticky header with title and close button, scrolling body,
 * optional sticky footer (`<div footer>…</div>`) and optional header content (`<div header>…</div>`).
 */
@Component({
  selector: 'app-side-pane-layout',
  imports: [LucideX],
  host: { class: 'flex min-h-0 flex-1 flex-col' },
  template: `
    <header class="border-b border-base-300 px-6 py-5">
      <div class="flex items-start justify-between gap-4">
        <div class="min-w-0">
          @if (eyebrow()) {
            <p class="mb-1 text-xs font-semibold tracking-wider text-primary uppercase">{{ eyebrow() }}</p>
          }
          <h2 id="side-pane-heading" class="text-lg font-semibold break-words">{{ title() }}</h2>
          @if (subtitle()) {
            <p class="mt-0.5 text-sm text-base-content/70">{{ subtitle() }}</p>
          }
        </div>
        <button type="button" class="btn btn-ghost btn-sm btn-square" (click)="pane.close()" aria-label="Close">
          <svg lucideX class="size-5"></svg>
        </button>
      </div>
      <ng-content select="[header]" />
    </header>
    <div class="min-h-0 flex-1 overflow-y-auto overscroll-contain px-6 py-5">
      <ng-content />
    </div>
    <ng-content select="[footer]" />
  `
})
export class SidePaneLayoutComponent {
  public readonly title = input.required<string>();
  public readonly subtitle = input<string>();
  public readonly eyebrow = input<string>();
  protected readonly pane = inject(SidePaneService);
}

/** Route for a component rendered in the side pane. Pane routes are top level and require an authenticated user. */
export function paneRoute(path: string, loadComponent: NonNullable<Route['loadComponent']>, size: SidePaneSize = 'md'): Route {
  return {
    path,
    outlet: SIDE_PANE_OUTLET,
    canActivate: [AuthGuardService],
    data: { paneSize: size },
    loadComponent
  };
}
