import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  input,
  output,
  viewChild,
} from '@angular/core';

/**
 * Confirmation for destructive or bulk actions. The page holds a `viewChild` to it, calls `open()`,
 * and acts on `confirmed`. A native `<dialog>` so focus trapping and Escape come for free.
 */
@Component({
  selector: 'app-confirm-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <dialog #dialog class="modal">
      <div class="modal-box max-w-sm">
        <h3 class="text-lg font-semibold text-base-content">{{ title() }}</h3>
        <p class="mt-2 text-sm text-base-content/70">{{ message() }}</p>
        <div class="modal-action">
          <button type="button" class="btn btn-ghost" (click)="close()">Cancel</button>
          <button type="button" class="btn" [class]="confirmClass()" (click)="confirm()">
            {{ confirmLabel() }}
          </button>
        </div>
      </div>
      <form method="dialog" class="modal-backdrop">
        <button>close</button>
      </form>
    </dialog>
  `,
})
export class ConfirmDialogComponent {
  readonly title = input('Are you sure?');
  readonly message = input('');
  readonly confirmLabel = input('Confirm');
  /** daisyUI button variant of the confirm button. */
  readonly confirmClass = input('btn-error');

  readonly confirmed = output<void>();

  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');

  open(): void {
    this.dialog().nativeElement.showModal();
  }

  close(): void {
    this.dialog().nativeElement.close();
  }

  protected confirm(): void {
    this.confirmed.emit();
    this.close();
  }
}
