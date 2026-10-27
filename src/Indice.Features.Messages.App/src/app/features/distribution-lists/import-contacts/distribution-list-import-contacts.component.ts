import { Component, inject, input, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideTriangleAlert } from '@lucide/angular';
import { ContactsImportResult, FileParameter, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { FileUploadComponent, IAttachment } from 'src/app/shared/components/file-upload/file-upload.component';
import { AppTranslatedToaster, ToastType } from 'src/app/shared/services/app-translated-toaster';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { SidePaneLayoutComponent, SidePaneService } from 'src/app/ui/side-pane/side-pane';

/** Side pane that bulk imports contacts from a CSV file into the distribution list in the `distributionListId` matrix param. */
@Component({
  selector: 'app-distribution-list-import-contacts',
  imports: [TranslatePipe, SidePaneLayoutComponent, FileUploadComponent, LucideTriangleAlert],
  template: `
    <app-side-pane-layout [title]="'DistributionLists.ImportContactsTitle' | translate">
      @if (result(); as result) {
        <div role="alert" class="alert alert-warning alert-soft items-start">
          <svg lucideTriangleAlert class="size-5"></svg>
          <div class="min-w-0">
            <p class="font-semibold">{{ 'DistributionLists.ImportCompletedWithErrors' | translate: { added: result.contactsAdded ?? 0, updated: result.contactsUpdated ?? 0 } }}</p>
            <ul class="mt-2 list-disc space-y-0.5 pl-4 text-sm">
              @for (error of result.errors; track $index) { <li class="break-words">{{ error }}</li> }
            </ul>
          </div>
        </div>
      } @else {
        <fieldset class="fieldset">
          <legend class="fieldset-legend whitespace-normal">{{ 'DistributionLists.ImportContactsLabel' | translate }}</legend>
          <app-file-upload accept=".csv,text/csv" [extendedView]="true" [maxSize]="maxFileSize" (fileChange)="onFileChange($event)" />
        </fieldset>
      }
      <div footer class="flex justify-end gap-2 border-t border-base-300 px-6 py-4">
        @if (result()) {
          <button type="button" class="btn btn-primary" (click)="pane.close()">{{ 'Common.Close' | translate }}</button>
        } @else {
          <button type="button" class="btn btn-ghost" (click)="pane.close()">{{ 'DistributionLists.CancelLabel' | translate }}</button>
          <button type="button" class="btn btn-primary" [disabled]="!attachment() || saving()" (click)="submit()">
            @if (saving()) { <span class="loading loading-spinner loading-sm"></span> }
            {{ 'DistributionLists.ImportContacts' | translate }}
          </button>
        }
      </div>
    </app-side-pane-layout>
  `
})
export class DistributionListImportContactsComponent {
  private readonly _api = inject(MessagesApiClient);
  private readonly _toaster = inject(AppTranslatedToaster);
  private readonly _refresh = inject(RefreshBus);
  protected readonly pane = inject(SidePaneService);

  /** Matrix param of the `import-contacts` pane route. */
  public readonly distributionListId = input.required<string>();

  protected readonly maxFileSize = 6 * 1024 * 1024;
  protected readonly attachment = signal<FileParameter | undefined>(undefined);
  protected readonly saving = signal(false);
  /** Set when the import finished but some rows were rejected. */
  protected readonly result = signal<ContactsImportResult | undefined>(undefined);

  protected onFileChange(file: IAttachment | undefined): void {
    this.attachment.set(file ? { fileName: file.title ?? 'contacts.csv', data: file.data } : undefined);
  }

  protected submit(): void {
    const attachment = this.attachment();
    if (!attachment) {
      return;
    }
    this.saving.set(true);
    this._api.bulkImportContactsToDistributionList(this.distributionListId(), attachment).subscribe({
      next: result => {
        this._refresh.notify('distribution-list-contacts');
        if (result.errors?.length) {
          this.result.set(result);
          this.saving.set(false);
          return;
        }
        this._toaster.show(ToastType.Success, 'DistributionLists.EditImportContactsSuccessTitle', 'DistributionLists.EditImportContactsSuccessMessage');
        this.pane.close();
      },
      error: () => this.saving.set(false)
    });
  }
}
