import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { JsonFormsAngularService, JsonFormsBaseRenderer } from '@jsonforms/angular';
import { getData, JsonFormsState, UISchemaElement } from '@jsonforms/core';
import { CaseDetailsService } from 'src/app/core/services/case-details.service';
import { addClasses, arrayIndicesOf, evaluateCondition, isShownForChannel } from '../jsonforms-utilities';

/** `Html` element (ajsf `html` / `help`): the `helpvalue` markup. */
@Component({
    selector: 'app-jf-html',
    template: `
    @if (shown) {
      <div [class]="wrapperClass">
        <div [innerHTML]="options.helpvalue ?? options.description"></div>
      </div>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class HtmlRendererComponent extends JsonFormsBaseRenderer<UISchemaElement> implements OnInit {
    private jsonFormsService = inject(JsonFormsAngularService);
    private caseDetailsService = inject(CaseDetailsService);
    options: any = {};
    shown = true;
    wrapperClass = '';

    ngOnInit(): void {
        this.options = this.uischema.options ?? {};
        this.wrapperClass = addClasses(this.options.htmlClass, 'schema-form-html', 'form-group');
        this.addSubscription(this.jsonFormsService.$state.subscribe((state: JsonFormsState) => {
            this.shown = isShownForChannel(this.options, this.caseDetailsService.caseDetails?.channel)
                && evaluateCondition(this.options.condition, getData(state), arrayIndicesOf(this.path));
        }));
    }
}
