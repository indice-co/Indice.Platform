import { Component, Injectable, inject, input } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideX } from '@lucide/angular';

/** Name of the router outlet the shell renders as a right-hand slide-over pane. */
export const SIDE_PANE_OUTLET = 'rightpane';

export type SidePaneSize = 'sm' | 'md' | 'lg';

@Injectable({ providedIn: 'root' })
export class SidePaneService {
  private readonly _router = inject(Router);

  /**
   * Router link commands that open `path` in the side pane, e.g. `[routerLink]="pane.link('new-case')"`.
   * `params` become matrix params of the pane route (`new-case;type=X`), bound to the pane component's inputs.
   */
  public link(path: string, params?: Record<string, string>): any[] {
    return ['/', { outlets: { [SIDE_PANE_OUTLET]: params ? [path, params] : [path] } }];
  }

  public open(path: string): Promise<boolean> {
    return this._router.navigate(this.link(path), { queryParamsHandling: 'preserve' });
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
      <button type="button" class="btn btn-ghost btn-sm btn-square" (click)="pane.close()" [attr.aria-label]="'common.close' | translate">
        <svg lucideX class="size-5"></svg>
      </button>
    </header>
    <div class="min-h-0 flex-1 overflow-y-auto overscroll-contain px-6 py-5 [scrollbar-width:thin]">
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
