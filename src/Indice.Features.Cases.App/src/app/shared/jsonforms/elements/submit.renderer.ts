import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { JsonFormsAngularService, JsonFormsBaseRenderer } from '@jsonforms/angular';
import { getConfig, getData, JsonFormsState, UISchemaElement } from '@jsonforms/core';
import { NgIcon } from '@ng-icons/core';
import { TranslatePipe } from '@ngx-translate/core';
import { addClasses, arrayIndicesOf, CasesJsonFormsConfig, evaluateCondition, isTrue } from '../jsonforms-utilities';

/**
 * `Submit` element (ajsf `submit` widget). Hidden when the form is read-only, disabled while the data is invalid
 * (unless the layout sets `disabled`). Clicking calls `onSubmit` of the `<jsonforms>` config with the form data.
 */
@Component({
    selector: 'app-jf-submit',
    imports: [NgIcon, TranslatePipe],
    template: `
    @if (shown) {
      <div [class]="wrapperClass">
        <button
          type="button"
          class="btn btn-primary btn-submit-form"
          [class]="options.fieldHtmlClass || ''"
          [disabled]="disabled"
          (click)="submit()">
          <ng-icon [name]="draft ? 'lucideFilePlus2' : 'lucideSave'" size="1.05rem" />
          {{ (draft ? 'caseForm.createCase' : 'common.save') | translate }}
        </button>
      </div>
    }`,
    host: { '[class.jf-hidden]': '!shown' },
    changeDetection: ChangeDetectionStrategy.Eager
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
        this.wrapperClass = addClasses('jf-submit', 'form-group', 'schema-form-submit', this.options.htmlClass);
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
