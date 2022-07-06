import { JsonSchemaFormService } from "@ajsf/core";
import { Component, Input, OnInit } from "@angular/core";
import { AbstractControl } from "@angular/forms";
import { tap } from "rxjs/operators";
import { UploadFileWidgetService } from "src/app/core/services/file-upload.service";
import { CasesApiService } from "../../core/services/cases-api.service";

@Component({
    templateUrl: './jsf-file-widget.component.html'
})
export class JSFFileWidgetComponent implements OnInit {
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

    constructor(
        private _api: CasesApiService,
        private jsf: JsonSchemaFormService,
        private uploadFileWidgetService: UploadFileWidgetService
    ) { }

    public ngOnInit() {
        this.options = this.layoutNode.options || {};
        this.jsf.initializeControl(this);
        this.draft = this.jsf.formOptions.draft;
    }

    public onFileSelect(event: any) {
        this.formControl?.markAsTouched();
        const value = event.target.files.length > 0 ? this.layoutNode.dataPointer : undefined;
        this.jsf.updateValue(this, value);
        if (!this.uploadFileWidgetService.files) {
            this.uploadFileWidgetService.files = {};
        }
        if (event.target.files.length > 0) {
            const file: File = event.target.files[0];
            this.uploadFileWidgetService.files[this.layoutNode.dataPointer] = file;
        }
    }

    public onDownload() {
        this._api.downloadAttachment(this.controlValue!)
            .pipe(
                tap(results => {
                    const fileURL = window.URL.createObjectURL(results.data);
                    window.open(fileURL, '_blank');
                }))
            .subscribe();
    }
}
