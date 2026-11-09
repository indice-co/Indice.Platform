import { Component, inject, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { CasesApiService, SaveQueryRequest } from 'src/app/core/services/cases-api.service';
import { DIALOG_DATA, DialogRef } from 'src/app/ui/dialog/dialog-ref';

export interface QueryModalData {
  /** Query string (starting with `?`) of the cases list search to save. */
  parameters: string;
}

/** Names and saves the current cases search. Closes with `true` once saved. */
@Component({
  selector: 'app-query-modal',
  imports: [TranslatePipe],
  template: `
    <form (submit)="$event.preventDefault(); save()">
      <h3 class="text-lg font-semibold">{{ 'cases.saveSearch' | translate }}</h3>
      <p class="mt-1 text-sm text-base-content/70">{{ 'modals.query.description' | translate }}</p>
      <label class="mt-4 block">
        <span class="mb-1 block text-sm font-medium">{{ 'modals.query.nameLabel' | translate }}</span>
        <input type="text" class="input w-full" name="friendlyName" maxlength="200" autofocus
          [placeholder]="'modals.query.namePlaceholder' | translate" [value]="name()" (input)="name.set($any($event.target).value)" />
      </label>
      <div class="modal-action">
        <button type="button" class="btn btn-ghost" (click)="ref.close(false)">{{ 'common.cancel' | translate }}</button>
        <button type="submit" class="btn btn-primary" [disabled]="!name().trim() || saving()">
          @if (saving()) { <span class="loading loading-spinner loading-xs"></span> }
          {{ 'common.save' | translate }}
        </button>
      </div>
    </form>
  `
})
export class QueriesModalComponent {
  private readonly _api = inject(CasesApiService);
  private readonly _data = inject<QueryModalData>(DIALOG_DATA);
  protected readonly ref = inject<DialogRef<boolean>>(DialogRef);
  protected readonly name = signal('');
  protected readonly saving = signal(false);

  protected save(): void {
    if (!this.name().trim()) {
      return;
    }
    this.saving.set(true);
    this._api.saveQuery(new SaveQueryRequest({ friendlyName: this.name().trim(), parameters: this._data.parameters }))
      .subscribe({
        next: () => this.ref.close(true),
        error: () => this.saving.set(false)
      });
  }
}
