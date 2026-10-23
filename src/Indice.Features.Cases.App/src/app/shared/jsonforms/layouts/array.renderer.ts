import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { JsonFormsArrayControl, JsonFormsOutlet } from '@jsonforms/angular';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { LucidePlus, LucideX } from '@lucide/angular';
import { Actions, composePaths, createDefaultValue, findUISchema, getData, JsonFormsState, JsonSchema, OwnPropsOfRenderer, StatePropsOfArrayControl } from '@jsonforms/core';
import { CaseDetailsService } from 'src/app/core/services/case-details.service';
import { addClasses, presentField } from '../jsonforms-utilities';

/** Arrays (ajsf `array`): each item drawn with `options.detail` (the ajsf item layout), plus add / remove buttons when editable. */
@Component({
    selector: 'app-jf-array',
    imports: [JsonFormsOutlet, TranslatePipe, LucidePlus, LucideX],
    template: `
    @if (shown) {
      <div [class]="wrapperClass">
        @if (title) {
          <p [class]="labelClass" [class.sr-only]="notitle" [innerHTML]="title"></p>
        }
        <div class="space-y-3">
          @for (item of items; track $index) {
            <div class="relative rounded-box border border-base-300 bg-base-100 px-4 py-2">
              @if (!readonly && options.removable !== false && items.length > minItems) {
                <button type="button" class="btn btn-ghost btn-xs btn-square absolute top-2 right-2 z-10"
                  [attr.aria-label]="'jsonforms.removeItem' | translate" [title]="'jsonforms.removeItem' | translate"
                  (click)="removeItem($index)">
                  <svg lucideX class="size-4"></svg>
                </button>
              }
              <jsonforms-outlet [renderProps]="item"></jsonforms-outlet>
            </div>
          } @empty {
            @if (readonly) {
              <p class="text-sm text-base-content/50">—</p>
            }
          }
        </div>
        @if (!readonly && options.addable !== false && (maxItems === undefined || items.length < maxItems)) {
          <button type="button" class="btn btn-sm btn-outline mt-3" (click)="addItem()">
            <svg lucidePlus class="size-4"></svg>
            {{ addButtonText || ('jsonforms.addItem' | translate) }}
          </button>
        }
      </div>
    }`,
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
        return addClasses(this.options.htmlClass, 'schema-form-array', 'py-1.5');
    }

    get labelClass(): string {
        return addClasses(this.options.labelHtmlClass, 'mb-2 text-sm font-medium');
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
        this.addButtonText = /^add\b/i.test(plainTitle) ? plainTitle : plainTitle ? this.translate.instant('jsonforms.addNamedItem', { title: plainTitle }) : '';
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
