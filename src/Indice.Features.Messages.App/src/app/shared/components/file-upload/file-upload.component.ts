import { Component, ElementRef, OnInit, computed, input, output, signal, viewChild } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideFileUp, LucidePaperclip, LucideX } from '@lucide/angular';

export interface IAttachment {
  id?: string;
  title?: string;
  data?: Blob;
  contentType?: string;
  size?: number;
  sizeText?: string;
}

/**
 * Single file picker. Emits the picked file as an {@link IAttachment} (or `undefined` when removed).
 * `existingFile` seeds the control; `reset()` restores it. `extendedView` also shows the file's type and size.
 */
@Component({
  selector: 'app-file-upload',
  imports: [TranslatePipe, LucideFileUp, LucidePaperclip, LucideX],
  host: { class: 'block' },
  template: `
    <input #fileInput type="file" class="hidden" [accept]="accept()" (change)="onFilesAdded()" />
    @if (file(); as current) {
      <div class="flex items-center gap-3 rounded-field border border-base-300 bg-base-100 px-3 py-2">
        <svg lucidePaperclip class="size-4 shrink-0 text-base-content/60"></svg>
        <div class="min-w-0 flex-1">
          <p class="truncate text-sm font-medium">{{ current.title }}</p>
          @if (extendedView()) {
            <p class="text-xs text-base-content/60">{{ current.contentType }} · {{ current.sizeText }}</p>
          }
        </div>
        <button type="button" class="btn btn-ghost btn-xs btn-square text-error" (click)="removeFile()"
          [attr.aria-label]="'Shared.RemoveActionLabel' | translate">
          <svg lucideX class="size-4"></svg>
        </button>
      </div>
    } @else {
      <button type="button" (click)="clickElement()"
        class="flex w-full items-center gap-3 rounded-field border border-dashed border-base-300 bg-base-100 px-3 py-3 text-left transition-colors hover:border-primary hover:bg-primary/5">
        <span class="grid size-9 shrink-0 place-items-center rounded-full bg-primary/10 text-primary">
          <svg lucideFileUp class="size-4"></svg>
        </span>
        <span class="min-w-0">
          <span class="block text-sm font-semibold">{{ 'Shared.AddAttachmentButtonTitle' | translate }}</span>
          <span class="block text-xs text-base-content/60">
            {{ 'Shared.NoAttachmentInfo' | translate }}
            @if (maxSizeText()) { · {{ 'Shared.MaxFileSize' | translate: { size: maxSizeText() } }} }
          </span>
        </span>
      </button>
    }
    @if (sizeLimitReached()) {
      <p class="mt-2 text-sm text-error">{{ 'Shared.FileSizeLimitExceeded' | translate: { size: maxSizeText() } }}</p>
    }
  `
})
export class FileUploadComponent implements OnInit {
  private readonly _fileInput = viewChild.required<ElementRef<HTMLInputElement>>('fileInput');

  public readonly maxSize = input<number | undefined>();
  public readonly accept = input('');
  public readonly extendedView = input(false);
  public readonly existingFile = input<IAttachment | undefined>();
  public readonly fileChange = output<IAttachment | undefined>();

  protected readonly file = signal<IAttachment | undefined>(undefined);
  protected readonly sizeLimitReached = signal(false);
  protected readonly maxSizeText = computed(() => {
    const max = this.maxSize();
    return max ? `${Math.round((max / 1024 / 1024) * 10) / 10} MB` : '';
  });

  public ngOnInit(): void {
    this.reset();
  }

  public clickElement(): void {
    this.sizeLimitReached.set(false);
    this._fileInput().nativeElement.click();
  }

  public removeFile(): void {
    this._fileInput().nativeElement.value = '';
    this.file.set(undefined);
    this.fileChange.emit(undefined);
  }

  /** Restores the initial state: the existing file when there is one, otherwise nothing. */
  public reset(): void {
    const existing = this.existingFile();
    if (!existing) {
      this._fileInput().nativeElement.value = '';
    }
    this.file.set(existing ? { ...existing } : undefined);
  }

  protected onFilesAdded(): void {
    const picked = this._fileInput().nativeElement.files?.[0];
    if (!picked) {
      return;
    }
    const max = this.maxSize();
    if (max && picked.size > max) {
      this.sizeLimitReached.set(true);
      return;
    }
    const attachment: IAttachment = {
      title: picked.name,
      data: picked,
      contentType: picked.type || 'unknown',
      size: picked.size,
      sizeText: `${Math.round((picked.size / 1024) * 10) / 10} KB`
    };
    this.file.set(attachment);
    this.fileChange.emit(attachment);
  }
}
