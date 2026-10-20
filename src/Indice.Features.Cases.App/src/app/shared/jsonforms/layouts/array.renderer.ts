import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { JsonFormsArrayControl, JsonFormsOutlet } from '@jsonforms/angular';
import { Actions, composePaths, createDefaultValue, findUISchema, getData, JsonFormsState, JsonSchema, OwnPropsOfRenderer, StatePropsOfArrayControl } from '@jsonforms/core';
import { NgIcon } from '@ng-icons/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { CaseDetailsService } from 'src/app/core/services/case-details.service';
import { addClasses, presentField } from '../jsonforms-utilities';

/** Arrays (ajsf `array`): each item drawn with `options.detail` (the ajsf item layout), plus add / remove buttons when editable. */
@Component({
    selector: 'app-jf-array',
    imports: [JsonFormsOutlet, NgIcon, TranslatePipe],
    template: `
    @if (shown) {
      <div [class]="wrapperClass">
        @if (title) {
          <div [class]="labelClass" [class.sr-only]="notitle" [innerHTML]="title"></div>
        }
        <div class="jf-array-items">
          @for (item of items; track $index) {
            <div class="jf-array-item list-group-item">
              @if (!readonly && options.removable !== false && items.length > minItems) {
                <button class="jf-array-remove btn btn-ghost btn-sm btn-square" type="button" (click)="removeItem($index)"
                        [attr.aria-label]="'caseForm.removeItem' | translate" [attr.title]="'caseForm.removeItem' | translate">
                  <ng-icon name="lucideTrash2" size="1rem" />
                </button>
              }
              <jsonforms-outlet [renderProps]="item"></jsonforms-outlet>
            </div>
          }
        </div>
        @if (!readonly && options.addable !== false && (maxItems === undefined || items.length < maxItems)) {
          <button type="button" class="jf-array-add btn btn-ghost btn-sm" (click)="addItem()">
            <ng-icon name="lucidePlus" size="1rem" />
            {{ addButtonText }}
          </button>
        }
      </div>
    }`,
    host: { '[class.jf-hidden]': '!shown' },
    changeDetection: ChangeDetectionStrategy.Eager
})
export class ArrayRendererComponent extends JsonFormsArrayControl {
    private caseDetailsService = inject(CaseDetailsService);
    private translate = inject(TranslateService);
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
        return addClasses('jf-array', 'form-group', 'schema-form-array', 'list-group', this.options.htmlClass);
    }

    get labelClass(): string {
        return addClasses('jf-label', this.options.labelHtmlClass);
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
        this.addButtonText = /^add\b/i.test(plainTitle) ? plainTitle : this.translate.instant('caseForm.addItem', { title: plainTitle }).trim();
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
