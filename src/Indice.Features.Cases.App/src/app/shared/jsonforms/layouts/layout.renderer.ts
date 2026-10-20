import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { JsonFormsAngularService, JsonFormsBaseRenderer, JsonFormsOutlet } from '@jsonforms/angular';
import { getData, JsonFormsState, Layout, mapStateToLayoutProps, OwnPropsOfRenderer } from '@jsonforms/core';
import { CaseDetailsService } from 'src/app/core/services/case-details.service';
import { addClasses, arrayIndicesOf, evaluateCondition, flexItemStyle, isShownForChannel } from '../jsonforms-utilities';

/**
 * `VerticalLayout`, `HorizontalLayout`, `Group` and `Category`, drawn like the ajsf `section` / `flex` containers:
 * flex direction and wrap from `options['flex-flow']`, child sizing from each child's `options.flex`, a `Group` as a
 * fieldset with its label as legend. Children are spaced by the container's gap; hidden children take no room.
 */
@Component({
    selector: 'app-jf-layout',
    imports: [JsonFormsOutlet, NgTemplateOutlet],
    template: `
    @if (shown) {
      @if (isGroup) {
        <fieldset [class]="wrapperClass">
          @if (groupLabel) {
            <legend class="jf-legend legend" [class]="options.labelHtmlClass || ''" [innerHTML]="groupLabel"></legend>
          }
          <ng-container *ngTemplateOutlet="children_" />
        </fieldset>
      } @else {
        <div [class]="wrapperClass">
          <ng-container *ngTemplateOutlet="children_" />
        </div>
      }
    }
    <ng-template #children_>
      <div
        [class]="flex ? 'jf-flex' : 'jf-stack'"
        [class.jf-flex-row]="flex && flexDirection.startsWith('row')"
        [style.display]="flex ? 'flex' : null"
        [style.flex-direction]="flex ? flexDirection : null"
        [style.flex-wrap]="flex ? flexWrap : null"
        [style.justify-content]="flex ? options['justify-content'] : null"
        [style.align-items]="flex ? options['align-items'] : null"
        [style.align-content]="flex ? options['align-content'] : null">
        @for (child of children; track $index) {
          <div
            class="jf-item"
            [class.form-flex-item]="flex"
            [style.flex-grow]="flex ? child.style.grow : null"
            [style.flex-shrink]="flex ? child.style.shrink : null"
            [style.flex-basis]="flex ? child.style.basis : null"
            [style.align-self]="flex ? child.style.alignSelf : null"
            [style.order]="flex ? child.style.order : null">
            <jsonforms-outlet [renderProps]="child.props"></jsonforms-outlet>
          </div>
        }
      </div>
    </ng-template>`,
    host: { '[class.jf-hidden]': '!shown' },
    changeDetection: ChangeDetectionStrategy.Eager
})
export class LayoutRendererComponent extends JsonFormsBaseRenderer<Layout> implements OnInit {
    private jsonFormsService = inject(JsonFormsAngularService);
    private caseDetailsService = inject(CaseDetailsService);
    options: any = {};
    children: { props: OwnPropsOfRenderer, style: ReturnType<typeof flexItemStyle> }[] = [];
    shown = true;
    flex = false;
    flexDirection = 'column';
    /** `null` lets the stylesheet decide (no wrap, but wrap rows on narrow screens). */
    flexWrap: string | null = null;
    isGroup = false;
    groupLabel: string | null = null;
    wrapperClass = '';

    ngOnInit(): void {
        this.options = this.uischema.options ?? {};
        const flexFlow = (this.options['flex-flow'] ?? '').split(/\s+/);
        this.flex = this.uischema.type === 'HorizontalLayout' || !!flexFlow[0] || !!this.options['flex-direction'] || !!this.options.displayFlex || this.options.display === 'flex';
        this.flexDirection = flexFlow[0] || this.options['flex-direction'] || (this.uischema.type === 'HorizontalLayout' ? 'row' : 'column');
        this.flexWrap = flexFlow[1] || this.options['flex-wrap'] || null;
        const label = (this.uischema as any).label;
        this.isGroup = this.uischema.type === 'Group';
        this.groupLabel = this.isGroup && typeof label === 'string' ? label : null;
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
            ? 'jf-root'
            : addClasses(this.isGroup ? 'jf-group' : 'jf-section', 'form-group', this.flex ? 'schema-form-flex' : 'schema-form-section', this.options.htmlClass);
        const { visible } = mapStateToLayoutProps(state, { uischema: this.uischema, schema: this.schema, path: this.path });
        this.shown = visible
            && isShownForChannel(this.options, this.caseDetailsService.caseDetails?.channel)
            && evaluateCondition(this.options.condition, getData(state), arrayIndicesOf(this.path));
    }
}
