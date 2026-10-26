import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { JsonFormsArrayControl, JsonFormsOutlet } from '@jsonforms/angular';
import {
    Actions, and, composePaths, createDefaultValue, encode, getData, hasType, isObjectArrayControl, isPrimitiveArrayControl, JsonFormsState, JsonSchema,
    optionIs, or, OwnPropsOfRenderer, resolveSchema, StatePropsOfArrayControl, Tester
} from '@jsonforms/core';
import { TranslatePipe } from '@ngx-translate/core';
import { LucidePlus, LucideX } from '@lucide/angular';
import { CaseDetailsService } from 'src/app/core/services/case-details.service';
import { addClasses, fixTitle, presentField } from '../jsonforms-utilities';

/** Arrays with `options.table: true`. */
export const isTableArrayControl: Tester = and(or(isObjectArrayControl, isPrimitiveArrayControl), optionIs('table', true));

/**
 * Arrays drawn as a table (`options.table: true`): a column per primitive property of the items (or a single column for primitive items),
 * each cell the field's own control without its label; `options.columns` picks and orders the properties.
 */
@Component({
    selector: 'app-jf-table',
    imports: [JsonFormsOutlet, TranslatePipe, LucidePlus, LucideX],
    template: `
    @if (shown) {
      <div [class]="wrapperClass">
        @if (title) {
          <p class="mb-2 text-sm font-medium" [class.sr-only]="notitle" [innerHTML]="title"></p>
        }
        <div class="overflow-x-auto rounded-box border border-base-300">
          <table class="table table-sm">
            <thead>
              <tr>
                @for (column of columns; track column.key) {
                  <th>{{ column.title }}</th>
                }
                @if (!readonly) {
                  <th class="w-0"><span class="sr-only">{{ 'jsonforms.removeItem' | translate }}</span></th>
                }
              </tr>
            </thead>
            <tbody>
              @for (row of rows; track $index) {
                <tr>
                  @for (cell of row; track $index) {
                    <td class="min-w-32 align-top [&_.fieldset]:py-0"><jsonforms-outlet [renderProps]="cell"></jsonforms-outlet></td>
                  }
                  @if (!readonly) {
                    <td class="align-top">
                      @if (rows.length > minItems) {
                        <button type="button" class="btn btn-ghost btn-xs btn-square mt-1.5" (click)="removeItem($index)"
                          [attr.aria-label]="'jsonforms.removeItem' | translate" [title]="'jsonforms.removeItem' | translate">
                          <svg lucideX class="size-4"></svg>
                        </button>
                      }
                    </td>
                  }
                </tr>
              } @empty {
                <tr>
                  <td [attr.colspan]="columns.length + (readonly ? 0 : 1)" class="text-center text-base-content/50">{{ readonly ? '—' : ('jsonforms.noItems' | translate) }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
        @if (!readonly && (maxItems === undefined || rows.length < maxItems)) {
          <button type="button" class="btn btn-sm btn-outline mt-3" (click)="addItem()">
            <svg lucidePlus class="size-4"></svg>
            {{ 'jsonforms.addItem' | translate }}
          </button>
        }
      </div>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager
})
export class TableRendererComponent extends JsonFormsArrayControl {
    private caseDetailsService = inject(CaseDetailsService);
    private rootData: any;
    private itemSchema: JsonSchema = {};
    options: any = {};
    title: string | null = null;
    notitle = false;
    readonly = false;
    shown = true;
    columns: { key: string, title: string }[] = [];
    rows: OwnPropsOfRenderer[][] = [];
    minItems = 0;
    maxItems: number | undefined;

    get wrapperClass(): string {
        return addClasses(this.options.htmlClass, 'schema-form-table', 'py-1.5');
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
        if (this.itemSchema !== props.schema) {
            this.itemSchema = props.schema;
            this.columns = this.buildColumns(props.schema, props.rootSchema);
            this.rows = [];
        }
        const count = Array.isArray(props.data) ? props.data.length : props.data ?? 0;
        // rebuilt only when the item count changes: new render props would re-create the cells (and lose the focus)
        if (count !== this.rows.length) {
            this.rows = Array.from({ length: count }, (_, i) => this.columns.map(column => ({
                uischema: { type: 'Control', scope: column.key ? `#/properties/${encode(column.key)}` : '#', label: false } as any,
                schema: props.schema,
                path: composePaths(props.path, `${i}`)
            })));
        }
    }

    addItem(): void {
        this.jsonFormsService.updateCore(Actions.update(this.propsPath, (array: any[]) => [...(array ?? []), createDefaultValue(this.itemSchema, this.rootSchema)]));
    }

    removeItem(index: number): void {
        this.jsonFormsService.updateCore(Actions.update(this.propsPath, (array: any[]) => array.filter((_, i) => i !== index)));
    }

    /** The primitive properties of object items (`key` = property name), or one unnamed column for primitive items. */
    private buildColumns(itemSchema: JsonSchema, rootSchema: JsonSchema): { key: string, title: string }[] {
        if (!hasType(itemSchema, 'object')) {
            return [{ key: '', title: itemSchema.title ?? '' }];
        }
        const properties = itemSchema.properties ?? {};
        const keys: string[] = Array.isArray(this.options.columns) ? this.options.columns : Object.keys(properties);
        return keys
            .filter(key => {
                let property: any = properties[key];
                if (property?.$ref) {
                    property = resolveSchema(rootSchema, property.$ref, rootSchema);
                }
                return property && !hasType(property, 'object') && !hasType(property, 'array');
            })
            .map(key => ({ key, title: properties[key].title ?? fixTitle(key) }));
    }
}
