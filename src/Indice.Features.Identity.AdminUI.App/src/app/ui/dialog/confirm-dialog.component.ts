import { Component, inject } from '@angular/core';
import { LucideTrash, LucideTriangleAlert } from '@lucide/angular';
import { DIALOG_DATA, DialogRef } from './dialog-ref';

export interface ConfirmDialogData {
  title: string;
  message?: string;
  /** Defaults to `Confirm`. */
  confirmLabel?: string;
  /** Defaults to `Cancel`. */
  cancelLabel?: string;
  /** `danger` styles the confirm button as destructive. */
  tone?: 'default' | 'danger';
}

@Component({
  selector: 'app-confirm-dialog',
  imports: [LucideTrash, LucideTriangleAlert],
  template: `
    <div class="flex gap-4">
      <div class="grid size-10 shrink-0 place-items-center rounded-full"
        [class]="data.tone === 'danger' ? 'bg-error/10 text-error' : 'bg-warning/15 text-warning'">
        @if (data.tone === 'danger') { <svg lucideTrash class="size-5"></svg> } @else { <svg lucideTriangleAlert class="size-5"></svg> }
      </div>
      <div class="min-w-0 space-y-1">
        <h3 class="text-lg font-semibold">{{ data.title }}</h3>
        @if (data.message) {
          <p class="text-sm break-words text-base-content/75">{{ data.message }}</p>
        }
      </div>
    </div>
    <div class="modal-action">
      <button type="button" class="btn btn-ghost" (click)="ref.close(false)">{{ data.cancelLabel ?? 'Cancel' }}</button>
      <button type="button" class="btn" [class]="data.tone === 'danger' ? 'btn-error' : 'btn-primary'" (click)="ref.close(true)">
        {{ data.confirmLabel ?? 'Confirm' }}
      </button>
    </div>
  `
})
export class ConfirmDialogComponent {
  protected readonly data = inject<ConfirmDialogData>(DIALOG_DATA);
  protected readonly ref = inject<DialogRef<boolean>>(DialogRef);
}
