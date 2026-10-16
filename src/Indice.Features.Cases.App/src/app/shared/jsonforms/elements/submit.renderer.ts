import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { JsonFormsAngularService, JsonFormsBaseRenderer } from '@jsonforms/angular';
import { getConfig, getData, JsonFormsState, UISchemaElement } from '@jsonforms/core';
import { addClasses, arrayIndicesOf, CasesJsonFormsConfig, evaluateCondition, isTrue } from '../jsonforms-utilities';

/**
 * `Submit` element (ajsf `submit` widget). Hidden when the form is read-only, disabled while the data is invalid
 * (unless the layout sets `disabled`). Clicking calls `onSubmit` of the `<jsonforms>` config with the form data.
 */
@Component({
    selector: 'app-jf-submit',
    template: `
    @if (shown) {
      <div [class]="wrapperClass">
        <button
          type="button"
          class="btn-submit-form"
          [class]="options.fieldHtmlClass || ''"
          [ngClass]="{ 'opacity-50 cursor-not-allowed': disabled }"
          [disabled]="disabled"
          (click)="submit()">
          <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round" d="M8 7H5a2 2 0 00-2 2v9a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-3m-1 4l-3 3m0 0l-3-3m3 3V4" />
          </svg>&nbsp;{{ draft ? 'Δημιουργία Νέας Υπόθεσης' : 'Αποθήκευση' }}
        </button>
      </div>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class SubmitRendererComponent extends JsonFormsBaseRenderer<UISchemaElement> implements OnInit {
    private jsonFormsService = inject(JsonFormsAngularService);
    private data: any;
    private config: CasesJsonFormsConfig = {};
    options: any = {};
    shown = true;
    disabled = false;
    draft = false;
    wrapperClass = '';

    ngOnInit(): void {
        this.options = this.uischema.options ?? {};
        this.wrapperClass = addClasses(this.options.htmlClass, 'schema-form-submit', 'form-group');
        this.addSubscription(this.jsonFormsService.$state.subscribe((state: JsonFormsState) => {
            this.data = getData(state);
            this.config = getConfig(state) ?? {};
            this.draft = !!this.config.draft;
            this.shown = !state.jsonforms.readonly && !isTrue(this.options.readonly) && evaluateCondition(this.options.condition, this.data, arrayIndicesOf(this.path));
            this.disabled = this.options.disabled !== undefined ? isTrue(this.options.disabled) : (state.jsonforms.core?.errors?.length ?? 0) > 0;
        }));
    }

    submit(): void {
        this.config.onSubmit?.(this.data);
    }
}
