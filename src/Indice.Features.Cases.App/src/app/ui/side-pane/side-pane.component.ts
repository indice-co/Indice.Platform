import { Component, inject, input, output } from '@angular/core';
import { Router } from '@angular/router';
import { NgIcon } from '@ng-icons/core';
import { TranslatePipe } from '@ngx-translate/core';

/**
 * Layout of a page routed into the `rightpane` outlet: a titled sheet with an optional confirm / cancel footer.
 * Closing (✕, cancel, or confirm with `closeOnOk`) removes the outlet and leaves the page underneath untouched.
 */
@Component({
  selector: 'app-side-pane',
  imports: [NgIcon, TranslatePipe],
  template: `
    <header class="flex items-start gap-3 border-b border-base-300 px-5 py-4">
      <div class="min-w-0 flex-1">
        <h2 class="text-lg font-semibold" id="side-pane-title">{{ title() }}</h2>
        @if (subtitle()) {
          <p class="mt-0.5 text-quiet">{{ subtitle() }}</p>
        }
      </div>
      <button type="button" class="btn btn-ghost btn-sm btn-square -mr-1" (click)="dismiss()" [attr.aria-label]="'common.close' | translate">
        <ng-icon name="lucideX" size="1.15rem" />
      </button>
    </header>
    <div class="min-h-0 flex-1 overflow-y-auto px-5 py-5">
      <ng-content />
    </div>
    @if (okShow() || cancelShow()) {
      <footer class="flex justify-end gap-2 border-t border-base-300 bg-base-100 px-5 py-3">
        @if (cancelShow()) {
          <button type="button" class="btn btn-ghost" (click)="dismiss()">{{ (cancelLabel() ?? 'common.cancel') | translate }}</button>
        }
        @if (okShow()) {
          <button type="button" class="btn btn-primary" [disabled]="disabled()" (click)="confirm()">{{ okLabel() ?? ('common.save' | translate) }}</button>
        }
      </footer>
    }
  `,
  host: { class: 'flex min-h-0 flex-1 flex-col', 'role': 'dialog', 'aria-labelledby': 'side-pane-title' }
})
export class SidePaneComponent {
  private readonly router = inject(Router);
  public readonly title = input<string | null | undefined>('');
  public readonly subtitle = input<string | null | undefined>();
  public readonly okLabel = input<string | null | undefined>();
  public readonly cancelLabel = input<string | undefined>();
  public readonly okShow = input(true);
  public readonly cancelShow = input(true);
  public readonly disabled = input(false);
  /** Close the pane after emitting `ok`. */
  public readonly closeOnOk = input(true);
  public readonly ok = output<void>();
  public readonly cancel = output<void>();

  protected confirm(): void {
    this.ok.emit();
    if (this.closeOnOk()) {
      this.close();
    }
  }

  protected dismiss(): void {
    this.cancel.emit();
    this.close();
  }

  public close(): void {
    this.router.navigate([{ outlets: { rightpane: null } }], { queryParamsHandling: 'preserve' });
  }
}
