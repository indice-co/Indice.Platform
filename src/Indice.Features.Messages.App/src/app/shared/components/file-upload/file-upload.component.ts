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
  @Input() existingFile: IAttachment | undefined;
  @Output() fileChange = new EventEmitter<IAttachment | undefined>();
  
  public sizeLimitReached: boolean = false;
  public maxSizeText?: string;
  public file?: IAttachment;

  constructor() { }

  ngOnInit(): void {
    if (this.existingFile) {
      this.file = JSON.parse(JSON.stringify(this.existingFile))
    }
    if (this.maxSize) {
      this.maxSizeText = `${Math.round((this.maxSize / 1024 / 1024) * 10) / 10} MB`
    }
  }

  public clickElement() {
    this.sizeLimitReached = false;
    this._fileInput?.nativeElement?.focus();
    this._fileInput?.nativeElement?.click();
  }

  public onFilesAdded() {
    if (this._fileInput?.nativeElement.files == null) {
        return;
    }
    if (this.maxSize && this._fileInput?.nativeElement.files[0].size > this.maxSize) {
      this.sizeLimitReached = true;
      return;
    }

    const reader = new FileReader();
    reader.onloadend = (evt) => {
      if (this._fileInput?.nativeElement?.files == null) {
          return;
      }
      let uploadedFile = this._fileInput?.nativeElement?.files[0];
      this.file = <IAttachment>{
        title: uploadedFile.name,
        data: uploadedFile,
        contentType: uploadedFile.type ? uploadedFile.type : 'unknown',
        size: uploadedFile.size,
        sizeText: `${Math.round((uploadedFile.size / 1024) * 10) / 10} KB`
      };
      this.fileChange.emit(this.file);
    };
    reader.readAsDataURL(this._fileInput?.nativeElement?.files[0]);
  }

  public removeFile() {
    if (this._fileInput?.nativeElement?.files == null) {
      return;
    }
    this._fileInput.nativeElement.value = null;
    this.file = undefined;
    this.fileChange.emit(this.file);
  }

  public reset() {
    if (!this.existingFile) {
      if (this._fileInput?.nativeElement) {
        this._fileInput.nativeElement.value = null;
      }
      this.file = undefined;
      return;
    }
    this.file = JSON.parse(JSON.stringify(this.existingFile));
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
