import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { tap } from 'rxjs/operators';
import { CasesApiService } from 'src/app/core/services/cases-api.service';
import { FileUploadService } from 'src/app/core/services/file-upload.service';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideDownload, LucidePaperclip, LucideTrash } from '@lucide/angular';
import { TranslatedToasterService, ToastType } from 'src/app/shared/services/translated-toaster.service';
import { FieldWrapperComponent } from './field-wrapper.component';
import { TailwindControl } from './tailwind-control';

/**
 * ajsf `file` widget. A selected file is kept in `FileUploadService.files`, keyed by the data path (eg `homePhone.attachmentId`),
 * and the field holds that path until the host uploads the file and replaces it with the attachment id.
 */
@Component({
    selector: 'app-jf-file',
    imports: [FieldWrapperComponent, TranslatePipe, LucideDownload, LucidePaperclip, LucideTrash],
    template: `
    @if (shown) {
      <app-jf-field [control]="this">
        @if (!isGuid(data)) {
          @if (!readonly) {
            <input
              type="file"
              [accept]="accept"
              [class]="fieldClass + ' max-w-md'"
              [attr.required]="options.required"
              [id]="id"
              (change)="onFileSelect($event)">
          } @else {
            <p class="text-sm text-base-content/60">{{ 'common.noDocumentSubmitted' | translate }}</p>
          }
        } @else {
          <div class="flex flex-wrap items-center gap-2 rounded-field border border-base-300 bg-base-200/50 px-3 py-2">
            <svg lucidePaperclip class="size-4 text-base-content/60"></svg>
            <span class="min-w-0 flex-1 truncate text-sm">{{ 'jsonforms.attachedFile' | translate }}</span>
            <button type="button" class="btn btn-sm" (click)="onDownload()">
              <svg lucideDownload class="size-4"></svg>
              {{ 'common.download' | translate }}
            </button>
            @if (!readonly) {
              <button type="button" class="btn btn-ghost btn-sm btn-square text-error" (click)="setValue(undefined)"
                [attr.aria-label]="'common.removeAttachment' | translate" [title]="'common.removeAttachment' | translate">
                <svg lucideTrash class="size-4"></svg>
              </button>
            }
          </div>
        }
      </app-jf-field>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager
})
export class FileControlComponent extends TailwindControl {
    protected widget = 'file';
    protected override fieldBase = 'file-input';
    override readonlyAsText = false;
    private toaster = inject(TranslatedToasterService);
    private api = inject(CasesApiService);
    private fileUploadService = inject(FileUploadService);

    get accept(): string {
        return this.options.accept !== undefined ? this.options.accept.join(', ') : '*.*';
    }

    onFileSelect(event: Event): void {
        const input = event.target as HTMLInputElement;
        const file = input.files?.[0];
        if (this.options.accept && file && !this.options.accept.some((fileExtension: string) => file.name.endsWith(fileExtension))) {
            input.value = '';
            this.setValue(undefined);
            // TODO: make this a validation error
            this.toaster.show(ToastType.Error, 'toasts.saveError.title', 'toasts.invalidFileType.body', 5000);
            return;
        }
        this.setValue(file ? this.propsPath : undefined);
        if (!this.fileUploadService.files) {
            this.fileUploadService.files = {};
        }
        if (file) {
            this.fileUploadService.files[this.propsPath] = file;
        }
    }

    onDownload(): void {
        this.api.downloadAttachment(this.data)
            .pipe(
                tap(results => {
                    const fileURL = window.URL.createObjectURL(results.data);
                    if (this.options.downloadToDisk) {
                        const a = document.createElement('a');
                        a.href = fileURL;
                        // we get the file name from the content-disposition header, so make sure its exposed
                        a.download = results.fileName ?? `${this.controlName}-${this.data}`;
                        a.click();
                        window.URL.revokeObjectURL(fileURL);
                    } else {
                        // open file in new tab to show the content
                        window.open(fileURL, '_blank');
                    }
                })
            )
            .subscribe();
    }

    isGuid(value: any): boolean {
        return /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/.test(value);
    }
}
