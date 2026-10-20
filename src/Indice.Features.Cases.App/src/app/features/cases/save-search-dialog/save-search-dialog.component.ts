import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgIcon } from '@ng-icons/core';
import { TranslatePipe } from '@ngx-translate/core';
import { CasesApiService, SaveQueryRequest } from 'src/app/core/services/cases-api.service';
import { DIALOG_DATA, DialogRef } from 'src/app/ui/feedback/dialog.service';

export interface SaveSearchDialogData {
  /** The list query string to store (page is always 1). */
  parameters: string;
}

/** Names the current case-list filters and stores them as a saved search ("My searches"). */
@Component({
  selector: 'app-save-search-dialog',
  imports: [FormsModule, NgIcon, TranslatePipe],
  template: `
    <form (ngSubmit)="save()">
      <div class="flex gap-4">
        <span class="grid size-10 shrink-0 place-items-center rounded-full bg-accent text-accent-content">
          <ng-icon name="lucideBookmarkPlus" size="1.2rem" />
        </span>
        <div class="min-w-0 flex-1">
          <h3 class="text-base font-semibold">{{ 'cases.saveSearch' | translate }}</h3>
          <p class="mt-1 text-quiet">{{ 'modals.query.description' | translate }}</p>
          <label class="mt-4 block">
            <span class="mb-1 block text-[0.8125rem] font-medium">{{ 'modals.query.nameLabel' | translate }}</span>
            <input class="input w-full" name="friendlyName" autocomplete="off" required maxlength="200"
                   [placeholder]="'modals.query.namePlaceholder' | translate" [(ngModel)]="friendlyName" />
          </label>
        </div>
      </div>
      <div class="modal-action">
        <button type="button" class="btn btn-ghost" (click)="ref.close(false)">{{ 'common.cancel' | translate }}</button>
        <button type="submit" class="btn btn-primary" [disabled]="!friendlyName || saving()">
          @if (saving()) { <span class="loading loading-spinner loading-xs"></span> }
          {{ 'cases.saveSearch' | translate }}
        </button>
      </div>
    </form>
  `
})
export class SaveSearchDialogComponent {
  private readonly api = inject(CasesApiService);
  protected readonly data = inject(DIALOG_DATA) as SaveSearchDialogData;
  protected readonly ref = inject(DialogRef) as DialogRef<boolean>;
  protected readonly saving = signal(false);
  protected friendlyName = '';

  protected save(): void {
    if (!this.friendlyName) {
      return;
    }
    this.saving.set(true);
    this.api.saveQuery(new SaveQueryRequest({ friendlyName: this.friendlyName, parameters: this.data.parameters }))
      .subscribe({
        next: () => this.ref.close(true),
        error: () => this.saving.set(false)
      });
  }
}
