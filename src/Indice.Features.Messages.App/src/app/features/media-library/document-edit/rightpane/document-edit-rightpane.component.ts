import { Component, OnInit, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';
import { MediaApiClient, MediaFile, MediaFolder, UpdateFileMetadataRequest } from 'src/app/core/services/media-api.service';
import { AppTranslatedToaster, ToastType } from 'src/app/shared/services/app-translated-toaster';
import { SidePaneLayoutComponent, SidePaneService } from 'src/app/ui/side-pane/side-pane';
import { MediaFileThumbComponent } from '../../item-views/media-item-parts';
import { MediaLibraryStore } from '../../media-library-store.service';

/** Side pane form that edits the metadata (name, description, folder) of the file in the `documentId` route param. */
@Component({
  selector: 'app-document-edit-rightpane',
  imports: [ReactiveFormsModule, TranslatePipe, SidePaneLayoutComponent, MediaFileThumbComponent],
  template: `
    <app-side-pane-layout [title]="'MediaLibrary.EditFileDialogTitle' | translate">
      @if (loading()) {
        <div class="space-y-4">
          @for (row of [1, 2, 3]; track row) { <div class="skeleton h-10 w-full"></div> }
        </div>
      } @else {
        @if (file(); as current) {
          <div class="mb-5 flex items-center gap-3 rounded-box border border-base-300 p-2.5">
            <app-media-file-thumb class="size-12 shrink-0 rounded-field" [file]="current" iconClass="size-6" />
            <div class="min-w-0 text-sm">
              <p class="truncate font-medium">{{ current.name }}</p>
              <p class="truncate text-xs text-base-content/60">{{ current.contentType }} · {{ current.contentSize }}</p>
            </div>
          </div>
        }
        <form id="file-form" class="space-y-4" [formGroup]="form" (ngSubmit)="submit()">
          <fieldset class="fieldset">
            <legend class="fieldset-legend">{{ 'MediaLibrary.FileNameLabelRequired' | translate }}</legend>
            <input class="input w-full" formControlName="name" maxlength="128" [placeholder]="'MediaLibrary.FileNamePlaceholder' | translate"
              [class.input-error]="form.controls.name.invalid && form.controls.name.touched" />
            @if (form.controls.name.touched && form.controls.name.hasError('required')) {
              <p class="label text-error">{{ 'MediaLibrary.FileNameRequiredError' | translate }}</p>
            } @else if (form.controls.name.hasError('maxlength')) {
              <p class="label text-error">{{ 'MediaLibrary.FileNameMaxLengthError' | translate }}</p>
            } @else {
              <p class="label whitespace-normal">{{ 'MediaLibrary.FileNameInfo' | translate }}</p>
            }
          </fieldset>

          <fieldset class="fieldset">
            <legend class="fieldset-legend">{{ 'MediaLibrary.FileDescriptionLabel' | translate }}</legend>
            <textarea class="textarea w-full" rows="3" formControlName="description" maxlength="512"
              [placeholder]="'MediaLibrary.FileDescriptionPlaceholder' | translate"></textarea>
            <p class="label whitespace-normal">{{ 'MediaLibrary.FileDescriptionInfo' | translate }}</p>
          </fieldset>

          <fieldset class="fieldset">
            <legend class="fieldset-legend">{{ 'MediaLibrary.FileFolderLabel' | translate }}</legend>
            <select class="select w-full" formControlName="folderId">
              <option value="">{{ 'MediaLibrary.Title' | translate }}</option>
              @for (folder of folders(); track folder.id) {
                <option [value]="folder.id">{{ folder.name }}</option>
              }
            </select>
            <p class="label whitespace-normal">{{ 'MediaLibrary.FileFolderInfo' | translate }}</p>
          </fieldset>
        </form>
      }
      <div footer class="flex justify-end gap-2 border-t border-base-300 px-6 py-4">
        <button type="button" class="btn btn-ghost" (click)="pane.close()">{{ 'MediaLibrary.Close' | translate }}</button>
        <button type="submit" form="file-form" class="btn btn-primary" [disabled]="form.invalid || saving() || loading()">
          @if (saving()) { <span class="loading loading-spinner loading-sm"></span> }
          {{ 'MediaLibrary.Save' | translate }}
        </button>
      </div>
    </app-side-pane-layout>
  `
})
export class DocumentEditRightpaneComponent implements OnInit {
  private readonly _api = inject(MediaApiClient);
  private readonly _store = inject(MediaLibraryStore);
  private readonly _toaster = inject(AppTranslatedToaster);
  protected readonly pane = inject(SidePaneService);

  /** Bound from the `edit-file/:documentId` route. */
  public readonly documentId = input.required<string>();

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly file = signal<MediaFile | undefined>(undefined);
  protected readonly folders = signal<MediaFolder[]>([]);
  protected readonly form = inject(NonNullableFormBuilder).group({
    name: ['', [Validators.required, Validators.maxLength(128)]],
    description: ['', Validators.maxLength(512)],
    folderId: ['']
  });

  public ngOnInit(): void {
    forkJoin([this._api.getFileDetails(this.documentId()), this._api.listFolders()]).subscribe({
      next: ([file, folders]) => {
        this.file.set(file);
        this.folders.set([...(folders ?? [])].sort((a, b) => (a.name ?? '').localeCompare(b.name ?? '')));
        this.form.setValue({ name: file.name ?? '', description: file.description ?? '', folderId: file.folderId ?? '' });
        this.loading.set(false);
      },
      error: () => this.pane.close()
    });
  }

  protected submit(): void {
    if (this.form.invalid) {
      return;
    }
    const value = this.form.getRawValue();
    const request = new UpdateFileMetadataRequest({ name: value.name, description: value.description || undefined, folderId: value.folderId || undefined });
    this.saving.set(true);
    this._api.updateFileMetadata(this.documentId(), request).subscribe({
      next: () => {
        this._toaster.show(ToastType.Success, 'MediaLibrary.UpdateFileSuccessTitle', 'MediaLibrary.UpdateFileSuccessMessage', undefined, { name: value.name });
        this._store.notifyChanged();
        this.pane.close();
      },
      error: () => this.saving.set(false)
    });
  }
}
