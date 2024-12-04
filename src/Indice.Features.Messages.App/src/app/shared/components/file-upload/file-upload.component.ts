import { Component, ElementRef, EventEmitter, Input, OnInit, Output, ViewChild } from '@angular/core';

@Component({
    selector: 'app-file-upload',
    templateUrl: './file-upload.component.html'
})
export class FileUploadComponent implements OnInit {
    @ViewChild('fileInput', { static: false }) public _fileInput: ElementRef | undefined;
    @Input() maxSize?: number;
    @Input() accept: string = '';
    @Input() extendedView: boolean = false;
    @Input() existingFiles: IAttachment[] = [];
    @Output() filesChange = new EventEmitter<IAttachment[]>();

    public sizeLimitReached: boolean = false;
    public maxSizeText?: string;
    public files: IAttachment[] = [];

    constructor() { }

    ngOnInit(): void {
        if (this.existingFiles.length) {
            this.files = [...this.existingFiles];
        }

        if (this.maxSize) {
            this.maxSizeText = `${(this.maxSize / 1024 / 1024).toFixed(1)} MB`;
        }
    }

    public clickElement(): void {
        this.sizeLimitReached = false;
        this._fileInput?.nativeElement?.click();
    }

    public onFilesAdded(): void {
        const inputFiles = this._fileInput?.nativeElement.files;
        if (!inputFiles) {
            return;
        }

        const newFiles: IAttachment[] = [];
        for (let i = 0; i < inputFiles.length; i++) {
            const uploadedFile = inputFiles[i];

            if (this.maxSize && uploadedFile.size > this.maxSize) {
                this.sizeLimitReached = true;
                continue;
            }

            const attachment: IAttachment = {
                title: uploadedFile.name,
                data: uploadedFile,
                contentType: uploadedFile.type || 'unknown',
                size: uploadedFile.size,
                sizeText: `${(uploadedFile.size / 1024).toFixed(1)} KB`
            };

            newFiles.push(attachment);
        }

        this.files = [...this.files, ...newFiles];
        this.filesChange.emit(this.files);

        this.resetFileInput();
    }

    public removeFile(index: number): void {
        this.files.splice(index, 1);
        this.filesChange.emit(this.files);
    }

    public reset(): void {
        this.files = [...this.existingFiles];
        this.resetFileInput();
        this.filesChange.emit(this.files);
    }

    private resetFileInput(): void {
        if (this._fileInput?.nativeElement) {
            this._fileInput.nativeElement.value = null;
        }
    }
}

export interface IAttachment {
    id?: string;
    title?: string;
    data?: Blob;
    contentType?: string;
    size?: number;
    sizeText?: string;
}
