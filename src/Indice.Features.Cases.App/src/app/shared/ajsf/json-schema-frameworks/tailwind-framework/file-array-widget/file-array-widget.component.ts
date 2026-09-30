import { JsonSchemaFormService } from '@ajsf-extended/core';
import { Component, Input, OnInit } from '@angular/core';
import { AbstractControl, FormArray, FormControl } from '@angular/forms';
import { ToastType } from '@indice/ng-components';
import { tap } from 'rxjs';
import { CasesApiService } from 'src/app/core/services/cases-api.service';
import { CaseDetailsService } from 'src/app/core/services/case-details.service';
import { FileUploadService } from 'src/app/core/services/file-upload.service';
import { TranslatedToasterService } from 'src/app/shared/services/translated-toaster.service';

@Component({
    templateUrl: './file-array-widget.component.html',
    standalone: false,
})
export class FileArrayWidgetComponent implements OnInit {
    formControl: AbstractControl | undefined;
    controlName: string | undefined;
    controlValue: string[] | undefined;
    controlDisabled: boolean = false;
    options?: FileArrayWidgetOptions;
    @Input() layoutNode: any;
    @Input() layoutIndex: number[] | undefined;
    @Input() dataIndex: number[] | undefined;
    @Input() data: any;
    draft?: boolean = false;
    accept: string = '*.*';

    constructor(
        private _toaster: TranslatedToasterService,
        private _api: CasesApiService,
        private _caseDetails: CaseDetailsService,
        private _jsf: JsonSchemaFormService,
        private _fileUploadService: FileUploadService,
    ) {}

    public ngOnInit() {
        this.options = this.layoutNode.options || {};
        if (this.options?.accept !== undefined) {
            this.accept = this.options.accept.join(', ');
        }
        this._jsf.initializeControl(this);
        if (this.formControl instanceof FormArray) {
            for (let index = this.formControl.length - 1; index >= 0; index--) {
                const value = this.formControl.at(index).value;
                if (typeof value !== 'string' || !value.trim()) {
                    this.formControl.removeAt(index, { emitEvent: false });
                }
            }
            this.controlValue = this.formControl.value;
            this.layoutNode.value = this.controlValue;
        }
        this.draft = this._jsf.formOptions.draft;
    }

    protected isGuid(str: string): boolean {
        const guidRegex = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;
        return guidRegex.test(str);
    }

    protected get fileValues(): string[] {
        return Array.isArray(this.controlValue) ? this.controlValue : [];
    }

    protected fileName(value: string): string | undefined {
        if (this.isGuid(value)) {
            return this._caseDetails.caseDetails?.attachments?.find(attachment => attachment.id === value)?.fileName;
        }
        return this._fileUploadService.files?.[value]?.name;
    }

    public onFileSelect(event: Event) {
        const input = event.target as HTMLInputElement;
        const files = input.files;
        if (!files?.length) {
            return;
        }
        this.formControl?.markAsTouched();
        if (!this.validateInput(files)) {
            this._toaster.show(
                ToastType.Error,
                'toasts.saveError.title',
                'toasts.invalidFileType.body',
                5000,
            );
            input.value = '';
            return;
        }
        if (!this._fileUploadService.files) {
            this._fileUploadService.files = {};
        }
        const fileValues = [...this.fileValues];
        for (const file of Array.from(files)) {
            const pointer = `${this.layoutNode.dataPointer}/${fileValues.length}`;
            fileValues.push(pointer);
            this._fileUploadService.files[pointer] = file;
        }
        this.updateFiles(fileValues);
        input.value = '';
    }

    private updateFiles(values: string[]): void {
        if (this.formControl instanceof FormArray) {
            while (this.formControl.length > values.length) {
                this.formControl.removeAt(this.formControl.length - 1, { emitEvent: false });
            }
            while (this.formControl.length < values.length) {
                this.formControl.push(new FormControl(values[this.formControl.length]), { emitEvent: false });
            }
        }
        this._jsf.updateValue(this, values);
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

            if (acceptedType === '*' || acceptedType === '*.*') {
                return true;
            }

            if (acceptedType.startsWith('.')) {
                return fileName.endsWith(acceptedType);
            }

            if (acceptedType.endsWith('/*')) {
                const mimePrefix = acceptedType.slice(0, -1);
                return mimeType.startsWith(mimePrefix);
            }

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
                        a.download = results.fileName ?? `${this.layoutNode.name}-${attachmentId}`;
                        a.click();
                        window.URL.revokeObjectURL(fileURL);
                    } else {
                        window.open(fileURL, '_blank');
                    }
                })
            )
            .subscribe();
    }

    protected onRemove(index: number): void {
        const values = this.fileValues.filter((_, fileIndex) => fileIndex !== index);
        const uploads = this._fileUploadService.files ?? {};
        const pendingFiles = values.map(value => this.isGuid(value) ? undefined : uploads[value]);

        for (const value of this.fileValues) {
            if (!this.isGuid(value)) {
                delete uploads[value];
            }
        }

        const updatedValues = values.map((value, fileIndex) => {
            const file = pendingFiles[fileIndex];
            if (!file) {
                return value;
            }
            const pointer = `${this.layoutNode.dataPointer}/${fileIndex}`;
            uploads[pointer] = file;
            return pointer;
        });

        this.updateFiles(updatedValues);
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
