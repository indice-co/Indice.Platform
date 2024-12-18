import { ChangeDetectorRef, Component, ElementRef, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges, ViewChild, OnDestroy } from '@angular/core';
import { ToasterService, ToastType } from '@indice/ng-components';
import { EMPTY, forkJoin, Observable, of } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { Case, CasesApiService, CasesAttachmentLink, EditCaseRequest, ProblemDetails } from 'src/app/core/services/cases-api.service';
import { FileUploadService } from 'src/app/core/services/file-upload.service';
import { SubmitWidgetComponent } from 'src/app/shared/ajsf/json-schema-frameworks/tailwind-framework/submit-widget/submit-widget.component';
import { TailwindFrameworkComponent } from 'src/app/shared/ajsf/json-schema-frameworks/tailwind-framework/tailwind-framework.component';
import { SelectWidgetComponent } from 'src/app/shared/ajsf/json-schema-frameworks/tailwind-framework/select-widget/select-widget.component';
import { get, isEqual, update } from 'lodash';
import { CurrencyWidgetComponent } from 'src/app/shared/ajsf/json-schema-frameworks/tailwind-framework/currency-widget/currency-widget.component';
import { DateWidgetComponent } from 'src/app/shared/ajsf/json-schema-frameworks/tailwind-framework/date-widget/date-widget.component';
import { LookupWidgetComponent } from 'src/app/shared/ajsf/json-schema-frameworks/tailwind-framework/lookup-widget/lookup-widget.component';
import { InputWidgetComponent } from 'src/app/shared/ajsf/json-schema-frameworks/tailwind-framework/input-widget/input-widget.component';
import { TextAreaWidgetComponent } from 'src/app/shared/ajsf/json-schema-frameworks/tailwind-framework/text-area-widget/text-area-widget.component';
import { Router } from '@angular/router';
import { FileWidgetComponent } from 'src/app/shared/ajsf/json-schema-frameworks/tailwind-framework/file-widget/file-widget.component';
import { LookupSelectorWidgetComponent } from 'src/app/shared/ajsf/json-schema-frameworks/tailwind-framework/lookup-selector-widget/lookup-selector-widget.component';
import { WysiwygWidgetComponent } from 'src/app/shared/ajsf/json-schema-frameworks/tailwind-framework/wysiwyg-widget/wysiwyg-widget.component';
import { HrefWidgetComponent } from 'src/app/shared/ajsf/json-schema-frameworks/tailwind-framework/href-widget/href-widget.component';
import { LabelOnlyWidgetComponent } from 'src/app/shared/ajsf/json-schema-frameworks/tailwind-framework/label-only-widget/label-only-widget.component';

@Component({
  selector: 'app-case-form',
  templateUrl: './case-form.component.html',
  styleUrls: ['./case-form.component.scss']
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
  // Add custom widget
  public widgets = {
    "file": FileWidgetComponent,
    "submit": SubmitWidgetComponent,
    "select": SelectWidgetComponent,
    "lookup": LookupWidgetComponent,
    "lookup-selector": LookupSelectorWidgetComponent,
    "date": DateWidgetComponent,
    "currency": CurrencyWidgetComponent,
    "text": InputWidgetComponent,
    "number": InputWidgetComponent,
    "textarea": TextAreaWidgetComponent,
    "wysiwyg": WysiwygWidgetComponent,
    "href": HrefWidgetComponent,
    "label-only": LabelOnlyWidgetComponent
  };
  // Add custom framework
  public framework = {
    framework: TailwindFrameworkComponent
  };
  public jsonFormOptions: any = {
    addSubmit: false, // Don't show a submit button if layout does not have one
    setSchemaDefaults: false, // Always use schema defaults for empty fields
    draft: false
  };
  private copiedLayout: any;
  private initialData: any;
  public formIsDirty: boolean = false;
  public showForm = true;
  public data: any;
  public schema: any;
  public layout: any;
  public lastChange: any;
  onChangeCallbackDictionary: any = {};

  @ViewChild('formContainer') formContainer: ElementRef | undefined;

  constructor(
    private router: Router,
    private _toaster: ToasterService,
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
    // since layout is reset we need to transform it
    if (!this.formEditable) {
      this.transformLayout(layout, !this.formEditable, this.case.draft);
      // here we need to check if case has become editable: we need to "enable" checkboxes
      // note: this won't affect checkboxes that have "disableCheckbox" class in their layout!
      if (this.formContainer?.nativeElement && (this.formEditable || this.case?.draft)) {
        this.formContainer.nativeElement.classList.remove('disableCheckbox');
      }
    }
    this.jsonFormOptions.draft = this.case.draft;
    this.jsonFormOptions.setSchemaDefaults = this.case.draft; // use schema defaults only for draft cases!
    this.jsonFormOptions.addSubmit = this.case.draft || this.formEditable;
    this.schema = (typeof this.case.caseType?.dataSchema! !== 'string') ? this.case.caseType?.dataSchema! : JSON.parse(this.case.caseType?.dataSchema!);
    this.layout = layout;
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
      // what is the key of the dictionary here? the dataPointer (e.g. "/homeAddress/attachmentId") that was added in the file-upload widget
      for (const key in this._fileUploadService.files) {
        if (this._fileUploadService.files.hasOwnProperty(key)) {
          // we may have 'garbage' attachments that we should not send to server!!!
          let path = key;
          path = path.replace(/\//g, '.'); // https://stackoverflow.com/a/63616567/19162333
          path = path.substring(1);
          if (get(event, path)) { // https://lodash.com/docs/4.17.15#get
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
                  // transform path to object key (eg. "/home/attachmentId") to a string that lodash update (https://lodash.com/docs/4.17.15#update)
                  // can understand (eg "home.attachmentId") and update its value with the response attachment id
                  update(event, key.slice(1).replace('/', '.'), () => response[key].id!);
                }
              }

              if (this.case?.draft) {
                // finally, submit the case
                this._api.submitAdminCase(this.case?.id!, event)
                  .pipe(
                    tap(() => {
                      this._fileUploadService.reset();
                      this._toaster.show(ToastType.Success, 'Επιτυχής Καταχώριση', `Η αρχική καταχώριση της υπόθεσης ολοκληρώθηκε.`, 5000);
                      this.updateDataEvent.emit({ draft: true });
                    }),
                    catchError((err: ProblemDetails) => { // error during case submit
                      this._toaster.show(ToastType.Error, 'Αποτυχία Καταχώρισης', err.detail || `Δεν κατέστη εφικτή η καταχώριση της υπόθεσής σας.`, 5000);
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
                      this._toaster.show(ToastType.Success, 'Επιτυχής αποθήκευση', `Οι αλλαγές σας αποθηκεύτηκαν με επιτυχία.`, 5000);
                      this._fileUploadService.reset();
                    }),
                    catchError(() => {
                      this._toaster.show(ToastType.Error, 'Αποτυχία αποθήκευσης', `Δεν κατέστη εφικτό να αποθηκευτούν οι αλλαγές σας.`, 5000);
                      return EMPTY;
                    })
                  )
                  .subscribe();
              }
            }),
          catchError(() => { // error during attachments upload
            this._toaster.show(ToastType.Error, 'Αποτυχία Καταχώρισης', `Προέκυψε πρόβλημα κατά την αποθήκευση των εγγράφων.`, 5000);
            return EMPTY;
          }))
        .subscribe();
  }

  // property-level onChange implementation from:
  // https://github.com/hamzahamidi/ajsf/issues/37
  onFormChanges(entity: any) {
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

    // ajsf's (onChanges) is very trigger happy and often is invoked with a fake empty
    // object, so ignore it
    if (Object.keys(entity).length === 0 && entity.constructor === Object) {
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

  private transformLayout(layout: any, viewOnlyMode: boolean = true, draft?: boolean): void {
    // first check for draft, and override json layout
    if (draft) {
      // case is draft, we need all form properties available and ignore layout restrictions (eg readonly, or html class disable)
      this.removeReadonlyProperties(layout);
      return;
    }
    // case is not draft, check for editability
    if (!viewOnlyMode) {
      // form is editable
      layout = this.copiedLayout;
      this.jsonFormOptions.addSubmit = true;
    } else {
      this.addReadonlyProperties(layout);
    }
  }

  private removeReadonlyProperties(layout: any): void {
    const activateElement = (element: any) => {
      delete element.readonly;
      if (element.htmlClass && element.htmlClass.indexOf('disableCheckbox') > -1) {
        element.htmlClass = element.htmlClass.replace('disableCheckbox', '');
      }
    }

    layout?.forEach((element: any) => {
      activateElement(element);
      if (element.hasOwnProperty('items')) {
        this.removeReadonlyProperties(element.items);;
      }
    });
  }

  private addReadonlyProperties(layout: any): void {
    // form is view-only -> add readonly property to all objects of layout object!
    layout?.forEach((element: any) => {
      element.readonly = "true";
      if (element.hasOwnProperty('items')) { // ajsf sections may have items, which may be flex containers, which may have items...
        this.addReadonlyProperties(element.items);
      }
    });
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
