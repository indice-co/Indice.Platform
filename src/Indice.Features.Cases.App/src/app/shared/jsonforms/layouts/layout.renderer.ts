import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { JsonFormsAngularService, JsonFormsBaseRenderer, JsonFormsOutlet } from '@jsonforms/angular';
import { getData, JsonFormsState, Layout, mapStateToLayoutProps, OwnPropsOfRenderer } from '@jsonforms/core';
import { CaseDetailsService } from 'src/app/core/services/case-details.service';
import { addClasses, arrayIndicesOf, evaluateCondition, flexItemStyle, gapAwareBasis, isShownForChannel, isSpacer } from '../jsonforms-utilities';

const COLUMN_GAP = '1rem';

/**
 * `VerticalLayout`, `HorizontalLayout`, `Group` and `Category`, drawn like the ajsf `section` / `flex` containers:
 * flex direction and wrap from `options['flex-flow']`, child sizing from each child's `options.flex`; a `Group` is a bordered
 * fieldset with its title as legend. In a row, percentage sizes make room for the column gap and an empty `Html` spacer
 * takes the place of the gap before it, so that ajsf rows such as `49% | 2% | 49%` still fit on one line.
 */
@Component({
    selector: 'app-jf-layout',
    imports: [JsonFormsOutlet, NgTemplateOutlet],
    template: `
    @if (shown) {
      @if (groupLabel !== null) {
        <fieldset [class]="wrapperClass">
          @if (groupLabel) {
            <legend class="fieldset-legend px-1 text-base font-semibold" [class]="options.labelHtmlClass || ''" [innerHTML]="groupLabel"></legend>
          }
          <ng-container *ngTemplateOutlet="body" />
        </fieldset>
      } @else {
        <div [class]="wrapperClass">
          <ng-container *ngTemplateOutlet="body" />
        </div>
      }
      <ng-template #body>
        <div
          [style.display]="flex ? 'flex' : null"
          [style.flex-direction]="flex ? flexDirection : null"
          [style.flex-wrap]="flex ? flexWrap : null"
          [style.column-gap]="flex ? columnGap : null"
          [style.justify-content]="flex ? options['justify-content'] : null"
          [style.align-items]="flex ? options['align-items'] : null"
          [style.align-content]="flex ? options['align-content'] : null">
          @for (child of children; track $index) {
            <div
              class="min-w-0"
              [style.flex-grow]="child.style.grow"
              [style.flex-shrink]="child.style.shrink"
              [style.flex-basis]="child.style.basis"
              [style.align-self]="child.style.alignSelf"
              [style.order]="child.style.order"
              [style.margin-inline-start]="child.marginStart"
              [style.margin-inline-end]="child.marginEnd">
              <jsonforms-outlet [renderProps]="child.props"></jsonforms-outlet>
            </div>
          }
        </div>
      </ng-template>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager
})
export class LayoutRendererComponent extends JsonFormsBaseRenderer<Layout> implements OnInit {
    private jsonFormsService = inject(JsonFormsAngularService);
    private caseDetailsService = inject(CaseDetailsService);
    options: any = {};
    children: { props: OwnPropsOfRenderer, style: ReturnType<typeof flexItemStyle>, marginStart: string | null, marginEnd: string | null }[] = [];
    readonly columnGap = COLUMN_GAP;
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
        // '' for an untitled group (still drawn as a fieldset), null for other layouts
        this.groupLabel = this.uischema.type === 'Group' ? (typeof label === 'string' ? label : '') : null;
        const row = this.flex && this.flexDirection.startsWith('row');
        const reverse = this.flexDirection === 'row-reverse';
        this.children = (this.uischema.elements ?? []).map(element => {
            const style = flexItemStyle(element.options);
            const spacer = row && isSpacer(element);
            return {
                props: { uischema: element, schema: this.schema, path: this.path },
                style: row && !spacer ? { ...style, basis: gapAwareBasis(style.basis, COLUMN_GAP) } : style,
                marginStart: spacer && !reverse ? `-${COLUMN_GAP}` : null,
                marginEnd: spacer && reverse ? `-${COLUMN_GAP}` : null
            };
        });
        this.addSubscription(this.jsonFormsService.$state.subscribe((state: JsonFormsState) => this.update(state)));
    }

    private update(state: JsonFormsState): void {
        // The root layout (the top-level ajsf array) had no container of its own.
        const isRoot = this.uischema === state.jsonforms.core?.uischema;
        this.wrapperClass = isRoot || this.uischema.type === 'Category'
            ? ''
            : addClasses(
                this.options.htmlClass,
                this.flex ? 'schema-form-flex' : 'schema-form-section',
                this.uischema.type === 'Group' ? 'fieldset my-3 rounded-box border border-base-300 px-4 pb-3' : 'py-1');
        const { visible } = mapStateToLayoutProps(state, { uischema: this.uischema, schema: this.schema, path: this.path });
        this.shown = visible
            && isShownForChannel(this.options, this.caseDetailsService.caseDetails?.channel)
            && evaluateCondition(this.options.condition, getData(state), arrayIndicesOf(this.path));
    }
}
