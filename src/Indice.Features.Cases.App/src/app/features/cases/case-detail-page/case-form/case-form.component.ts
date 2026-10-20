import { ChangeDetectionStrategy, ChangeDetectorRef, Component, ElementRef, EventEmitter, inject, Input, OnChanges, OnDestroy, Output, SimpleChanges, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { JsonFormsModule } from '@jsonforms/angular';
import { createAjv, Generate } from '@jsonforms/core';
import { NgIcon } from '@ng-icons/core';
import { TranslatePipe } from '@ngx-translate/core';
import { get, isEqual, update } from 'lodash';
import { EMPTY, forkJoin, Observable, of } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { Case, CasesApiService, CasesAttachmentLink, EditCaseRequest, ProblemDetails } from 'src/app/core/services/cases-api.service';
import { FileUploadService } from 'src/app/core/services/file-upload.service';
import { CasesJsonFormsConfig } from 'src/app/shared/jsonforms/jsonforms-utilities';
import { caseFormRenderers } from 'src/app/shared/jsonforms/renderers';
import { ToastService, ToastType } from 'src/app/ui/feedback/toast.service';

/**
 * The case data, rendered from the case type's JSON schema and UI schema (ajsf layouts are adapted server-side).
 * Draft cases are submitted, editable cases are saved, everything else is read-only. Files picked in the form are uploaded
 * first and their attachment ids written into the data before it is sent.
 *
 * Kept imperative (inputs + ngOnChanges, eager change detection) because the JSON Forms host is rebuilt on every case change.
 */
@Component({
  selector: 'app-case-form',
  imports: [JsonFormsModule, NgIcon, TranslatePipe],
  template: `
    @if (case) {
      <div class="case-form form-container" #formContainer>
        @if (case.draft) {
          <div role="status" class="alert alert-soft alert-info mb-5 items-start">
            <ng-icon name="lucideInfo" size="1.2rem" class="mt-0.5" />
            <div>
              <p class="font-semibold">{{ 'cases.newCase' | translate }}</p>
              <p class="text-[0.8125rem]">{{ 'caseDetails.form.newCaseInfo' | translate }}</p>
            </div>
          </div>
        }
        @if (showForm) {
          <jsonforms
            (keydown.enter)="preventImplicitSubmit($event)"
            [schema]="schema"
            [data]="data"
            [uischema]="uiSchema"
            [renderers]="renderers"
            [readonly]="readonly"
            [ajv]="ajv"
            [config]="formsConfig"
            (dataChange)="onFormChanges($event)"
            (errors)="isValid(!$event?.length)" />
        }
        @if (!case.draft && formEditable) {
          <div class="mt-4 flex justify-end">
            <button type="button" class="btn btn-ghost" [disabled]="!formIsDirty" (click)="onCancel()">
              <ng-icon name="lucideUndo2" size="1rem" /> {{ 'caseDetails.form.discardChanges' | translate }}
            </button>
          </div>
        }
      </div>
    }
  `,
  changeDetection: ChangeDetectionStrategy.Eager
})
export class CaseFormComponent implements OnChanges, OnDestroy {
  private readonly router = inject(Router);
  private readonly toaster = inject(ToastService);
  private readonly api = inject(CasesApiService);
  private readonly fileUploadService = inject(FileUploadService);
  private readonly changeDetector = inject(ChangeDetectorRef);

  private latestModel: any;
  private latestIsValid = false;
  private initialData: any;
  /** The form is editable. */
  @Input() formEditable: boolean | undefined;
  @Input() case: Case | undefined;
  /** Asks the page to refresh the case. */
  @Output() updateDataEvent = new EventEmitter<{ draft: boolean }>();
  @Output() formIsValid = new EventEmitter<boolean>();
  @Output() unSavedChanges = new EventEmitter<boolean>();
  @ViewChild('formContainer') formContainer: ElementRef | undefined;

  public renderers = caseFormRenderers;
  public formsConfig: CasesJsonFormsConfig = {};
  public ajv = createAjv();
  public readonly = false;
  public formIsDirty = false;
  public showForm = true;
  public data: any;
  public schema: any;
  /** The ajsf layout, still read for its onChange / onInit scripts. */
  public layout: any;
  public uiSchema: any;
  public lastChange: any;
  private onChangeCallbackDictionary: any = {};

  public ngOnDestroy(): void {
    // Files picked for upload belong to this case only.
    this.fileUploadService.reset();
  }

  public ngOnChanges(changes: SimpleChanges): void {
    if (!this.case) {
      return;
    }
    if (changes.hasOwnProperty('case')) {
      this.showForm = false;
      this.changeDetector.detectChanges(); // drop the previous form right away
      /*
       * Delete empty strings and empty objects in the data, as ajsf did at initialisation, so that onChange scripts and the
       * dirty check compare like with like.
       */
      if (this.case.data) {
        this.initialData = this.removeEmptyObjects(this.deleteEmptyStringProperties(this.case.data));
      }
    }
    const layout = (typeof this.case.caseType?.layout !== 'string') ? this.case.caseType?.layout : JSON.parse(this.case.caseType?.layout!);
    this.schema = (typeof this.case.caseType?.dataSchema !== 'string') ? this.case.caseType?.dataSchema : JSON.parse(this.case.caseType?.dataSchema!);
    // No UI schema: generate the one JSON Forms would, so a submit button can be added to it.
    const uiSchema = structuredClone(this.case.caseType?.uiSchema) ?? Generate.uiSchema(this.schema);
    if (!this.formEditable) {
      if (this.case.draft) {
        // A draft needs every field available, whatever the layout says (readonly, disableCheckbox).
        this.removeReadonlyProperties(uiSchema);
      }
      if (this.formContainer?.nativeElement && (this.formEditable || this.case?.draft)) {
        this.formContainer.nativeElement.classList.remove('disableCheckbox');
      }
    }
    if (this.case.draft || this.formEditable) {
      this.addSubmit(uiSchema);
    }
    // Neither draft nor editable: view only.
    this.readonly = !this.formEditable && !this.case.draft;
    this.formsConfig = { draft: this.case.draft, onSubmit: data => this.onSubmit(data) };
    this.ajv = createAjv({ useDefaults: !!this.case.draft }); // schema defaults apply to drafts only
    this.layout = layout;
    this.uiSchema = uiSchema;
    this.data = this.initialData;
    this.populateForm(this.data);
    this.showForm = true;
    this.changeDetector.detectChanges();
    if (this.readonly) {
      this.formContainer?.nativeElement.classList.add('disableCheckbox');
    }
  }

  public onSubmit(event: any): void {
    // Upload the files picked in the form first; the dictionary is keyed by the field's data path (e.g. homeAddress.attachmentId).
    const callsDict: { [key: string]: Observable<CasesAttachmentLink> } = { '': of({} as CasesAttachmentLink) };
    for (const key in this.fileUploadService.files) {
      // Skip files whose field was cleared afterwards.
      if (this.fileUploadService.files.hasOwnProperty(key) && get(event, key)) {
        const fileParam = { data: this.fileUploadService.files[key], fileName: this.fileUploadService.files[key].name };
        callsDict[key] = this.api.uploadAdminCaseAttachment(this.case?.id!, fileParam);
      }
    }
    forkJoin(callsDict)
      .pipe(
        tap((response: { [key: string]: CasesAttachmentLink }) => {
          // Write each attachment id into the field it was picked for.
          for (const key in response) {
            if (response.hasOwnProperty(key) && key !== '') {
              update(event, key, () => response[key].id!);
            }
          }
          if (this.case?.draft) {
            this.api.submitAdminCase(this.case?.id!, event)
              .pipe(
                tap(() => {
                  this.fileUploadService.reset();
                  this.toaster.show(ToastType.Success, 'toasts.caseSubmitted.title', 'toasts.caseSubmitted.body', 5000);
                  this.updateDataEvent.emit({ draft: true });
                }),
                catchError((err: ProblemDetails) => {
                  this.toaster.show(ToastType.Error, 'toasts.caseSubmitError.title', err.detail || 'toasts.caseSubmitError.body', 5000);
                  this.router.navigate(['/cases']);
                  return EMPTY;
                })
              )
              .subscribe();
          } else {
            this.api.editCase(this.case?.id!, new EditCaseRequest({ data: event }))
              .pipe(
                tap(() => {
                  this.initialData = event; // the saved data is the new baseline
                  this.formIsDirty = false;
                  this.unSavedChanges.emit(this.formIsDirty);
                  this.updateDataEvent.emit({ draft: false });
                  this.toaster.show(ToastType.Success, 'toasts.saveSuccess.title', 'toasts.saveSuccess.body', 5000);
                  this.fileUploadService.reset();
                }),
                catchError(() => {
                  this.toaster.show(ToastType.Error, 'toasts.saveError.title', 'toasts.saveError.body', 5000);
                  return EMPTY;
                })
              )
              .subscribe();
          }
        }),
        catchError(() => {
          this.toaster.show(ToastType.Error, 'toasts.caseSubmitError.title', 'toasts.attachmentsError.body', 5000);
          return EMPTY;
        }))
      .subscribe();
  }

  /** Property-level onChange scripts of the layout (https://github.com/hamzahamidi/ajsf/issues/37) and the dirty check. */
  public onFormChanges(entity: any): void {
    this.latestModel = entity;
    if (!this.case?.draft) {
      this.formIsDirty = !isEqual(entity, this.initialData);
      this.unSavedChanges.emit(this.formIsDirty);
    }
    if (!entity || (Object.keys(entity).length === 0 && entity.constructor === Object)) {
      return;
    }
    for (const [key, onChange] of Object.entries(this.onChangeCallbackDictionary)) {
      if (get(entity, key) === get(this.lastChange, key)) {
        continue;
      }
      if (typeof onChange === 'function') {
        onChange(get(entity, key), entity);
      }
    }
    this.lastChange = entity;
  }

  public isValid(valid: boolean): void {
    this.formIsValid.emit(valid);
    this.latestIsValid = valid;
  }

  public onSubmitExternal(): void {
    if (this.latestIsValid) {
      this.onSubmit(this.latestModel);
    }
  }

  /** Reverts unsaved edits to the last saved data. */
  public onCancel(): void {
    this.showForm = false;
    this.changeDetector.detectChanges();
    this.data = this.initialData;
    this.populateForm(this.data);
    this.formIsDirty = false;
    this.unSavedChanges.emit(false);
    this.showForm = true;
    this.changeDetector.detectChanges();
  }

  /** Enter in a text field must not trigger a submit; textareas still get their new lines. */
  public preventImplicitSubmit(event: Event): void {
    if (!(event.target instanceof HTMLTextAreaElement)) {
      event.preventDefault();
    }
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

  private deleteEmptyStringProperties(obj: any): any {
    for (const k in obj) {
      if (!obj[k] || typeof obj[k] !== 'object') {
        if (obj[k] === '') {
          delete obj[k];
        }
        continue;
      }
      this.deleteEmptyStringProperties(obj[k]);
    }
    return obj;
  }

  private removeEmptyObjects(obj: any): any {
    for (const k in obj) {
      if (!obj[k] || typeof obj[k] !== 'object') {
        continue;
      }
      this.removeEmptyObjects(obj[k]);
      if (Object.keys(obj[k]).length === 0) {
        delete obj[k];
      }
    }
    return obj;
  }

  /** Registers the layout's onChange scripts and runs its onInit scripts against the initial data. */
  private populateForm(entity: any): void {
    this.lastChange = entity;
    const onInitCallbackDictionary: any = {};
    if (!Array.isArray(this.layout)) {
      return;
    }
    const traverse = (item: any) => {
      if (item.items) {
        item.items.map(traverse);
      } else if (item.tabs) {
        item.tabs.map(traverse);
      }
      if (item.onChange) {
        this.onChangeCallbackDictionary[item.key] = Function('"use strict";return (' + item.onChange + ')')();
      }
      if (item.onInit) {
        onInitCallbackDictionary[item.key] = Function('"use strict";return (' + item.onInit + ')')();
      }
    };
    this.layout.map(traverse);
    for (const [key, onInit] of Object.entries(onInitCallbackDictionary)) {
      if (typeof onInit === 'function') {
        const initEntity = onInit(get(entity, key), entity, this.case?.metadata);
        if (initEntity) {
          this.data = Object.assign({}, this.data, initEntity);
        }
      }
    }
  }
}
