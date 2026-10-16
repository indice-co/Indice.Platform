import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { JsonFormsAngularService, JsonFormsBaseRenderer } from '@jsonforms/angular';
import { Categorization, getData, JsonFormsState, mapStateToLayoutProps, OwnPropsOfRenderer } from '@jsonforms/core';
import { CaseDetailsService } from 'src/app/core/services/case-details.service';
import { addClasses, arrayIndicesOf, evaluateCondition, isShownForChannel } from '../jsonforms-utilities';

/** `Categorization` (ajsf `tabs`): one tab per `Category`. */
@Component({
    selector: 'app-jf-categorization',
    template: `
    @if (shown) {
      <div [class]="wrapperClass">
        <nav class="flex gap-4 border-b border-gray-200 mb-4">
          @for (tab of tabs; track $index) {
            <button
              type="button"
              class="py-2 px-1 border-b-2 text-sm font-medium"
              [class.border-indigo-500]="$index === selected"
              [class.text-indigo-600]="$index === selected"
              [class.border-transparent]="$index !== selected"
              [class.text-gray-500]="$index !== selected"
              [innerHTML]="tab.label"
              (click)="selected = $index"></button>
          }
        </nav>
        @for (tab of tabs; track $index) {
          <div [hidden]="$index !== selected">
            <jsonforms-outlet [renderProps]="tab.props"></jsonforms-outlet>
          </div>
        }
      </div>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class CategorizationRendererComponent extends JsonFormsBaseRenderer<Categorization> implements OnInit {
    private jsonFormsService = inject(JsonFormsAngularService);
    private caseDetailsService = inject(CaseDetailsService);
    options: any = {};
    tabs: { label: string, props: OwnPropsOfRenderer }[] = [];
    selected = 0;
    shown = true;
    wrapperClass = '';

    ngOnInit(): void {
        this.options = this.uischema.options ?? {};
        this.wrapperClass = addClasses(this.options.htmlClass, 'schema-form-tabs', 'form-group');
        this.tabs = (this.uischema.elements ?? []).map(category => ({
            label: typeof category.label === 'string' ? category.label : '',
            props: { uischema: category, schema: this.schema, path: this.path }
        }));
        this.addSubscription(this.jsonFormsService.$state.subscribe((state: JsonFormsState) => {
            const { visible } = mapStateToLayoutProps(state, { uischema: this.uischema, schema: this.schema, path: this.path });
            this.shown = visible
                && isShownForChannel(this.options, this.caseDetailsService.caseDetails?.channel)
                && evaluateCondition(this.options.condition, getData(state), arrayIndicesOf(this.path));
        }));
    }
}
