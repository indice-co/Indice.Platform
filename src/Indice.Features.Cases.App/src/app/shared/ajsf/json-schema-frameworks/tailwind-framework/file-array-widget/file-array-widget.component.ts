import { isInputRequired, JsonPointer, JsonSchemaFormService } from '@ajsf-extended/core';
import { Component, Input, OnInit } from '@angular/core';
import { AbstractControl, FormArray, FormControl, NgForm } from '@angular/forms';
import { tap } from 'rxjs';
import { CasesApiService } from 'src/app/core/services/cases-api.service';
import { CaseDetailsService } from 'src/app/core/services/case-details.service';
import { FileUploadService } from 'src/app/core/services/file-upload.service';

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
    accept = '';
    protected isRequired = false;
    selectionErrors: { invalidFileType?: string[]; fileTooLarge?: string[]; tooManyFiles?: boolean } = {};
    private arraySchema: { minItems?: number; maxItems?: number } = {};

    constructor(
        private _api: CasesApiService,
        private _caseDetails: CaseDetailsService,
        private _jsf: JsonSchemaFormService,
        private _fileUploadService: FileUploadService,
        private _form: NgForm,
    ) {}

    public ngOnInit() {
        this.options = this.layoutNode.options || {};
        const schemaPointer = JsonPointer.toSchemaPointer(this.layoutNode.dataPointer, this._jsf.schema);
        this.arraySchema = JsonPointer.get(this._jsf.schema, schemaPointer) ?? {};
        this.isRequired = isInputRequired(this._jsf.schema, schemaPointer);
        if (this.options?.accept?.length) {
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

    protected get minFiles(): number {
        return this.arraySchema.minItems ?? 0;
    }

    protected get maxFiles(): number | undefined {
        return this.arraySchema.maxItems;
    }

    protected get maxFileSizeBytes(): number | undefined {
        return this.options?.maxFileSizeBytes;
    }

    protected get maxFileSizeLabel(): string | undefined {
        const bytes = this.maxFileSizeBytes;
        if (bytes === undefined) {
            return undefined;
        }
        if (bytes >= 1024 * 1024) {
            return `${Number((bytes / (1024 * 1024)).toFixed(2))} MB`;
        }
        if (bytes >= 1024) {
            return `${Number((bytes / 1024).toFixed(2))} KB`;
        }
        return `${bytes} B`;
    }

    protected get showCountError(): boolean {
        return this.fileValues.length > 0 || (this.isRequired && (this.formControl?.dirty || this._form.submitted));
    }

    protected get hasTypeLimit(): boolean {
        return !!this.options?.accept?.length && !this.options.accept.some(type => ['*', '*.*', '*/*'].includes(type.trim()));
    }

    protected get hasRequirements(): boolean {
        return this.hasTypeLimit || this.minFiles > 0 || this.maxFiles !== undefined || this.maxFileSizeBytes !== undefined;
    }

    protected get validationMessages(): FileValidationMessage[] {
        const messages: FileValidationMessage[] = [];
        if (this.selectionErrors.invalidFileType?.length) {
            messages.push({ key: 'fileUpload.invalidType', params: { types: this.accept }, files: this.selectionErrors.invalidFileType });
        }
        if (this.selectionErrors.fileTooLarge?.length) {
            messages.push({ key: 'fileUpload.fileTooLarge', params: { size: this.maxFileSizeLabel }, files: this.selectionErrors.fileTooLarge });
        }
        if (this.selectionErrors.tooManyFiles || (this.showCountError && this.maxFiles !== undefined && this.fileValues.length > this.maxFiles)) {
            messages.push({ key: this.maxFiles === 1 ? 'fileUpload.tooManyFilesOne' : 'fileUpload.tooManyFiles', params: { count: this.maxFiles } });
        }
        if (this.showCountError && this.fileValues.length < this.minFiles) {
            messages.push({ key: this.minFiles === 1 ? 'fileUpload.tooFewFilesOne' : 'fileUpload.tooFewFiles', params: { count: this.minFiles } });
        }
        return messages;
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
        this.selectionErrors = this.validateInput(files);
        if (Object.keys(this.selectionErrors).length) {
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

    private validateInput(files: FileList): typeof this.selectionErrors {
        const acceptedTypes = this.options?.accept;
        const errors: typeof this.selectionErrors = {};
        if (acceptedTypes?.length) {
            const invalidFiles = Array.from(files).filter(file => !this.isFileAccepted(file, acceptedTypes));
            if (invalidFiles.length) {
                errors.invalidFileType = invalidFiles.map(file => file.name);
            }
        }
        const maxFileSizeBytes = this.maxFileSizeBytes;
        const oversizedFiles = maxFileSizeBytes === undefined
            ? []
            : Array.from(files).filter(file => file.size > maxFileSizeBytes);
        if (oversizedFiles.length) {
            errors.fileTooLarge = oversizedFiles.map(file => file.name);
        }
        if (this.maxFiles !== undefined && this.fileValues.length + files.length > this.maxFiles) {
            errors.tooManyFiles = true;
        }
        return errors;
    }

    private isFileAccepted(file: File, acceptedTypes: string[]): boolean {
        const fileName = file.name.toLowerCase();
        const mimeType = file.type.toLowerCase();

        return acceptedTypes.some(type => {
            const acceptedType = type.trim().toLowerCase();

            if (acceptedType === '*' || acceptedType === '*.*' || acceptedType === '*/*') {
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
        this.selectionErrors = {};
        this.formControl?.markAsTouched();
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

interface FileValidationMessage {
    key: string;
    params: { types?: string; size?: string; count?: number };
    files?: string[];
}

export interface FileArrayWidgetOptions {
    accept?: string[];
    maxFileSizeBytes?: number;
    readonly?: boolean;
    downloadToDisk?: boolean;
}
