import { Component, OnInit, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { Observable, forkJoin, of } from 'rxjs';
import { CreateFolderRequest, MediaApiClient, MediaFolder, UpdateFolderRequest } from 'src/app/core/services/media-api.service';
import { AppTranslatedToaster, ToastType } from 'src/app/shared/services/app-translated-toaster';
import { SidePaneLayoutComponent, SidePaneService } from 'src/app/ui/side-pane/side-pane';
import { MediaLibraryStore } from '../media-library-store.service';

/**
 * Side pane form that creates a folder (inside the folder of the `parentId` matrix param) or edits the folder of the
 * `folderId` route param.
 */
@Component({
  selector: 'app-folder-edit',
  imports: [ReactiveFormsModule, TranslatePipe, SidePaneLayoutComponent],
  template: `
    <app-side-pane-layout [title]="(folderId() ? 'MediaLibrary.EditFolderDialogTitle' : 'MediaLibrary.CreateFolderDialogTitle') | translate">
      @if (loading()) {
        <div class="space-y-4">
          @for (row of [1, 2, 3]; track row) { <div class="skeleton h-10 w-full"></div> }
        </div>
      } @else {
        <form id="folder-form" class="space-y-4" [formGroup]="form" (ngSubmit)="submit()">
          <fieldset class="fieldset">
            <legend class="fieldset-legend">{{ 'MediaLibrary.FolderNameLabelRequired' | translate }}</legend>
            <input class="input w-full" formControlName="name" maxlength="128" [placeholder]="'MediaLibrary.FolderNamePlaceholder' | translate"
              [class.input-error]="form.controls.name.invalid && form.controls.name.touched" />
            @if (form.controls.name.touched && form.controls.name.hasError('required')) {
              <p class="label text-error">{{ 'MediaLibrary.FolderNameRequiredError' | translate }}</p>
            } @else if (form.controls.name.hasError('maxlength')) {
              <p class="label text-error">{{ 'MediaLibrary.FolderNameMaxLengthError' | translate }}</p>
            } @else {
              <p class="label whitespace-normal">{{ 'MediaLibrary.FolderNameInfo' | translate }}</p>
            }
          </fieldset>

          <fieldset class="fieldset">
            <legend class="fieldset-legend">{{ 'MediaLibrary.FolderDescriptionLabel' | translate }}</legend>
            <textarea class="textarea w-full" rows="3" formControlName="description" maxlength="512"
              [placeholder]="'MediaLibrary.FolderDescriptionPlaceholder' | translate"></textarea>
            <p class="label whitespace-normal">{{ 'MediaLibrary.FolderDescriptionInfo' | translate }}</p>
          </fieldset>

          <fieldset class="fieldset">
            <legend class="fieldset-legend">{{ 'MediaLibrary.ParentFolderSelectLabel' | translate }}</legend>
            <select class="select w-full" formControlName="parentId">
              <option value="">{{ 'MediaLibrary.Title' | translate }}</option>
              @for (folder of folders(); track folder.id) {
                <option [value]="folder.id">{{ folder.name }}</option>
              }
            </select>
            <p class="label whitespace-normal">{{ 'MediaLibrary.ParentFolderInfo' | translate }}</p>
          </fieldset>
        </form>
      }
      <div footer class="flex justify-end gap-2 border-t border-base-300 px-6 py-4">
        <button type="button" class="btn btn-ghost" (click)="pane.close()">{{ 'MediaLibrary.Close' | translate }}</button>
        <button type="submit" form="folder-form" class="btn btn-primary" [disabled]="form.invalid || saving() || loading()">
          @if (saving()) { <span class="loading loading-spinner loading-sm"></span> }
          {{ (folderId() ? 'MediaLibrary.Save' : 'MediaLibrary.Create') | translate }}
        </button>
      </div>
    </app-side-pane-layout>
  `
})
export class FolderEditComponent implements OnInit {
  private readonly _api = inject(MediaApiClient);
  private readonly _store = inject(MediaLibraryStore);
  private readonly _toaster = inject(AppTranslatedToaster);
  protected readonly pane = inject(SidePaneService);

  /** Bound from the `edit-folder/:folderId` route; absent when creating. */
  public readonly folderId = input<string>();
  /** Matrix param of `create-folder`: the folder the new folder is created in. */
  public readonly parentId = input<string>();

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly folders = signal<MediaFolder[]>([]);
  protected readonly form = inject(NonNullableFormBuilder).group({
    name: ['', [Validators.required, Validators.maxLength(128)]],
    description: ['', Validators.maxLength(512)],
    parentId: ['']
  });

  public ngOnInit(): void {
    const id = this.folderId();
    forkJoin([this._api.listFolders(), id ? this._api.getFolderById(id) : of(undefined)]).subscribe({
      next: ([folders, folder]) => {
        this.folders.set((folders ?? []).filter(f => f.id !== id).sort((a, b) => (a.name ?? '').localeCompare(b.name ?? '')));
        this.form.setValue({
          name: folder?.name ?? '',
          description: folder?.description ?? '',
          parentId: (folder ? folder.parentId : this.parentId()) ?? ''
        });
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
    const body = { name: value.name, description: value.description || undefined, parentId: value.parentId || undefined };
    const id = this.folderId();
    const request$: Observable<unknown> = id
      ? this._api.updateFolder(id, new UpdateFolderRequest(body))
      : this._api.createFolder(new CreateFolderRequest(body));
    this.saving.set(true);
    request$.subscribe({
      next: () => {
        this._toaster.show(ToastType.Success,
          id ? 'MediaLibrary.UpdateFolderSuccessTitle' : 'MediaLibrary.CreateFolderSuccessTitle',
          id ? 'MediaLibrary.UpdateFolderSuccessMessage' : 'MediaLibrary.CreateFolderSuccessMessage', undefined, { name: value.name });
        this._store.notifyChanged();
        this.pane.close();
      },
      error: () => this.saving.set(false)
    });
  }
}
