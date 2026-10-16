import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ToastType } from '@indice/ng-components';
import { tap } from 'rxjs/operators';
import { CasesApiService } from 'src/app/core/services/cases-api.service';
import { FileUploadService } from 'src/app/core/services/file-upload.service';
import { TranslatedToasterService } from 'src/app/shared/services/translated-toaster.service';
import { TailwindControl } from './tailwind-control';

/**
 * ajsf `file` widget. A selected file is kept in `FileUploadService.files`, keyed by the data path (eg `homePhone.attachmentId`),
 * and the field holds that path until the host uploads the file and replaces it with the attachment id.
 */
@Component({
    selector: 'app-jf-file',
    template: `
    @if (shown) {
      <app-jf-field [control]="this">
        @if (!isGuid(data)) {
          @if (!readonly) {
            <input
              type="file"
              [accept]="accept"
              [class]="fieldClass"
              [attr.required]="options.required"
              [id]="id"
              (change)="onFileSelect($event)">
          }
          <p class="text-sm text-gray-900">{{ 'common.noDocumentSubmitted' | translate }}</p>
        } @else {
          <div class="flex align-items-center justify-content-center align-middle">
            <button type="button" (click)="onDownload()" class="btn-default">
              <svg class="fill-current w-4 h-4 mr-2" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20">
                <path d="M13 8V2H7v6H2l8 8 8-8h-5zM0 18h20v2H0v-2z" />
              </svg>
              <span>{{ 'common.download' | translate }}</span>
            </button>
            @if (!readonly) {
              <button (click)="setValue(undefined)" type="button" class="ml-2 bg-gray-300 hover:bg-gray-400 focus:ring-4 focus:outline-none focus:ring-gray-300 font-medium rounded-full text-sm p-1 text-center inline-flex items-center me-2 dark:bg-gray-600 dark:hover:bg-gray-700 dark:focus:ring-gray-800">
                <svg class="w-6 h-6 text-gray-800 text-black" style="color: rgb(31 41 55 / 1) !important" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" width="24" height="24" fill="none" viewBox="0 0 24 24">
                  <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 7h14m-9 3v8m4-8v8M10 3h4a1 1 0 0 1 1 1v3H9V4a1 1 0 0 1 1-1ZM6 7h12v13a1 1 0 0 1-1 1H7a1 1 0 0 1-1-1V7Z"/>
                </svg>
                <span class="sr-only">{{ 'common.removeAttachment' | translate }}</span>
              </button>
            }
          </div>
        }
      </app-jf-field>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class FileControlComponent extends TailwindControl {
    protected widget = 'file';
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
