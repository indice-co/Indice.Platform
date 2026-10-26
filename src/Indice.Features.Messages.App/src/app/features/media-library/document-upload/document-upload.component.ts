import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { catchError, finalize, from, map, mergeMap, of } from 'rxjs';
import { LucideFolder, LucideInfo } from '@lucide/angular';
import { settings } from 'src/app/core/models/settings';
import { FileParameter, MediaApiClient } from 'src/app/core/services/media-api.service';
import { MultiFileUploadComponent, UploadItem, UploadStatus } from 'src/app/shared/components/multi-file-upload/multi-file-upload.component';
import { AppTranslatedToaster, ToastType } from 'src/app/shared/services/app-translated-toaster';
import { SidePaneLayoutComponent, SidePaneService } from 'src/app/ui/side-pane/side-pane';
import { MediaLibraryStore } from '../media-library-store.service';

/** Number of files uploaded in parallel. */
const UPLOAD_CONCURRENCY = 3;

/** Side pane that uploads files to the folder in the `folderId` matrix param (library root when absent). */
@Component({
  selector: 'app-document-upload',
  imports: [TranslatePipe, SidePaneLayoutComponent, MultiFileUploadComponent, LucideFolder, LucideInfo],
  template: `
    <app-side-pane-layout [title]="'MediaLibrary.AddFileDialogTitle' | translate" [subtitle]="'MediaLibrary.AddFileIntro' | translate">
      <div class="space-y-5">
        <div class="flex items-center gap-2 text-sm">
          <svg lucideFolder class="size-4 shrink-0 text-warning"></svg>
          <span class="text-base-content/70">{{ 'MediaLibrary.Upload.Destination' | translate }}</span>
          <span class="truncate font-semibold">{{ folderName() || ('MediaLibrary.Title' | translate) }}</span>
        </div>

        <app-multi-file-upload [(items)]="items" [maxSize]="settings.maxFileSize" [accept]="settings.acceptableFileExtensions" [disabled]="uploading()" />

        <div role="note" class="alert alert-soft alert-info items-start text-sm">
          <svg lucideInfo class="mt-0.5 size-4 shrink-0"></svg>
          <span>
            {{ 'MediaLibrary.AttachmentsUsagePrefix' | translate }}
            <strong>{{ 'MediaLibrary.ChannelInbox' | translate }}</strong>
            {{ 'MediaLibrary.AttachmentsUsageConnectorOr' | translate }}
            <strong>{{ 'MediaLibrary.ChannelEmail' | translate }}</strong>
            {{ 'MediaLibrary.AttachmentsUsageSuffix' | translate }}
          </span>
        </div>
      </div>
      <div footer class="flex justify-end gap-2 border-t border-base-300 px-6 py-4">
        <button type="button" class="btn btn-ghost" (click)="pane.close()">{{ 'MediaLibrary.Close' | translate }}</button>
        <button type="button" class="btn btn-primary" [disabled]="!queuedCount() || uploading()" (click)="upload()">
          @if (uploading()) { <span class="loading loading-spinner loading-sm"></span> }
          {{ 'MediaLibrary.Upload.Submit' | translate: { count: queuedCount() } }}
        </button>
      </div>
    </app-side-pane-layout>
  `
})
export class DocumentUploadComponent implements OnInit {
  private readonly _api = inject(MediaApiClient);
  private readonly _store = inject(MediaLibraryStore);
  private readonly _toaster = inject(AppTranslatedToaster);
  protected readonly pane = inject(SidePaneService);
  protected readonly settings = settings;

  /** Matrix param: the folder the files are uploaded to. */
  public readonly folderId = input<string>();

  protected readonly items = signal<UploadItem[]>([]);
  protected readonly uploading = signal(false);
  protected readonly folderName = signal<string | undefined>(undefined);
  /** Files waiting to be uploaded (new ones and ones whose upload failed). */
  protected readonly queuedCount = computed(() => this.items().filter(item => item.status === 'pending' || item.status === 'error').length);

  public ngOnInit(): void {
    const folderId = this.folderId();
    if (folderId) {
      this._api.getFolderById(folderId).subscribe(folder => this.folderName.set(folder.name));
    }
  }

  protected upload(): void {
    const queue = this.items().filter(item => item.status === 'pending' || item.status === 'error');
    if (!queue.length) {
      return;
    }
    let uploaded = 0;
    this.uploading.set(true);
    from(queue)
      .pipe(
        mergeMap(item => {
          this._setStatus(item.key, 'uploading');
          const file: FileParameter = { fileName: item.attachment.title ?? 'file', data: item.attachment.data };
          return this._api.uploadFile(this.folderId() || undefined, [file]).pipe(
            map(() => ({ key: item.key, ok: true })),
            catchError(() => of({ key: item.key, ok: false }))
          );
        }, UPLOAD_CONCURRENCY),
        finalize(() => {
          this.uploading.set(false);
          if (uploaded > 0) {
            this._toaster.show(ToastType.Success, 'MediaLibrary.AttachmentUpdateSuccessTitle', 'MediaLibrary.Upload.SuccessMessage', undefined, { count: uploaded });
            this._store.notifyChanged();
          }
          if (uploaded === queue.length) {
            this.pane.close();
          }
        })
      )
      .subscribe(result => {
        if (result.ok) {
          uploaded++;
        }
        this._setStatus(result.key, result.ok ? 'done' : 'error');
      });
  }

  private _setStatus(key: number, status: UploadStatus): void {
    this.items.update(items => items.map(item => item.key === key ? { ...item, status } : item));
  }
}
