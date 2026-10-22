import { Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideTrash, LucideTriangleAlert } from '@lucide/angular';
import { DIALOG_DATA, DialogRef } from './dialog-ref';

export interface ConfirmDialogData {
  /** Translation key or text. */
  title: string;
  /** Translation key or text. */
  message?: string;
  messageParams?: Record<string, unknown>;
  /** Translation key or text for the confirm button. Defaults to `common.confirm`. */
  confirmLabel?: string;
  /** Translation key or text for the cancel button. Defaults to `common.cancel`. */
  cancelLabel?: string;
  /** `danger` styles the confirm button as destructive. */
  tone?: 'default' | 'danger';
}

@Component({
  selector: 'app-confirm-dialog',
  imports: [TranslatePipe, LucideTrash, LucideTriangleAlert],
  template: `
    <div class="flex gap-4">
      <div class="grid size-10 shrink-0 place-items-center rounded-full"
        [class]="data.tone === 'danger' ? 'bg-error/10 text-error' : 'bg-warning/15 text-warning'">
        @if (data.tone === 'danger') { <svg lucideTrash class="size-5"></svg> } @else { <svg lucideTriangleAlert class="size-5"></svg> }
      </div>
      <div class="space-y-1">
        <h3 class="text-lg font-semibold">{{ data.title | translate }}</h3>
        @if (data.message) {
          <p class="text-sm text-base-content/75">{{ data.message | translate: data.messageParams }}</p>
        }
      </div>
    </div>
    <div class="modal-action">
      <button type="button" class="btn btn-ghost" (click)="ref.close(false)">{{ (data.cancelLabel ?? 'common.cancel') | translate }}</button>
      <button type="button" class="btn" [class]="data.tone === 'danger' ? 'btn-error' : 'btn-primary'" (click)="ref.close(true)">
        {{ (data.confirmLabel ?? 'common.confirm') | translate }}
      </button>
    </div>
  `
})
export class ConfirmDialogComponent {
  protected readonly data = inject<ConfirmDialogData>(DIALOG_DATA);
  protected readonly ref = inject<DialogRef<boolean>>(DialogRef);
}
