import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { catchError, map, of, switchMap } from 'rxjs';
import { CreateDistributionListRequest, DistributionList, FileParameter, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { FileUploadComponent, IAttachment } from 'src/app/shared/components/file-upload/file-upload.component';
import { AppTranslatedToaster, ToastType } from 'src/app/shared/services/app-translated-toaster';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { SidePaneLayoutComponent, SidePaneService } from 'src/app/ui/side-pane/side-pane';

/** Side pane form that creates a distribution list and optionally imports its contacts from a CSV file. */
@Component({
  selector: 'app-distribution-list-create',
  imports: [ReactiveFormsModule, TranslatePipe, SidePaneLayoutComponent, FileUploadComponent],
  template: `
    <app-side-pane-layout [title]="'DistributionLists.CreateTitle' | translate">
      <form id="distribution-list-create-form" class="space-y-4" [formGroup]="form" (ngSubmit)="submit()">
        <fieldset class="fieldset">
          <label class="fieldset-legend" for="distribution-list-name">{{ 'DistributionLists.NameLabelRequired' | translate }}</label>
          <input id="distribution-list-name" class="input w-full" formControlName="name" maxlength="128" [placeholder]="'DistributionLists.NamePlaceholder' | translate"
            [class.input-error]="form.controls.name.invalid && form.controls.name.touched" />
          @if (form.controls.name.touched && form.controls.name.hasError('required')) {
            <p class="label text-error">{{ 'DistributionLists.NameRequiredError' | translate }}</p>
          } @else if (form.controls.name.hasError('maxlength')) {
            <p class="label text-error">{{ 'DistributionLists.NameMaxLengthError' | translate }}</p>
          } @else {
            <p class="label whitespace-normal">{{ 'DistributionLists.NameInfo' | translate }}</p>
          }
        </fieldset>

        <fieldset class="fieldset">
          <label class="fieldset-legend" for="distribution-list-alias">{{ 'DistributionLists.AliasLabelRequired' | translate }}</label>
          <input id="distribution-list-alias" class="input w-full font-mono" formControlName="alias" maxlength="64" [placeholder]="'DistributionLists.AliasPlaceholder' | translate"
            [class.input-error]="form.controls.alias.invalid && form.controls.alias.touched" />
          @if (form.controls.alias.hasError('maxlength')) {
            <p class="label text-error">{{ 'DistributionLists.AliasMaxLengthError' | translate }}</p>
          }
        </fieldset>

        <fieldset class="fieldset">
          <legend class="fieldset-legend">{{ 'DistributionLists.CsvImportLabel' | translate }}</legend>
          <app-file-upload accept=".csv,text/csv" [extendedView]="true" [maxSize]="maxFileSize" (fileChange)="onFileChange($event)" />
        </fieldset>
      </form>
      <div footer class="flex justify-end gap-2 border-t border-base-300 px-6 py-4">
        <button type="button" class="btn btn-ghost" (click)="pane.close()">{{ 'DistributionLists.CancelLabel' | translate }}</button>
        <button type="submit" form="distribution-list-create-form" class="btn btn-primary" [disabled]="form.invalid || saving()">
          @if (saving()) { <span class="loading loading-spinner loading-sm"></span> }
          {{ 'DistributionLists.Create' | translate }}
        </button>
      </div>
    </app-side-pane-layout>
  `
})
export class DistributionListCreateComponent {
  private readonly _api = inject(MessagesApiClient);
  private readonly _toaster = inject(AppTranslatedToaster);
  private readonly _refresh = inject(RefreshBus);
  protected readonly pane = inject(SidePaneService);

  protected readonly maxFileSize = 6 * 1024 * 1024;
  protected readonly saving = signal(false);
  private _attachment: FileParameter | undefined;
  protected readonly form = inject(NonNullableFormBuilder).group({
    name: ['', [Validators.required, Validators.maxLength(128)]],
    alias: ['', [Validators.required, Validators.maxLength(64)]]
  });

  protected onFileChange(file: IAttachment | undefined): void {
    this._attachment = file ? { fileName: file.title ?? 'contacts.csv', data: file.data } : undefined;
  }

  /** Creates the list first; when that succeeds, imports the contacts of the optional CSV file. */
  protected submit(): void {
    if (this.form.invalid) {
      return;
    }
    const attachment = this._attachment;
    this.saving.set(true);
    this._api
      .createDistributionList(new CreateDistributionListRequest(this.form.getRawValue()))
      .pipe(
        switchMap((list: DistributionList) => !attachment
          ? of(list)
          : this._api.bulkImportContactsToDistributionList(list.id!, attachment).pipe(
            map(() => list),
            // the list exists even if the import failed (the error is already shown); contacts can be imported later
            catchError(() => of(list))
          ))
      )
      .subscribe({
        next: list => {
          this._toaster.show(ToastType.Success, 'DistributionLists.CreateSuccessTitle', 'DistributionLists.CreateSuccessMessage', undefined, { name: list.name });
          this._refresh.notify('distribution-lists');
          this.pane.close();
        },
        error: () => this.saving.set(false)
      });
  }
}
