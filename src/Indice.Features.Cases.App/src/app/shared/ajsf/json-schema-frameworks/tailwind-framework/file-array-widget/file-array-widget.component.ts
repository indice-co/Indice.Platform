import { JsonSchemaFormService } from '@ajsf-extended/core';
import { Component, Input, OnInit } from '@angular/core';
import { AbstractControl, FormArray, FormControl } from '@angular/forms';
import { ToastType } from '@indice/ng-components';
import { tap } from 'rxjs';
import { CasesApiService } from 'src/app/core/services/cases-api.service';
import { FileUploadService } from 'src/app/core/services/file-upload.service';
import { TranslatedToasterService } from 'src/app/shared/services/translated-toaster.service';

@Component({
    templateUrl: './file-array-widget.component.html',
    standalone: false,
})
export class FileArrayWidgetComponent implements OnInit {
    formControl: AbstractControl | undefined;
    controlName: string | undefined;
    controlValue: string | undefined;
    controlDisabled: boolean = false;
    options?: FileArrayWidgetOptions;
    @Input() layoutNode: any;
    @Input() layoutIndex: number[] | undefined;
    @Input() dataIndex: number[] | undefined;
    @Input() data: any;
    draft?: boolean = false;
    accept: string = '*.*';
    hasDownloadableFiles: boolean = false;

    constructor(
        private _toaster: TranslatedToasterService,
        private _api: CasesApiService,
        private _jsf: JsonSchemaFormService,
        private _fileUploadService: FileUploadService,
    ) {}

    public ngOnInit() {
        this.options = this.layoutNode.options || {};
        if (this.options?.accept !== undefined) {
            this.accept = this.options.accept.join(', ');
        }
        this._jsf.initializeControl(this);
        this.draft = this._jsf.formOptions.draft;
        this.hasDownloadableFiles = Array.isArray(this.controlValue) && this.controlValue.some((v: any) => typeof v === 'string' && this.isGuid(v));
    }

    protected isGuid(str: string) {
        const guidRegex = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;
        return guidRegex.test(str);
    }

    public onFileSelect(event: any) {
        this.formControl?.markAsTouched();
        if (!this.validateInput(event.target.files)) {
            this._toaster.show(
                ToastType.Error,
                'toasts.saveError.title',
                'toasts.invalidFileType.body',
                5000,
            );
            return;
        }
        if (!this._fileUploadService.files) {
            this._fileUploadService.files = {};
        }
        if (event.target.files.length > 0) {
            const files: FileList = event.target.files;
            const existingValues = Array.isArray(this.controlValue)
                ? this.controlValue.filter(
                      (v: any) => typeof v === 'string' && v.trim() !== '',
                  )
                : [];
            const fileValues: string[] = [...existingValues];
            let startIndex = existingValues.length;
            for (let i = 0; i < files.length; i++) {
                const file = files[i];
                const pointerKey = `${this.layoutNode.dataPointer}/${startIndex + i}`;
                fileValues.push(pointerKey);
                this._fileUploadService.files[pointerKey] = file;
            }

            if (this.formControl instanceof FormArray) {
                while (this.formControl.length < fileValues.length) {
                    const newValue = fileValues[this.formControl.length];
                    this.formControl.push(new FormControl(newValue), {
                        emitEvent: false,
                    });
                }
            }

            this._jsf.updateValue(this, fileValues);
        }
    }

    private validateInput(files: FileList): boolean {
        const acceptedTypes = this.options?.accept;

        if (!acceptedTypes?.length) {
            this.clearInvalidFileTypeError();
            return true;
        }

        const invalidFiles = Array.from(files).filter(file =>
            !this.isFileAccepted(file, acceptedTypes)
        );

        if (invalidFiles.length > 0) {
            this.formControl?.setErrors({
                ...this.formControl.errors,
                invalidFileType: {
                    files: invalidFiles.map(file => file.name),
                    acceptedTypes
                }
            });

            this.formControl?.markAsTouched();
            return false;
        }

        this.clearInvalidFileTypeError();
        return true;

    }

    private isFileAccepted(file: File, acceptedTypes: string[]): boolean {
        const fileName = file.name.toLowerCase();
        const mimeType = file.type.toLowerCase();

        return acceptedTypes.some(type => {
            const acceptedType = type.trim().toLowerCase();

            // "wildcard" */*
            if (acceptedType === '*' || acceptedType === '*.*') {
                return true;
            }

            // extensions eg .pdf, .docx, etc.
            if (acceptedType.startsWith('.')) {
                return fileName.endsWith(acceptedType);
            }

            // wildcard image/*, application/*
            if (acceptedType.endsWith('/*')) {
                const mimePrefix = acceptedType.slice(0, -1);
                return mimeType.startsWith(mimePrefix);
            }

            // exact type image/png, application/pdf
            return mimeType === acceptedType;
        });
    }

    private clearInvalidFileTypeError(): void {
        if (!this.formControl?.errors?.['invalidFileType']) {
            return;
        }

        const { invalidFileType, ...remainingErrors } = this.formControl.errors;

        this.formControl.setErrors(
            Object.keys(remainingErrors).length > 0 ? remainingErrors : null,
        );
    }

    public onDownload(attachmentId: string) {
        this._api.downloadAttachment(attachmentId)
            .pipe(
                tap(results => {
                    const fileURL = window.URL.createObjectURL(results.data);

                    if (this.options?.downloadToDisk) {
                        const a = document.createElement('a');
                        a.href = fileURL;
                        //we get the file name from the content-disposition header, so make sure its exposed
                        a.download = results.fileName ?? `${this.layoutNode.name}-${attachmentId}`;
                        a.click();
                        window.URL.revokeObjectURL(fileURL); //clean up
                    } else { //if downloadToDisk is not there or set to false, then open file in new tab to show the content
                        window.open(fileURL, '_blank');
                    }
                })
            )
            .subscribe();
    }

    protected onRemove(attachmentId: string) {
        const existingValues = Array.isArray(this.controlValue)
            ? this.controlValue.filter((v: any) => typeof v === 'string' && v.trim() !== '')
            : [];
        const updatedValues = existingValues.filter((v: any) => v !== attachmentId);
        this._jsf.updateValue(this, updatedValues);
        return;
    }
}

export interface FileArrayWidgetOptions {
    accept: string[];
    maxLength?: number;
    minLength?: number;
    required?: boolean;
    readonly?: boolean;
    downloadToDisk?: boolean;
}
