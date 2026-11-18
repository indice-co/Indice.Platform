import { ChangeDetectionStrategy, Component, inject, OnInit, ViewEncapsulation } from '@angular/core';
import { JsonFormsAngularService, JsonFormsBaseRenderer, JsonFormsOutlet } from '@jsonforms/angular';
import { getData, getSchema, hasType, JsonFormsState, JsonSchema, Layout, mapStateToLayoutProps, OwnPropsOfRenderer, Resolve, UISchemaElement } from '@jsonforms/core';
import { CaseDetailsService } from 'src/app/core/services/case-details.service';
import { addClasses, arrayIndicesOf, evaluateCondition, flexItemStyle, isShownForChannel, isSpacer, isTrue } from '../jsonforms-utilities';

/** Widgets whose value reads better across the whole width. */
const WIDE_WIDGETS = ['textarea', 'wysiwyg'];

/**
 * `VerticalLayout`, `HorizontalLayout`, `Group` and `Category` of the read-only case view: label / value pairs in two columns
 * (one on phones), a `Group` titled by a heading. Layouts, nested objects, arrays and long text span both columns;
 * horizontal layouts (and ajsf `flex-flow: row`) keep their fields side by side in as many aligned columns as fit,
 * a child sized `100%` taking a whole line. Empty `Html` spacers (the ajsf gutters) are left out; the grid has its own gaps.
 */
@Component({
    selector: 'app-jf-display-layout',
    imports: [JsonFormsOutlet],
    template: `
    @if (shown) {
      <section [class]="wrapperClass">
        @if (heading) {
          <h3 class="mb-3 border-b border-base-300 pb-1.5 text-sm font-semibold" [class]="options.labelHtmlClass || ''" [innerHTML]="heading"></h3>
        }
        <div [class]="row ? 'grid grid-cols-[repeat(auto-fit,minmax(min(12rem,100%),1fr))] gap-x-6 gap-y-4' : 'grid grid-cols-1 gap-x-6 gap-y-4 sm:grid-cols-2'">
          @for (child of children; track $index) {
            <div [class]="'jf-display-cell min-w-0 ' + child.cellClass">
              <jsonforms-outlet [renderProps]="child.props"></jsonforms-outlet>
            </div>
          }
        </div>
      </section>
    }`,
    // A hidden field leaves its cell empty (the outlet inserts the field next to itself): drop such cells so they leave no gap.
    styles: [`.jf-display-cell:not(:has(> :not(jsonforms-outlet) > *)) { display: none; }`],
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.Eager
})
export class DisplayLayoutRendererComponent extends JsonFormsBaseRenderer<Layout> implements OnInit {
    private jsonFormsService = inject(JsonFormsAngularService);
    private caseDetailsService = inject(CaseDetailsService);
    options: any = {};
    children: { props: OwnPropsOfRenderer, cellClass: string }[] = [];
    shown = true;
    row = false;
    heading: string | null = null;
    wrapperClass = '';

    ngOnInit(): void {
        this.options = this.uischema.options ?? {};
        const flexFlow: string = this.options['flex-flow'] ?? this.options['flex-direction'] ?? '';
        this.row = this.uischema.type === 'HorizontalLayout' || flexFlow.startsWith('row');
        const label = (this.uischema as any).label;
        this.heading = this.uischema.type === 'Group' && typeof label === 'string' && label ? label : null;
        let rootSchema: JsonSchema | undefined;
        this.addSubscription(this.jsonFormsService.$state.subscribe((state: JsonFormsState) => {
            if (!rootSchema) {
                rootSchema = getSchema(state);
                this.children = (this.uischema.elements ?? []).map(element => ({
                    props: { uischema: element, schema: this.schema, path: this.path },
                    cellClass: this.cellClass(element, rootSchema!)
                }));
            }
            this.update(state);
        }));
    }

    private update(state: JsonFormsState): void {
        const isRoot = this.uischema === state.jsonforms.core?.uischema;
        this.wrapperClass = addClasses(
            !isRoot && this.options.htmlClass,
            this.uischema.type === 'Group' ? 'schema-form-section pt-2' : 'schema-form-section');
        const { visible } = mapStateToLayoutProps(state, { uischema: this.uischema, schema: this.schema, path: this.path });
        this.shown = visible
            && isShownForChannel(this.options, this.caseDetailsService.caseDetails?.channel)
            && evaluateCondition(this.options.condition, getData(state), arrayIndicesOf(this.path));
    }

    private cellClass(element: UISchemaElement, rootSchema: JsonSchema): string {
        if (isSpacer(element)) {
            // still rendered: a spacer may carry a condition script with side effects
            return 'hidden';
        }
        if (this.row) {
            const basis = flexItemStyle(element.options).basis.trim();
            return basis.endsWith('%') && parseFloat(basis) >= 100 ? 'col-span-full' : '';
        }
        return this.isWide(element, rootSchema) ? 'sm:col-span-2' : '';
    }

    private isWide(element: UISchemaElement, rootSchema: JsonSchema): boolean {
        if (element.type !== 'Control') {
            // layouts, groups, captions and html blocks; a LabelOnly is a value like any other
            return element.type !== 'LabelOnly';
        }
        const options = element.options ?? {};
        if (isTrue(options['multi']) || WIDE_WIDGETS.includes(options['widget']) || isTrue(options['table'])) {
            return true;
        }
        const scope: string | undefined = (element as any).scope;
        const schema = scope ? Resolve.schema(this.schema, scope, rootSchema) : undefined;
        if (!schema) {
            return false;
        }
        if (schema.oneOf && !schema.oneOf.every(s => 'const' in s) || schema.anyOf || schema.allOf || hasType(schema, 'object')) {
            return true;
        }
        // arrays of objects are blocks, arrays of values a list of chips
        const items: any = schema.items;
        const itemSchema = items?.$ref ? Resolve.schema(rootSchema, items.$ref, rootSchema) : items;
        return hasType(schema, 'array') && !!itemSchema && (hasType(itemSchema, 'object') || !!itemSchema.properties);
    }
}
