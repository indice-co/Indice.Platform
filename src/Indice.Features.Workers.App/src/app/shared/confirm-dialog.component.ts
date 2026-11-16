import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  input,
  output,
  viewChild,
} from '@angular/core';
import { LucideInfo, LucideTriangleAlert } from '@lucide/angular';

/**
 * Confirmation for destructive or bulk actions. The page holds a `viewChild` to it, calls `open()`,
 * and acts on `confirmed`. A native `<dialog>` so focus trapping and Escape come for free.
 */
@Component({
  selector: 'app-confirm-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideInfo, LucideTriangleAlert],
  template: `
    <dialog #dialog class="modal">
      <div class="modal-box max-w-sm">
        <div class="flex items-start gap-3">
          <span
            class="grid size-10 shrink-0 place-items-center rounded-full"
            [class]="destructive() ? 'bg-error/10 text-error' : 'bg-info/10 text-info'"
            aria-hidden="true"
          >
            @if (destructive()) {
              <svg lucideTriangleAlert class="size-5"></svg>
            } @else {
              <svg lucideInfo class="size-5"></svg>
            }
          </span>
          <div class="min-w-0">
            <h3 class="text-lg font-semibold text-base-content">{{ title() }}</h3>
            <p class="mt-1 text-sm text-base-content/75">{{ message() }}</p>
          </div>
        </div>
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

  /** An error-coloured confirm gets the warning icon; anything else the neutral one. */
  protected readonly destructive = computed(() => this.confirmClass().includes('btn-error'));

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
