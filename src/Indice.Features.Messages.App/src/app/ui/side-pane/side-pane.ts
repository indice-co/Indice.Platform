import { Component, Injectable, inject, input } from '@angular/core';
import { Route, Router, UrlTree } from '@angular/router';
import { AuthGuardService } from '@indice/ng-auth';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideX } from '@lucide/angular';

/** Name of the router outlet the shell renders as a right-hand slide-over pane. */
export const SIDE_PANE_OUTLET = 'rightpane';

export type SidePaneSize = 'sm' | 'md' | 'lg';

@Injectable({ providedIn: 'root' })
export class SidePaneService {
  private readonly _router = inject(Router);

  /**
   * Router link commands that open a pane route, e.g. `[routerLink]="pane.link('create-folder')"` or
   * `pane.link(['edit-folder', folder.id])`. `params` become matrix params of the pane route (`upload-file;folderId=X`),
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
  imports: [TranslatePipe, LucideX],
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
export function paneRoute(path: string, loadComponent: LoadComponent, size: SidePaneSize = 'md', breadcrumb?: string): Route {
  return {
    path,
    outlet: SIDE_PANE_OUTLET,
    canActivate: [AuthGuardService],
    data: { paneSize: size, ...(breadcrumb ? { breadcrumb: { title: breadcrumb } } : {}) },
    loadComponent
  };
}

type LoadComponent = NonNullable<Route['loadComponent']>;
