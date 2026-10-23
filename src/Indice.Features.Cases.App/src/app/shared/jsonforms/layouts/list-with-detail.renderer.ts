import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { JsonFormsArrayControl, JsonFormsOutlet } from '@jsonforms/angular';
import {
    Actions, composePaths, createDefaultValue, findUISchema, getData, getFirstPrimitiveProp, JsonFormsState, JsonSchema, OwnPropsOfRenderer,
    StatePropsOfArrayControl, toDataPath, UISchemaElement
} from '@jsonforms/core';
import { TranslatePipe } from '@ngx-translate/core';
import { LucidePlus, LucideTrash } from '@lucide/angular';
import { get } from 'lodash';
import { CaseDetailsService } from 'src/app/core/services/case-details.service';
import { addClasses, presentField } from '../jsonforms-utilities';

/**
 * JSON Forms `ListWithDetail` (master-detail) over an array of objects: the items on the left, the selected item's form on the right
 * (stacked on narrow screens). Items are named by `options.labelRef` (a scope inside the item) or by their first primitive property.
 */
@Component({
    selector: 'app-jf-list-with-detail',
    imports: [JsonFormsOutlet, TranslatePipe, LucidePlus, LucideTrash],
    template: `
    @if (shown) {
      <div [class]="wrapperClass">
        @if (title) {
          <p class="mb-2 text-sm font-medium" [class.sr-only]="notitle" [innerHTML]="title"></p>
        }
        <div class="grid gap-4 md:grid-cols-[minmax(10rem,16rem)_1fr]">
          <div class="min-w-0">
            <ul class="menu w-full rounded-box border border-base-300 bg-base-100 p-1">
              @for (label of labels; track $index) {
                <li>
                  <a class="flex items-center gap-2" [class.menu-active]="$index === selected" (click)="select($index)">
                    <span class="min-w-0 flex-1 truncate">{{ label }}</span>
                    @if (!readonly && labels.length > minItems) {
                      <button type="button" class="btn btn-ghost btn-xs btn-square" (click)="removeItem($index); $event.stopPropagation()"
                        [attr.aria-label]="'jsonforms.removeItem' | translate" [title]="'jsonforms.removeItem' | translate">
                        <svg lucideTrash class="size-3.5"></svg>
                      </button>
                    }
                  </a>
                </li>
              } @empty {
                <li class="px-3 py-2 text-sm text-base-content/50">{{ 'jsonforms.noItems' | translate }}</li>
              }
            </ul>
            @if (!readonly && (maxItems === undefined || labels.length < maxItems)) {
              <button type="button" class="btn btn-sm btn-outline mt-3 w-full" (click)="addItem()">
                <svg lucidePlus class="size-4"></svg>
                {{ 'jsonforms.addItem' | translate }}
              </button>
            }
          </div>
          <div class="min-w-0 rounded-box border border-base-300 px-4 py-2">
            @if (detail) {
              <jsonforms-outlet [renderProps]="detail"></jsonforms-outlet>
            } @else {
              <p class="py-6 text-center text-sm text-base-content/50">{{ 'jsonforms.selectItem' | translate }}</p>
            }
          </div>
        </div>
      </div>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager
})
export class ListWithDetailRendererComponent extends JsonFormsArrayControl {
    private caseDetailsService = inject(CaseDetailsService);
    private rootData: any;
    private itemSchema: JsonSchema = {};
    private detailUiSchema: UISchemaElement | undefined;
    options: any = {};
    title: string | null = null;
    notitle = false;
    readonly = false;
    shown = true;
    labels: string[] = [];
    selected = -1;
    detail: OwnPropsOfRenderer | undefined;
    minItems = 0;
    maxItems: number | undefined;

    get wrapperClass(): string {
        return addClasses(this.options.htmlClass, 'schema-form-list-with-detail', 'py-1.5');
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
        this.itemSchema = props.schema;
        this.detailUiSchema ??= findUISchema(props.uischemas ?? [], props.schema, this.uischema.scope, props.path, undefined, this.uischema, props.rootSchema);
        const items: any[] = get(this.rootData, props.path) ?? [];
        const labelPath = this.options.labelRef ? toDataPath(this.options.labelRef) : getFirstPrimitiveProp(props.schema);
        this.labels = items.map((item, i) => {
            const label = labelPath && item && typeof item === 'object' ? get(item, labelPath) : undefined;
            return label !== undefined && label !== null && label !== '' ? `${label}` : `#${i + 1}`;
        });
        if (this.selected >= items.length) {
            this.select(items.length - 1);
        } else if (this.selected === -1 && items.length) {
            this.select(0);
        }
    }

    select(index: number): void {
        if (index === this.selected && this.detail) {
            return;
        }
        this.selected = index;
        this.detail = index >= 0 ? { uischema: this.detailUiSchema!, schema: this.itemSchema, path: composePaths(this.propsPath, `${index}`) } : undefined;
    }

    addItem(): void {
        this.jsonFormsService.updateCore(Actions.update(this.propsPath, (array: any[]) => [...(array ?? []), createDefaultValue(this.itemSchema, this.rootSchema)]));
        this.select(this.labels.length - 1);
    }

    removeItem(index: number): void {
        // stay on the same item, which moves up when an item above it goes
        const next = index < this.selected ? this.selected - 1 : this.selected;
        this.jsonFormsService.updateCore(Actions.update(this.propsPath, (array: any[]) => array.filter((_, i) => i !== index)));
        this.detail = undefined;
        this.select(Math.min(next, this.labels.length - 1));
    }
}
