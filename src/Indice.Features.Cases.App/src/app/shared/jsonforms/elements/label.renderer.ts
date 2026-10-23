import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { JsonFormsAngularService, JsonFormsBaseRenderer } from '@jsonforms/angular';
import { getAjv, getConfig, getData, isVisible, JsonFormsState, LabelElement } from '@jsonforms/core';
import { CaseDetailsService } from 'src/app/core/services/case-details.service';
import { addClasses, arrayIndicesOf, evaluateCondition, isShownForChannel } from '../jsonforms-utilities';

/** JSON Forms `Label` element (`{ type: 'Label', text }`): a caption between fields. */
@Component({
    selector: 'app-jf-label',
    template: `
    @if (shown && text) {
      <p [class]="wrapperClass">{{ text }}</p>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager
})
export class LabelRendererComponent extends JsonFormsBaseRenderer<LabelElement> implements OnInit {
    private jsonFormsService = inject(JsonFormsAngularService);
    private caseDetailsService = inject(CaseDetailsService);
    options: any = {};
    text = '';
    shown = true;
    wrapperClass = '';

    ngOnInit(): void {
        this.options = this.uischema.options ?? {};
        this.text = this.uischema.text ?? '';
        this.wrapperClass = addClasses(this.options.htmlClass, 'schema-form-label', 'pt-3 pb-1 text-sm font-semibold text-base-content/80');
        this.addSubscription(this.jsonFormsService.$state.subscribe((state: JsonFormsState) => {
            const data = getData(state);
            this.shown = isVisible(this.uischema, data, this.path, getAjv(state), getConfig(state))
                && isShownForChannel(this.options, this.caseDetailsService.caseDetails?.channel)
                && evaluateCondition(this.options.condition, data, arrayIndicesOf(this.path));
        }));
    }
}
