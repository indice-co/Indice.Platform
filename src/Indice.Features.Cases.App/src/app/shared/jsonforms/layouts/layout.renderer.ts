import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { JsonFormsAngularService, JsonFormsBaseRenderer } from '@jsonforms/angular';
import { getData, JsonFormsState, Layout, mapStateToLayoutProps, OwnPropsOfRenderer } from '@jsonforms/core';
import { CaseDetailsService } from 'src/app/core/services/case-details.service';
import { addClasses, arrayIndicesOf, evaluateCondition, flexItemStyle, isShownForChannel } from '../jsonforms-utilities';

/**
 * `VerticalLayout`, `HorizontalLayout`, `Group` and `Category`, drawn like the ajsf `section` / `flex` containers:
 * flex direction and wrap from `options['flex-flow']`, child sizing from each child's `options.flex`, the group title as legend.
 */
@Component({
    selector: 'app-jf-layout',
    template: `
    @if (shown) {
      <div [class]="wrapperClass">
        @if (groupLabel) {
          <label class="legend" [class]="options.labelHtmlClass || ''" [innerHTML]="groupLabel"></label>
        }
        <div
          [style.display]="flex ? 'flex' : null"
          [style.flex-direction]="flex ? flexDirection : null"
          [style.flex-wrap]="flex ? flexWrap : null"
          [style.justify-content]="flex ? options['justify-content'] : null"
          [style.align-items]="flex ? options['align-items'] : null"
          [style.align-content]="flex ? options['align-content'] : null">
          @for (child of children; track $index) {
            <div
              [class.form-flex-item]="flex"
              [style.flex-grow]="child.style.grow"
              [style.flex-shrink]="child.style.shrink"
              [style.flex-basis]="child.style.basis"
              [style.align-self]="child.style.alignSelf"
              [style.order]="child.style.order">
              <jsonforms-outlet [renderProps]="child.props"></jsonforms-outlet>
            </div>
          }
        </div>
      </div>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class LayoutRendererComponent extends JsonFormsBaseRenderer<Layout> implements OnInit {
    private jsonFormsService = inject(JsonFormsAngularService);
    private caseDetailsService = inject(CaseDetailsService);
    options: any = {};
    children: { props: OwnPropsOfRenderer, style: ReturnType<typeof flexItemStyle> }[] = [];
    shown = true;
    flex = false;
    flexDirection = 'column';
    flexWrap = 'nowrap';
    groupLabel: string | null = null;
    wrapperClass = '';

    ngOnInit(): void {
        this.options = this.uischema.options ?? {};
        const flexFlow = (this.options['flex-flow'] ?? '').split(/\s+/);
        this.flex = this.uischema.type === 'HorizontalLayout' || !!flexFlow[0] || !!this.options['flex-direction'] || !!this.options.displayFlex || this.options.display === 'flex';
        this.flexDirection = flexFlow[0] || this.options['flex-direction'] || (this.uischema.type === 'HorizontalLayout' ? 'row' : 'column');
        this.flexWrap = flexFlow[1] || this.options['flex-wrap'] || 'nowrap';
        const label = (this.uischema as any).label;
        this.groupLabel = this.uischema.type === 'Group' && typeof label === 'string' ? label : null;
        this.children = (this.uischema.elements ?? []).map(element => ({
            props: { uischema: element, schema: this.schema, path: this.path },
            style: flexItemStyle(element.options)
        }));
        this.addSubscription(this.jsonFormsService.$state.subscribe((state: JsonFormsState) => this.update(state)));
    }

    private update(state: JsonFormsState): void {
        // The root layout (the top-level ajsf array) had no container of its own.
        const isRoot = this.uischema === state.jsonforms.core?.uischema;
        this.wrapperClass = isRoot || this.uischema.type === 'Category'
            ? ''
            : addClasses(this.options.htmlClass, this.flex ? 'schema-form-flex' : 'schema-form-section', 'form-group');
        const { visible } = mapStateToLayoutProps(state, { uischema: this.uischema, schema: this.schema, path: this.path });
        this.shown = visible
            && isShownForChannel(this.options, this.caseDetailsService.caseDetails?.channel)
            && evaluateCondition(this.options.condition, getData(state), arrayIndicesOf(this.path));
    }
}
