import { ChangeDetectorRef, Component, ElementRef, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges, ViewChild, OnDestroy, ChangeDetectionStrategy } from '@angular/core';
import { ToastType } from '@indice/ng-components';
import { TranslatedToasterService } from 'src/app/shared/services/translated-toaster.service';
import { EMPTY, forkJoin, Observable, of } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { Case, CasesApiService, CasesAttachmentLink, EditCaseRequest, ProblemDetails } from 'src/app/core/services/cases-api.service';
import { FileUploadService } from 'src/app/core/services/file-upload.service';
import { get, isEqual, update } from 'lodash';
import { Router } from '@angular/router';
import { createAjv, Generate } from '@jsonforms/core';
import { CasesJsonFormsConfig } from 'src/app/shared/jsonforms/jsonforms-utilities';
import { tailwindRenderers } from 'src/app/shared/jsonforms/tailwind-renderers';


@Component({
    selector: 'app-case-form',
    templateUrl: './case-form.component.html',
    styleUrls: ['./case-form.component.css'],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class CaseFormComponent implements OnChanges, OnInit, OnDestroy {
    private latestModel: any;
    private latestIsValid: boolean = false;
    // form is editable?
    @Input() formEditable: boolean | undefined;
    @Input() case: Case | undefined;
    // inform parent that data should be refreshed
    @Output() updateDataEvent = new EventEmitter<{ draft: boolean }>();
    @Output() formIsValid = new EventEmitter<boolean>();
    @Output() unSavedChanges = new EventEmitter<boolean>();
    public renderers = tailwindRenderers;
    public formsConfig: CasesJsonFormsConfig = {};
    public ajv = createAjv();
    public readonly = false;
    private copiedLayout: any;
    private initialData: any;
    public formIsDirty: boolean = false;
    public showForm = true;
    public data: any;
    public schema: any;
    // the ajsf layout, still read for its onChange / onInit scripts
    public layout: any;
    public uiSchema: any;
    public lastChange: any;
    onChangeCallbackDictionary: any = {};

    @ViewChild('formContainer') formContainer: ElementRef | undefined;

    constructor(
        private router: Router,
        private _toaster: TranslatedToasterService,
        private _api: CasesApiService,
        private _fileUploadService: FileUploadService,
        private changeDetector: ChangeDetectorRef
    ) { }

    ngOnDestroy(): void {
        // we should DEFINITELY remove the uploaded/saved for upload files
        this._fileUploadService.reset();
    }

    ngOnChanges(changes: SimpleChanges) {
        if (!this.case) { // no case -> no point to continue execution
            return;
        }
        if (changes.hasOwnProperty('case')) {
            this.showForm = false;
            this.changeDetector.detectChanges(); // enforce the instantaneous deletion of form
            // deep copy layout
            this.copiedLayout = (typeof this.case.caseType?.layout! !== 'string') ? this.case.caseType?.layout! : JSON.parse(this.case.caseType?.layout!);
            /**
             * delete empty strings and clear potential empty objects in data
             * for ajsf (so that onChanges can work as expected - ajsf, at initialization, deletes empty objects and strings)
             */
            if (this.case.data) {
                this.initialData = this.removeEmptyObjects(this.deleteEmptyStringProperties(this.case.data!));
            }
        }
        // extract layout from case type
        let layout = (typeof this.case.caseType?.layout! !== 'string') ? this.case.caseType?.layout! : JSON.parse(this.case.caseType?.layout!);
        this.schema = (typeof this.case.caseType?.dataSchema! !== 'string') ? this.case.caseType?.dataSchema! : JSON.parse(this.case.caseType?.dataSchema!);
        // no layout: generate the UI schema JSON Forms would generate, so that a submit can be added to it
        const uiSchema = structuredClone(this.case.caseType?.uiSchema) ?? Generate.uiSchema(this.schema);
        if (!this.formEditable) {
            if (this.case.draft) {
                // case is draft, we need all form properties available and ignore layout restrictions (eg readonly, or html class disable)
                this.removeReadonlyProperties(uiSchema);
            }
            // here we need to check if case has become editable: we need to "enable" checkboxes
            // note: this won't affect checkboxes that have "disableCheckbox" class in their layout!
            if (this.formContainer?.nativeElement && (this.formEditable || this.case?.draft)) {
                this.formContainer.nativeElement.classList.remove('disableCheckbox');
            }
        }
        if (this.case.draft || this.formEditable) {
            this.addSubmit(uiSchema);
        }
        // not draft and not editable: the form is view-only
        this.readonly = !this.formEditable && !this.case.draft;
        this.formsConfig = { draft: this.case.draft, onSubmit: data => this.onSubmit(data) };
        this.ajv = createAjv({ useDefaults: !!this.case.draft }); // use schema defaults only for draft cases!
        this.layout = layout;
        this.uiSchema = uiSchema;
        this.data = this.initialData;
        this.populateForm(this.data);
        this.showForm = true;
        this.changeDetector.detectChanges();
    }

    ngOnInit(): void { }

    public ngAfterViewInit(): void {
        // check if we need to "disable" case's checkboxes
        if (!this.formEditable && !this.case?.draft) {
            this.formContainer?.nativeElement.classList.add('disableCheckbox');
        }
    }

    public onSubmit(event: any): void {
        // we have to send user's document(s) to the server
        const callsDict: { [key: string]: Observable<CasesAttachmentLink> } = { '': of({} as CasesAttachmentLink) };
        // what is the key of the dictionary here? the data path (e.g. "homeAddress.attachmentId") that was added in the file control
        for (const key in this._fileUploadService.files) {
            if (this._fileUploadService.files.hasOwnProperty(key)) {
                // we may have 'garbage' attachments that we should not send to server!!!
                if (get(event, key)) { // https://lodash.com/docs/4.17.15#get
                    const fileParam = { data: this._fileUploadService.files[key], fileName: this._fileUploadService.files[key].name };
                    callsDict[key] = this._api.uploadAdminCaseAttachment(this.case?.id!, fileParam);
                }
            }
        }
        forkJoin(callsDict)
            .pipe(
                tap(
                    // we can match the received attachment guids to the respective fields with the help of the keys/dataPointers!
                    (response: { [key: string]: CasesAttachmentLink }) => {
                        for (const key in response) {
                            if (response.hasOwnProperty(key) && key !== '') {
                                // the key is a data path (eg "home.attachmentId") that lodash update (https://lodash.com/docs/4.17.15#update)
                                // understands; update its value with the response attachment id
                                update(event, key, () => response[key].id!);
                            }
                        }

                        if (this.case?.draft) {
                            // finally, submit the case
                            this._api.submitAdminCase(this.case?.id!, event)
                                .pipe(
                                    tap(() => {
                                        this._fileUploadService.reset();
                                        this._toaster.show(ToastType.Success, 'toasts.caseSubmitted.title', 'toasts.caseSubmitted.body', 5000);
                                        this.updateDataEvent.emit({ draft: true });
                                    }),
                                    catchError((err: ProblemDetails) => { // error during case submit
                                        this._toaster.show(ToastType.Error, 'toasts.caseSubmitError.title', err.detail || 'toasts.caseSubmitError.body', 5000);
                                        this.router.navigate(['/cases']);
                                        return EMPTY;
                                    })
                                )
                                .subscribe();
                        } else {
                            const editCaseRequest = new EditCaseRequest({ data: event });
                            this._api.editCase(this.case?.id!, editCaseRequest)
                                .pipe(
                                    tap(() => {
                                        this.initialData = event; // initial data are, now, the saved data
                                        this.formIsDirty = false;
                                        this.unSavedChanges.emit(this.formIsDirty);
                                        this.updateDataEvent.emit({ draft: false });
                                        this._toaster.show(ToastType.Success, 'toasts.saveSuccess.title', 'toasts.saveSuccess.body', 5000);
                                        this._fileUploadService.reset();
                                    }),
                                    catchError(() => {
                                        this._toaster.show(ToastType.Error, 'toasts.saveError.title', 'toasts.saveError.body', 5000);
                                        return EMPTY;
                                    })
                                )
                                .subscribe();
                        }
                    }),
                catchError(() => { // error during attachments upload
                    this._toaster.show(ToastType.Error, 'toasts.caseSubmitError.title', 'toasts.attachmentsError.body', 5000);
                    return EMPTY;
                }))
            .subscribe();
    }

    // property-level onChange implementation from:
    // https://github.com/hamzahamidi/ajsf/issues/37
    onFormChanges(entity: any) {
        this.latestModel = entity;
        if (!this.case?.draft) { // this logic applies only for non-draft cases
            // this will only work if setSchemaDefaults is set to FALSE, because, at initialization, ajsf adds to data the properties with default values from schema!
            if (!isEqual(entity, this.initialData)) {
                this.formIsDirty = true;
                this.unSavedChanges.emit(this.formIsDirty);
            } else {
                this.formIsDirty = false;
                this.unSavedChanges.emit(this.formIsDirty);
            }
        }

        // ignore an empty form
        if (!entity || (Object.keys(entity).length === 0 && entity.constructor === Object)) {
            return;
        }

        for (const [key, onChange] of Object.entries(this.onChangeCallbackDictionary)) {
            if (get(entity, key) === get(this.lastChange, key)) {
                // no changes, no need to invoke onChange
                continue;
            }

            // check needed so typescript won't complain when compiling
            if (typeof onChange === 'function') {
                onChange(get(entity, key), entity);
            }
        }
        this.lastChange = entity;
    }

    public isValid(event: boolean) {
        this.formIsValid.emit(event);
        this.latestIsValid = event;
    }

    onSubmitExternal(): void {
        if (this.latestIsValid) {
            this.onSubmit(this.latestModel);
        }
    }

    onCancel(): void {
        this.showForm = false;
        this.changeDetector.detectChanges(); // enforce the instantaneous deletion of form
        this.schema = JSON.parse(this.case?.caseType?.dataSchema!);
        this.layout = JSON.parse(this.case?.caseType?.layout!);
        this.data = this.initialData;
        this.populateForm(this.data);
        this.showForm = true;
        this.changeDetector.detectChanges();
    }

    /** Removes `readonly` and the `disableCheckbox` class from every UI schema element. */
    private removeReadonlyProperties(uiSchema: any): void {
        if (!uiSchema) {
            return;
        }
        const options = uiSchema.options;
        if (options) {
            delete options.readonly;
            if (options.htmlClass && options.htmlClass.indexOf('disableCheckbox') > -1) {
                options.htmlClass = options.htmlClass.replace('disableCheckbox', '');
            }
            this.removeReadonlyProperties(options.detail); // array item layout
        }
        uiSchema.elements?.forEach((element: any) => this.removeReadonlyProperties(element));
    }

    /** Adds a submit button at the end of the form when the layout has none, as ajsf `addSubmit` did. */
    private addSubmit(uiSchema: any): void {
        const hasSubmit = (element: any): boolean =>
            element?.type === 'Submit' || !!element?.elements?.some(hasSubmit) || (!!element?.options?.detail && hasSubmit(element.options.detail));
        if (Array.isArray(uiSchema?.elements) && !hasSubmit(uiSchema)) {
            uiSchema.elements.push({ type: 'Submit' });
        }
    }

    private deleteEmptyStringProperties(obj: any) {
        for (var k in obj) {
            if (!obj[k] || typeof obj[k] !== "object") {
                if (obj[k] === '') {
                    delete obj[k];
                }
                continue // If null or not an object, skip to the next iteration
            }

            // The property is an object
            this.deleteEmptyStringProperties(obj[k]); // <-- Make a recursive call on the nested object
        }
        return obj;
    }

    private removeEmptyObjects(obj: any) {
        for (var k in obj) {
            if (!obj[k] || typeof obj[k] !== "object") {
                continue // If null or not an object, skip to the next iteration
            }

            // The property is an object
            this.removeEmptyObjects(obj[k]); // <-- Make a recursive call on the nested object
            if (Object.keys(obj[k]).length === 0) {
                delete obj[k]; // The object had no properties, so delete that property
            }
        }
        return obj;
    }

    private populateForm(entity: any) {
        this.lastChange = entity;
        const onInitCallbackDictionary: any = {};
        if (!Array.isArray(this.layout)) {
            return;
        }
        if (this.layout) {
            // recursively traverse the form json
            const traverse = (item: any) => {
                if (item.items) {
                    item.items.map(traverse);
                } else if (item.tabs) {
                    item.tabs.map(traverse);
                }

                // keep track of onChange callbacks
                if (item.onChange) {
                    this.onChangeCallbackDictionary[item.key] = Function('"use strict";return (' + item.onChange + ')')();
                }

                if (item.onInit) {
                    onInitCallbackDictionary[item.key] = Function('"use strict";return (' + item.onInit + ')')();
                }
            };
            this.layout.map(traverse);
        }

        for (const [key, onInit] of Object.entries(onInitCallbackDictionary)) {
            if (typeof onInit === 'function') {
                const initEntity = onInit(get(entity, key), entity, this.case?.metadata);
                if (initEntity) {
                    this.data = Object.assign({}, this.data, initEntity);
                }
            }
        }
    }

    isObject(entity: any) {
        return typeof entity === 'object' &&
            !Array.isArray(entity) &&
            entity !== null;
    }

}
