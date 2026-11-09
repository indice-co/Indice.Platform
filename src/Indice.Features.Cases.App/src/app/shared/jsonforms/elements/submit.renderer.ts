import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { JsonFormsAngularService, JsonFormsBaseRenderer } from '@jsonforms/angular';
import { getConfig, getData, JsonFormsState, UISchemaElement } from '@jsonforms/core';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideSave, LucideSend } from '@lucide/angular';
import { addClasses, arrayIndicesOf, CasesJsonFormsConfig, evaluateCondition, isTrue } from '../jsonforms-utilities';

/**
 * `Submit` element (ajsf `submit` widget). Hidden when the form is read-only, disabled while the data is invalid
 * (unless the layout sets `disabled`). Clicking calls `onSubmit` of the `<jsonforms>` config with the form data.
 */
@Component({
    selector: 'app-jf-submit',
    imports: [TranslatePipe, LucideSave, LucideSend],
    template: `
    @if (shown) {
      <div [class]="wrapperClass">
        <button
          type="button"
          class="btn btn-primary"
          [class]="options.fieldHtmlClass || ''"
          [disabled]="disabled"
          (click)="submit()">
          @if (draft) {
            <svg lucideSend class="size-4"></svg>
            {{ 'jsonforms.createCase' | translate }}
          } @else {
            <svg lucideSave class="size-4"></svg>
            {{ 'common.save' | translate }}
          }
        </button>
      </div>
    }`,
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
        this.wrapperClass = addClasses(this.options.htmlClass, 'schema-form-submit', 'pt-4');
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
