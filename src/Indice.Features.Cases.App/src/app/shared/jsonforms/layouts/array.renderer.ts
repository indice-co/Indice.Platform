import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { JsonFormsArrayControl } from '@jsonforms/angular';
import { Actions, composePaths, createDefaultValue, findUISchema, getData, JsonFormsState, JsonSchema, OwnPropsOfRenderer, StatePropsOfArrayControl } from '@jsonforms/core';
import { CaseDetailsService } from 'src/app/core/services/case-details.service';
import { addClasses, presentField } from '../jsonforms-utilities';

/** Arrays (ajsf `array`): each item drawn with `options.detail` (the ajsf item layout), plus add / remove buttons when editable. */
@Component({
    selector: 'app-jf-array',
    template: `
    @if (shown) {
      <div [class]="wrapperClass">
        @if (title) {
          <label [class]="labelClass" [class.sr-only]="notitle" [innerHTML]="title"></label>
        }
        @for (item of items; track $index) {
          <div class="list-group-item">
            @if (!readonly && options.removable !== false && items.length > minItems) {
              <button class="close pull-right" type="button" (click)="removeItem($index)">
                <span aria-hidden="true">&times;</span>
                <span class="sr-only">Close</span>
              </button>
            }
            <jsonforms-outlet [renderProps]="item"></jsonforms-outlet>
          </div>
        }
        @if (!readonly && options.addable !== false && (maxItems === undefined || items.length < maxItems)) {
          <button type="button" class="btn btn-info float-right m-2 btn-default" (click)="addItem()">{{ addButtonText }}</button>
        }
      </div>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class ArrayRendererComponent extends JsonFormsArrayControl {
    private caseDetailsService = inject(CaseDetailsService);
    private rootData: any;
    private itemSchema: JsonSchema = {};
    options: any = {};
    title: string | null = null;
    notitle = false;
    readonly = false;
    shown = true;
    items: OwnPropsOfRenderer[] = [];
    minItems = 0;
    maxItems: number | undefined;
    addButtonText = '';

    get wrapperClass(): string {
        return addClasses(this.options.htmlClass, 'schema-form-array', 'list-group');
    }

    get labelClass(): string {
        return addClasses(this.options.labelHtmlClass, 'form-label inline-block mb-1 text-gray-700');
    }

    protected override mapToProps(state: JsonFormsState) {
        this.rootData = getData(state);
        return super.mapToProps(state);
    }

    override mapAdditionalProps(props: StatePropsOfArrayControl): void {
        const arraySchema: JsonSchema = (props as any).arraySchema ?? {};
        const field = presentField(this.uischema, arraySchema, props.path, this.rootData, props, this.caseDetailsService.caseDetails?.channel);
        this.options = field.options;
        this.title = field.title;
        this.notitle = field.notitle;
        this.readonly = field.readonly;
        this.shown = field.shown;
        this.minItems = arraySchema.minItems ?? 0;
        this.maxItems = arraySchema.maxItems;
        const plainTitle = typeof this.uischema.label === 'string' ? this.uischema.label : this.options.title ?? '';
        this.addButtonText = /^add\b/i.test(plainTitle) ? plainTitle : `Add ${plainTitle}`.trim();
        this.itemSchema = props.schema;
        const count = Array.isArray(props.data) ? props.data.length : props.data ?? 0;
        if (count !== this.items.length) {
            const detail = findUISchema(props.uischemas ?? [], props.schema, this.uischema.scope, props.path, undefined, this.uischema, props.rootSchema);
            this.items = Array.from({ length: count }, (_, i) => ({ uischema: detail, schema: props.schema, path: composePaths(props.path, `${i}`) }));
        }
    }

    addItem(): void {
        this.jsonFormsService.updateCore(Actions.update(this.propsPath, (array: any[]) => [...(array ?? []), createDefaultValue(this.itemSchema, this.rootSchema)]));
    }

    removeItem(index: number): void {
        this.jsonFormsService.updateCore(Actions.update(this.propsPath, (array: any[]) => array.filter((_, i) => i !== index)));
    }
}
