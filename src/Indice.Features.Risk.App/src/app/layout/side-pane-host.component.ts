import { Component, ElementRef, inject, signal, viewChild } from '@angular/core';
import { ActivatedRoute, RouterOutlet } from '@angular/router';
import { SIDE_PANE_OUTLET, SidePaneService, SidePaneSize } from '../ui/side-pane/side-pane';
import { ToasterComponent } from '../ui/toast/toaster.component';

/**
 * Slide-over panel hosting the `rightpane` named router outlet. Route `data.paneSize` picks the width.
 * Rendered as a native modal dialog, so the browser moves focus inside, makes the page behind it inert and handles Escape.
 */
@Component({
  selector: 'app-side-pane-host',
  imports: [RouterOutlet, ToasterComponent],
  template: `
    <dialog #panel
      class="fixed inset-y-0 right-0 left-auto m-0 h-full max-h-none w-full max-w-full overflow-hidden bg-base-100 p-0 shadow-2xl backdrop:bg-neutral/40"
      [class]="widthClass()" (cancel)="onCancel($event)" (click)="onClick($event)">
      <div class="flex h-full flex-col">
        <router-outlet [name]="outlet" (activate)="onActivate()" (deactivate)="onDeactivate()" />
      </div>
      <app-toaster />
    </dialog>
  `
})
export class SidePaneHostComponent {
  private readonly _route = inject(ActivatedRoute);
  private readonly _panel = viewChild.required<ElementRef<HTMLDialogElement>>('panel');
  private _returnFocus: HTMLElement | null = null;
  protected readonly pane = inject(SidePaneService);
  protected readonly outlet = SIDE_PANE_OUTLET;
  protected readonly widthClass = signal('sm:max-w-md');

  protected onCancel(event: Event): void {
    // Closing goes through the router so the outlet deactivates
    event.preventDefault();
    this.pane.close();
  }

  protected onClick(event: MouseEvent): void {
    // Clicks on the backdrop target the dialog itself; clicks on the content target its children
    if (event.target === this._panel().nativeElement) {
      this.pane.close();
    }
  }

  protected onActivate(): void {
    const paneRoute = this._route.snapshot.children.find(c => c.outlet === SIDE_PANE_OUTLET);
    const size: SidePaneSize = paneRoute?.data?.['paneSize'] ?? 'md';
    this.widthClass.set({ sm: 'sm:max-w-sm', md: 'sm:max-w-md', lg: 'sm:max-w-2xl' }[size]);
    const panel = this._panel().nativeElement;
    if (!panel.open) {
      this._returnFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null;
      panel.showModal();
    }
  }

  protected onDeactivate(): void {
    const panel = this._panel().nativeElement;
    if (panel.open) {
      panel.close();
    }
    if (this._returnFocus?.isConnected) {
      this._returnFocus.focus();
    }
    this._returnFocus = null;
  }
}
