import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { JsonFormsAngularService, JsonFormsBaseRenderer, JsonFormsOutlet } from '@jsonforms/angular';
import { Categorization, getData, JsonFormsState, mapStateToLayoutProps, OwnPropsOfRenderer } from '@jsonforms/core';
import { CaseDetailsService } from 'src/app/core/services/case-details.service';
import { addClasses, arrayIndicesOf, evaluateCondition, isShownForChannel } from '../jsonforms-utilities';

/** `Categorization` (ajsf `tabs`): one tab per `Category`. */
@Component({
    selector: 'app-jf-categorization',
    imports: [JsonFormsOutlet],
    template: `
    @if (shown) {
      <div [class]="wrapperClass">
        <div role="tablist" class="tabs tabs-border mb-4 flex-nowrap overflow-x-auto">
          @for (tab of tabs; track $index) {
            <button
              type="button"
              role="tab"
              class="tab whitespace-nowrap"
              [class.tab-active]="$index === selected"
              [attr.aria-selected]="$index === selected"
              [innerHTML]="tab.label"
              (click)="selected = $index"></button>
          }
        </div>
        @for (tab of tabs; track $index) {
          <div role="tabpanel" [hidden]="$index !== selected">
            <jsonforms-outlet [renderProps]="tab.props"></jsonforms-outlet>
          </div>
        }
      </div>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager
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
        this.wrapperClass = addClasses(this.options.htmlClass, 'schema-form-tabs', 'py-1');
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
