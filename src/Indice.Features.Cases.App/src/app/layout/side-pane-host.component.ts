import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterOutlet } from '@angular/router';
import { SIDE_PANE_OUTLET, SidePaneService, SidePaneSize } from '../ui/side-pane/side-pane';

/** Slide-over panel hosting the `rightpane` named router outlet. Route `data.paneSize` picks the width. */
@Component({
  selector: 'app-side-pane-host',
  imports: [RouterOutlet],
  host: { '(document:keydown.escape)': 'onEscape()' },
  template: `
    <div class="fixed inset-0 z-50 bg-neutral/40 transition-opacity duration-200"
      [class]="active() ? 'opacity-100' : 'pointer-events-none opacity-0'" (click)="pane.close()" aria-hidden="true"></div>
    <aside class="fixed inset-y-0 right-0 z-50 flex w-full flex-col bg-base-100 shadow-2xl transition-transform duration-200 ease-out"
      [class]="[widthClass(), active() ? 'translate-x-0' : 'translate-x-full']" [attr.aria-hidden]="!active()" role="dialog" aria-modal="true">
      <router-outlet [name]="outlet" (activate)="onActivate()" (deactivate)="active.set(false)" />
    </aside>
  `
})
export class SidePaneHostComponent {
  private readonly _route = inject(ActivatedRoute);
  protected readonly pane = inject(SidePaneService);
  protected readonly outlet = SIDE_PANE_OUTLET;
  protected readonly active = signal(false);
  protected readonly widthClass = signal('sm:max-w-md');

  protected onEscape(): void {
    // Escape inside an open dialog belongs to the dialog, not to the pane behind it
    if (this.active() && !document.querySelector('dialog[open]')) {
      this.pane.close();
    }
  }

  protected onActivate(): void {
    const paneRoute = this._route.snapshot.children.find(c => c.outlet === SIDE_PANE_OUTLET);
    const size: SidePaneSize = paneRoute?.data?.['paneSize'] ?? 'md';
    this.widthClass.set({ sm: 'sm:max-w-sm', md: 'sm:max-w-md', lg: 'sm:max-w-2xl' }[size]);
    this.active.set(true);
  }
}
