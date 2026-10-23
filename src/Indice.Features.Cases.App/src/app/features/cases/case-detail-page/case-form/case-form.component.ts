import { Component, OnDestroy, effect, inject, input, output, signal, untracked } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { JsonForms } from '@jsonforms/angular';
import { createAjv, Generate } from '@jsonforms/core';
import { EMPTY, forkJoin, Observable, of } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { get, isEqual, update } from 'lodash';
import { LucideInfo, LucidePencil, LucideRotateCcw } from '@lucide/angular';
import { Case, CasesApiService, CasesAttachmentLink, EditCaseRequest } from 'src/app/core/services/cases-api.service';
import { FileUploadService } from 'src/app/core/services/file-upload.service';
import { CasesJsonFormsConfig } from 'src/app/shared/jsonforms/jsonforms-utilities';
import { displayRenderers, tailwindRenderers } from 'src/app/shared/jsonforms/tailwind-renderers';
import { ToastType, TranslatedToasterService } from 'src/app/shared/services/translated-toaster.service';

/** Renders the case data with JSON Forms and handles submit (drafts) / save (editable cases). */
@Component({
  selector: 'app-case-form',
  templateUrl: './case-form.component.html',
  imports: [TranslatePipe, JsonForms, LucideInfo, LucidePencil, LucideRotateCcw]
})
export class CaseFormComponent implements OnDestroy {
  private readonly _router = inject(Router);
  private readonly _toaster = inject(TranslatedToasterService);
  private readonly _api = inject(CasesApiService);
  private readonly _fileUploadService = inject(FileUploadService);

  public readonly case = input<Case | undefined>();
  public readonly formEditable = input<boolean | undefined>(false);
  /** Asks the parent to refresh the case. */
  public readonly updateDataEvent = output<{ draft: boolean }>();
  public readonly formIsValid = output<boolean>();
  public readonly unSavedChanges = output<boolean>();

  /** The form renderers, or the label / value renderers of the read-only view. */
  protected readonly renderers = signal(tailwindRenderers);
  protected readonly formsConfig = signal<CasesJsonFormsConfig>({});
  protected readonly ajv = signal(createAjv());
  protected readonly readonly = signal(false);
  protected readonly formIsDirty = signal(false);
  protected readonly schema = signal<any>(undefined);
  protected readonly uiSchema = signal<any>(undefined);
  protected readonly data = signal<any>(undefined);
  /** Bumped to destroy and re-create the `<jsonforms>` element (a fresh form state). */
  protected readonly formKey = signal(0);

  private _previousCase: Case | undefined;
  private _latestModel: any;
  private _latestIsValid = false;
  private _initialData: any;
  /** The case type layout, still read for its onChange / onInit scripts. */
  private _layout: any;
  private _lastChange: any;
  private _onChangeCallbackDictionary: Record<string, unknown> = {};

  constructor() {
    effect(() => {
      const caseModel = this.case();
      const formEditable = this.formEditable();
      untracked(() => this.build(caseModel, !!formEditable));
    });
  }

  public ngOnDestroy(): void {
    // the uploaded / saved-for-upload files must not outlive the form
    this._fileUploadService.reset();
  }

  private build(caseModel: Case | undefined, formEditable: boolean): void {
    if (!caseModel) {
      return;
    }
    if (caseModel !== this._previousCase) {
      this._previousCase = caseModel;
      // delete empty strings and empty objects so that change detection against the initial data works as expected
      if (caseModel.data) {
        this._initialData = this.removeEmptyObjects(this.deleteEmptyStringProperties(caseModel.data));
      }
    }
    const schema = parse(caseModel.caseType?.dataSchema);
    // no UI schema: generate the one JSON Forms would generate, so that a submit can be added to it
    const uiSchema = structuredClone(caseModel.caseType?.uiSchema) ?? Generate.uiSchema(schema);
    if (!formEditable && caseModel.draft) {
      // a draft needs all form properties available, ignoring layout restrictions (readonly, disableCheckbox)
      this.removeReadonlyProperties(uiSchema);
    }
    if (caseModel.draft || formEditable) {
      this.addSubmit(uiSchema);
    }
    // not draft and not editable: the form is view-only
    this.readonly.set(!formEditable && !caseModel.draft);
    this.renderers.set(this.readonly() ? displayRenderers : tailwindRenderers);
    this.formsConfig.set({ draft: caseModel.draft, onSubmit: data => this.onSubmit(data) });
    this.ajv.set(createAjv({ useDefaults: !!caseModel.draft })); // schema defaults only for draft cases
    this._layout = parse(caseModel.caseType?.layout);
    this.schema.set(schema);
    this.uiSchema.set(uiSchema);
    this.data.set(this._initialData);
    this.populateForm(this._initialData);
    this.formKey.update(key => key + 1);
  }

  public onSubmit(event: any): void {
    const caseModel = this.case();
    // upload the user's documents first; keys are data paths (e.g. "homeAddress.attachmentId") set by the file control
    const callsDict: { [key: string]: Observable<CasesAttachmentLink> } = { '': of({} as CasesAttachmentLink) };
    const files = this._fileUploadService.files;
    for (const key in files) {
      // skip 'garbage' attachments that are no longer referenced by the data
      if (Object.prototype.hasOwnProperty.call(files, key) && get(event, key)) {
        callsDict[key] = this._api.uploadAdminCaseAttachment(caseModel?.id!, { data: files[key], fileName: files[key].name });
      }
    }
    forkJoin(callsDict)
      .pipe(
        tap(response => {
          // match the received attachment ids to their fields through the data paths
          for (const key in response) {
            if (Object.prototype.hasOwnProperty.call(response, key) && key !== '') {
              update(event, key, () => response[key].id!);
            }
          }
          if (caseModel?.draft) {
            this._api.submitAdminCase(caseModel.id!, event)
              .pipe(
                tap(() => {
                  this._fileUploadService.reset();
                  this._toaster.show(ToastType.Success, 'toasts.caseSubmitted.title', 'toasts.caseSubmitted.body', 5000);
                  this.updateDataEvent.emit({ draft: true });
                }),
                catchError(() => {
                  this._router.navigate(['/cases']);
                  return EMPTY;
                })
              )
              .subscribe();
          } else {
            this._api.editCase(caseModel?.id!, new EditCaseRequest({ data: event }))
              .pipe(
                tap(() => {
                  this._initialData = event; // the saved data are now the initial data
                  this.formIsDirty.set(false);
                  this.unSavedChanges.emit(false);
                  this.updateDataEvent.emit({ draft: false });
                  this._toaster.show(ToastType.Success, 'toasts.saveSuccess.title', 'toasts.saveSuccess.body', 5000);
                  this._fileUploadService.reset();
                }),
                catchError(() => EMPTY)
              )
              .subscribe();
          }
        }),
        catchError(() => EMPTY)
      )
      .subscribe();
  }

  /** Tracks dirtiness and runs the layout's property-level onChange scripts. */
  protected onFormChanges(entity: any): void {
    this._latestModel = entity;
    if (!this.case()?.draft) {
      const dirty = !isEqual(entity, this._initialData);
      this.formIsDirty.set(dirty);
      this.unSavedChanges.emit(dirty);
    }
    // ignore an empty form
    if (!entity || (Object.keys(entity).length === 0 && entity.constructor === Object)) {
      return;
    }
    for (const [key, onChange] of Object.entries(this._onChangeCallbackDictionary)) {
      if (get(entity, key) === get(this._lastChange, key)) {
        continue;
      }
      if (typeof onChange === 'function') {
        onChange(get(entity, key), entity);
      }
    }
    this._lastChange = entity;
  }

  protected isValid(valid: boolean): void {
    this.formIsValid.emit(valid);
    this._latestIsValid = valid;
  }

  public onSubmitExternal(): void {
    if (this._latestIsValid) {
      this.onSubmit(this._latestModel);
    }
  }

  protected onCancel(): void {
    const caseModel = this.case();
    this.schema.set(parse(caseModel?.caseType?.dataSchema));
    this._layout = parse(caseModel?.caseType?.layout);
    this.data.set(this._initialData);
    this.populateForm(this._initialData);
    this.formKey.update(key => key + 1);
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

  /** Adds a submit button at the end of the form when the layout has none. */
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

  /** Collects the layout's onChange scripts and applies its onInit scripts to the data. */
  private populateForm(entity: any): void {
    this._lastChange = entity;
    const onInitCallbackDictionary: Record<string, unknown> = {};
    if (!Array.isArray(this._layout)) {
      return;
    }
    const traverse = (item: any) => {
      if (item.items) {
        item.items.map(traverse);
      } else if (item.tabs) {
        item.tabs.map(traverse);
      }
      if (item.onChange) {
        this._onChangeCallbackDictionary[item.key] = Function('"use strict";return (' + item.onChange + ')')();
      }
      if (item.onInit) {
        onInitCallbackDictionary[item.key] = Function('"use strict";return (' + item.onInit + ')')();
      }
    };
    this._layout.map(traverse);
    for (const [key, onInit] of Object.entries(onInitCallbackDictionary)) {
      if (typeof onInit === 'function') {
        const initEntity = onInit(get(entity, key), entity, this.case()?.metadata);
        if (initEntity) {
          this.data.update(data => Object.assign({}, data, initEntity));
        }
      }
    }
  }
}

/** Case type schemas / layouts may arrive as JSON strings or as objects. */
function parse(value: any): any {
  return typeof value === 'string' ? JSON.parse(value) : value;
}
