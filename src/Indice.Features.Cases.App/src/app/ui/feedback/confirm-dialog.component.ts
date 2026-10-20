import { Component, inject } from '@angular/core';
import { NgIcon } from '@ng-icons/core';
import { TranslatePipe } from '@ngx-translate/core';
import { DIALOG_DATA, DialogRef } from './dialog.service';

export interface ConfirmDialogData {
  /** Translation key or text. */
  title: string;
  /** Translation key or text. */
  message?: string;
  /** Emphasised value shown after the message, e.g. the code of the record being deleted. */
  subject?: string;
  confirmLabel?: string;
  cancelLabel?: string;
  /** `danger` for destructive actions, `warning` for irreversible but expected ones. */
  tone?: 'danger' | 'warning' | 'primary';
}

/** A yes/no question. Closes with `true` when confirmed. */
@Component({
  selector: 'app-confirm-dialog',
  imports: [NgIcon, TranslatePipe],
  template: `
    <div class="flex gap-4">
      @if (data.tone !== 'primary') {
        <span class="grid size-10 shrink-0 place-items-center rounded-full"
              [class]="data.tone === 'danger' ? 'bg-error/12 text-error' : 'bg-warning/20 text-warning-content'">
          <ng-icon [name]="data.tone === 'danger' ? 'lucideTrash2' : 'lucideTriangleAlert'" size="1.25rem" />
        </span>
      }
      <div class="min-w-0">
        <h3 class="text-base font-semibold" id="confirm-title">{{ data.title | translate }}</h3>
        @if (data.message) {
          <p class="mt-1.5 text-quiet">
            {{ data.message | translate }}
            @if (data.subject) {
              <strong class="text-base-content">{{ data.subject }}</strong>
            }
          </p>
        }
      </div>
    </div>
    <div class="modal-action">
      <button type="button" class="btn btn-ghost" (click)="ref.close(false)">{{ (data.cancelLabel ?? 'common.cancel') | translate }}</button>
      <button type="button" class="btn" [class]="data.tone === 'danger' ? 'btn-error' : 'btn-primary'" (click)="ref.close(true)">
        {{ (data.confirmLabel ?? 'common.yes') | translate }}
      </button>
    </div>
  `,
  host: { 'role': 'alertdialog', '[attr.aria-labelledby]': '"confirm-title"' }
})
export class ConfirmDialogComponent {
  protected readonly data = inject(DIALOG_DATA) as ConfirmDialogData;
  protected readonly ref = inject(DialogRef) as DialogRef<boolean>;
}
