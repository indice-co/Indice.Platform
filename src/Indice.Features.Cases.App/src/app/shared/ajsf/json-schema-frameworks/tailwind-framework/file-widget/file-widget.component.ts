import { JsonSchemaFormService } from "@ajsf-extended/core";
import { Component, Input, OnInit } from "@angular/core";
import { AbstractControl } from "@angular/forms";
import { ToasterService, ToastType } from "@indice/ng-components";
import { tap } from "rxjs/operators";
import { CasesApiService } from "src/app/core/services/cases-api.service";
import { FileUploadService } from "src/app/core/services/file-upload.service";

@Component({
    templateUrl: './file-widget.component.html'
})
export class FileWidgetComponent implements OnInit {
    formControl: AbstractControl | undefined;
    controlName: string | undefined;
    controlValue: string | undefined;
    controlDisabled: boolean = false;
    boundControl: boolean = false;
    options: any;
    autoCompleteList: string[] = [];
    @Input() formID: number | undefined;
    @Input() layoutNode: any;
    @Input() layoutIndex: number[] | undefined;
    @Input() dataIndex: number[] | undefined;
    @Input() data: any;
    attachmentId: any;
    draft?: boolean = false;
    accept: string = '*.*';

    constructor(
        private _toaster: ToasterService,
        private _api: CasesApiService,
        private _jsf: JsonSchemaFormService,
        private _fileUploadService: FileUploadService
    ) { }

    public ngOnInit() {
        this.options = this.layoutNode.options || {};
        if (this.options.accept !== undefined) {
            this.accept = this.options.accept.join(', ');
        }
        this._jsf.initializeControl(this);
        this.draft = this._jsf.formOptions.draft;
    }

    public onFileSelect(event: any) {
        this.formControl?.markAsTouched();
        if (
            this.options.accept
            && event.target.files.length > 0
            && !this.options.accept.some((fileExtension: string) => event.target.files[0].name.endsWith(fileExtension))
        ) {
            event.target.value = '';
            this._jsf.updateValue(this, undefined);
            // TODO: make this a validation error
            this._toaster.show(ToastType.Error, 'Αποτυχία αποθήκευσης', `Μη αποδεκτός τύπος αρχείου.`, 5000);
            return;
        }
        const value = event.target.files.length > 0 ? this.layoutNode.dataPointer : undefined;
        this._jsf.updateValue(this, value);
        if (!this._fileUploadService.files) {
            this._fileUploadService.files = {};
        }
        if (event.target.files.length > 0) {
            const file: File = event.target.files[0];
            this._fileUploadService.files[this.layoutNode.dataPointer] = file;
        }
    }

    public onDownload() {
        this._api.downloadAttachment(this.controlValue!)
            .pipe(
                tap(results => {
                    const fileURL = window.URL.createObjectURL(results.data);

          if (this.options?.downloadToDisk) {
                        const a = document.createElement('a');
                        a.href = fileURL;
            //we get the file name from the content-disposition header, so make sure its exposed
            a.download = results.fileName ?? `${this.layoutNode.name}-${this.controlValue}`;
                        a.click();
                        window.URL.revokeObjectURL(fileURL); //clean up
          } else { //if downloadToDisk is not there or set to false, then open file in new tab to show the content
                        window.open(fileURL, '_blank');
                    }
                })
            )
            .subscribe();
    }
}
