import { Component, DestroyRef, ElementRef, computed, inject, input, model, output, signal, viewChild } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideCircleAlert, LucideCircleCheck, LucideCloudUpload, LucideDynamicIcon, LucideX } from '@lucide/angular';
import { FileUtilitiesService } from 'src/app/shared/services/file-utilities.service';
import { IAttachment } from '../file-upload/file-upload.component';

export type { IAttachment } from '../file-upload/file-upload.component';

export type UploadStatus = 'pending' | 'uploading' | 'done' | 'error' | 'invalid';

/** A file picked in the {@link MultiFileUploadComponent} and its upload state (set by the host while uploading). */
export interface UploadItem {
  key: number;
  attachment: IAttachment;
  status: UploadStatus;
  /** Object URL of an image preview. */
  previewUrl?: string;
  /** Translation key describing why the file is invalid or failed to upload. */
  error?: string;
  errorParams?: Record<string, unknown>;
}

let nextKey = 1;

/**
 * Drag & drop multi file picker. Validates size (`maxSize`, bytes) and type (`accept`, e.g. `.png, .jpg` or `image/*`),
 * lists the picked files with previews and shows each file's upload status. The host owns the list through the
 * two-way `items` binding and updates each item's `status` while uploading; `filesChange` emits the valid pending files.
 */
@Component({
  selector: 'app-multi-file-upload',
  imports: [TranslatePipe, LucideCircleAlert, LucideCircleCheck, LucideCloudUpload, LucideDynamicIcon, LucideX],
  host: { class: 'block space-y-4' },
  template: `
    <input #fileInput type="file" class="hidden" multiple [accept]="accept()" (change)="onInputChange()" />
    <button type="button" [disabled]="disabled()" (click)="browse()"
      (dragenter)="onDragOver($event)" (dragover)="onDragOver($event)" (dragleave)="dragging.set(false)" (drop)="onDrop($event)"
      class="flex w-full flex-col items-center gap-3 rounded-box border-2 border-dashed px-6 py-10 text-center transition-colors disabled:cursor-not-allowed disabled:opacity-60"
      [class]="dragging() ? 'border-primary bg-primary/10' : 'border-base-300 bg-base-200/40 hover:border-primary hover:bg-primary/5'">
      <span class="grid size-12 place-items-center rounded-full bg-primary/10 text-primary">
        <svg lucideCloudUpload class="size-6"></svg>
      </span>
      <span class="space-y-1">
        <span class="block font-semibold">{{ 'MediaLibrary.Upload.DropTitle' | translate }}</span>
        <span class="block text-sm text-base-content/70">{{ 'MediaLibrary.Upload.DropHint' | translate }}</span>
      </span>
      @if (acceptText() || maxSizeText()) {
        <span class="flex flex-wrap justify-center gap-1.5 text-xs text-base-content/60">
          @if (acceptText()) { <span class="badge badge-ghost badge-sm">{{ acceptText() }}</span> }
          @if (maxSizeText()) { <span class="badge badge-ghost badge-sm">{{ 'Shared.MaxFileSize' | translate: { size: maxSizeText() } }}</span> }
        </span>
      }
    </button>

    @if (items().length) {
      <div class="flex items-center justify-between gap-2 text-sm">
        <span class="text-base-content/70 tabular">{{ 'MediaLibrary.Upload.Summary' | translate: { count: items().length, size: totalSizeText() } }}</span>
        @if (!busy()) {
          <button type="button" class="btn btn-ghost btn-xs" (click)="reset()">{{ 'MediaLibrary.Upload.RemoveAll' | translate }}</button>
        }
      </div>
      <ul class="space-y-2">
        @for (item of items(); track item.key) {
          <li class="flex items-center gap-3 rounded-box border bg-base-100 p-2.5"
            [class]="item.status === 'invalid' || item.status === 'error' ? 'border-error/40' : item.status === 'done' ? 'border-success/40' : 'border-base-300'">
            <span class="grid size-11 shrink-0 place-items-center overflow-hidden rounded-field bg-base-200 text-base-content/50">
              @if (item.previewUrl) {
                <img class="size-full object-cover" [src]="item.previewUrl" alt="" />
              } @else {
                <svg [lucideIcon]="icon(item)" class="size-5"></svg>
              }
            </span>
            <div class="min-w-0 flex-1">
              <p class="truncate text-sm font-medium" [title]="item.attachment.title">{{ item.attachment.title }}</p>
              @switch (item.status) {
                @case ('uploading') {
                  <progress class="progress progress-primary mt-1.5 h-1.5 w-full"></progress>
                }
                @case ('done') {
                  <p class="text-xs text-success">{{ 'MediaLibrary.Upload.Done' | translate }} · {{ item.attachment.sizeText }}</p>
                }
                @case ('pending') {
                  <p class="truncate text-xs text-base-content/60">{{ item.attachment.sizeText }} · {{ item.attachment.contentType }}</p>
                }
                @default {
                  <p class="text-xs text-error">{{ (item.error || 'MediaLibrary.Upload.Failed') | translate: item.errorParams }}</p>
                }
              }
            </div>
            @switch (item.status) {
              @case ('uploading') { <span class="loading loading-spinner loading-sm text-primary"></span> }
              @case ('done') { <svg lucideCircleCheck class="size-5 shrink-0 text-success"></svg> }
              @case ('pending') {}
              @default { <svg lucideCircleAlert class="size-5 shrink-0 text-error"></svg> }
            }
            @if (item.status !== 'uploading' && item.status !== 'done') {
              <button type="button" class="btn btn-ghost btn-xs btn-square" [disabled]="busy()" (click)="remove(item)"
                [attr.aria-label]="'Shared.RemoveActionLabel' | translate">
                <svg lucideX class="size-4"></svg>
              </button>
            }
          </li>
        }
      </ul>
    }
  `
})
export class MultiFileUploadComponent {
  private readonly _files = inject(FileUtilitiesService);
  private readonly _fileInput = viewChild.required<ElementRef<HTMLInputElement>>('fileInput');

  /** Maximum file size in bytes. */
  public readonly maxSize = input<number | undefined>();
  /** Accepted types, as in the `accept` attribute (`.png, .jpg`, `image/*`). Empty accepts everything. */
  public readonly accept = input<string | undefined>('');
  public readonly disabled = input(false);
  public readonly items = model<UploadItem[]>([]);
  public readonly filesChange = output<IAttachment[]>();

  protected readonly dragging = signal(false);
  protected readonly busy = computed(() => this.disabled() || this.items().some(item => item.status === 'uploading'));
  private readonly _acceptTokens = computed(() => (this.accept() ?? '').split(',').map(token => token.trim().toLowerCase()).filter(Boolean));
  protected readonly acceptText = computed(() => this._acceptTokens().map(token => token.replace(/^\./, '').toUpperCase()).join(', '));
  protected readonly maxSizeText = computed(() => this.maxSize() ? this._files.formatSize(this.maxSize()) : '');
  protected readonly totalSizeText = computed(() => this._files.formatSize(this.items().reduce((total, item) => total + (item.attachment.size ?? 0), 0)));

  constructor() {
    inject(DestroyRef).onDestroy(() => this.items().forEach(item => this._revoke(item)));
  }

  /** Removes every file. */
  public reset(): void {
    this.items().forEach(item => this._revoke(item));
    this._setItems([]);
  }

  protected browse(): void {
    this._fileInput().nativeElement.click();
  }

  protected onInputChange(): void {
    const input = this._fileInput().nativeElement;
    this._add(Array.from(input.files ?? []));
    input.value = '';
  }

  protected onDragOver(event: DragEvent): void {
    event.preventDefault();
    if (!this.busy()) {
      this.dragging.set(true);
    }
  }

  protected onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(false);
    if (!this.busy()) {
      this._add(Array.from(event.dataTransfer?.files ?? []));
    }
  }

  protected remove(item: UploadItem): void {
    this._revoke(item);
    this._setItems(this.items().filter(other => other.key !== item.key));
  }

  protected icon(item: UploadItem) {
    const name = item.attachment.title ?? '';
    const dot = name.lastIndexOf('.');
    return this._files.getFileIcon({ fileExtension: dot >= 0 ? name.substring(dot) : '' });
  }

  private _add(files: File[]): void {
    const existing = this.items();
    const added = files
      .filter(file => !existing.some(item => item.status !== 'done' && item.attachment.title === file.name && item.attachment.size === file.size))
      .map(file => this._toItem(file));
    if (added.length) {
      this._setItems([...existing, ...added]);
    }
  }

  private _toItem(file: File): UploadItem {
    const item: UploadItem = {
      key: nextKey++,
      status: 'pending',
      attachment: {
        title: file.name,
        data: file,
        contentType: file.type || 'unknown',
        size: file.size,
        sizeText: this._files.formatSize(file.size)
      }
    };
    const maxSize = this.maxSize();
    if (!this._isAccepted(file)) {
      return { ...item, status: 'invalid', error: 'MediaLibrary.Upload.InvalidType', errorParams: { types: this.acceptText() } };
    }
    if (maxSize && file.size > maxSize) {
      return { ...item, status: 'invalid', error: 'Shared.FileSizeLimitExceeded', errorParams: { size: this.maxSizeText() } };
    }
    return file.type.startsWith('image/') ? { ...item, previewUrl: URL.createObjectURL(file) } : item;
  }

  private _isAccepted(file: File): boolean {
    const tokens = this._acceptTokens();
    if (!tokens.length) {
      return true;
    }
    const name = file.name.toLowerCase();
    const type = (file.type || '').toLowerCase();
    return tokens.some(token => token.startsWith('.')
      ? name.endsWith(token)
      : token.endsWith('/*') ? type.startsWith(token.slice(0, -1)) : type === token);
  }

  private _setItems(items: UploadItem[]): void {
    this.items.set(items);
    this.filesChange.emit(items.filter(item => item.status === 'pending').map(item => item.attachment));
  }

  private _revoke(item: UploadItem): void {
    if (item.previewUrl) {
      URL.revokeObjectURL(item.previewUrl);
    }
  }
}
