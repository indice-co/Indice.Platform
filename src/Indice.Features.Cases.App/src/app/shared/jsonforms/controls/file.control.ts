import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { NgIcon } from '@ng-icons/core';
import { TranslatePipe } from '@ngx-translate/core';
import { tap } from 'rxjs/operators';
import { CasesApiService } from 'src/app/core/services/cases-api.service';
import { FileUploadService } from 'src/app/core/services/file-upload.service';
import { ToastService, ToastType } from 'src/app/ui/feedback/toast.service';
import { CaseFormControl, FieldKind } from './case-form-control';
import { FieldWrapperComponent } from './field-wrapper.component';

/**
 * ajsf `file` widget. A selected file is kept in `FileUploadService.files`, keyed by the data path (eg `homePhone.attachmentId`),
 * and the field holds that path until the host uploads the file and replaces it with the attachment id.
 */
@Component({
    selector: 'app-jf-file',
    imports: [FieldWrapperComponent, NgIcon, TranslatePipe],
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
              [attr.aria-invalid]="invalid"
              [attr.aria-describedby]="describedBy"
              [id]="id"
              (change)="onFileSelect($event)">
          }
          <p class="jf-file-empty text-quiet text-xs">{{ 'common.noDocumentSubmitted' | translate }}</p>
        } @else {
          <div class="jf-attachment">
            <ng-icon name="lucidePaperclip" size="1rem" class="text-quiet" />
            <button type="button" class="btn btn-ghost btn-sm" (click)="onDownload()">
              <ng-icon name="lucideDownload" size="1rem" />
              <span>{{ 'common.download' | translate }}</span>
            </button>
            @if (!readonly) {
              <button type="button" class="btn btn-ghost btn-sm btn-square ml-auto text-error" (click)="setValue(undefined)"
                      [attr.aria-label]="'common.removeAttachment' | translate" [attr.title]="'common.removeAttachment' | translate">
                <ng-icon name="lucideTrash2" size="1rem" />
              </button>
            }
          </div>
        }
      </app-jf-field>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager
})
export class FileControlComponent extends CaseFormControl {
    protected widget = 'file';
    protected override fieldKind: FieldKind = 'file-input';
    private toaster = inject(ToastService);
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
