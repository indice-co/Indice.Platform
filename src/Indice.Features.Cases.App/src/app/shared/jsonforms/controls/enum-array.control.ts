import { ChangeDetectionStrategy, Component } from '@angular/core';
import { and, hasType, JsonSchema, resolveSchema, schemaMatches, schemaSubPathMatches, Tester, uiTypeIs } from '@jsonforms/core';
import { buildTitleMap } from '../jsonforms-utilities';
import { FieldWrapperComponent } from './field-wrapper.component';
import { textOf } from './select.control';
import { TailwindControl } from './tailwind-control';

/** An array whose items are an `enum` or a `oneOf` of constants: a multi-select. */
export const isEnumArrayControl: Tester = and(
    uiTypeIs('Control'),
    schemaMatches(schema => hasType(schema, 'array') && !!schema.items && !Array.isArray(schema.items)),
    schemaSubPathMatches('items', (items, rootSchema) => {
        const resolved = items.$ref ? resolveSchema(rootSchema, items.$ref, rootSchema) : items;
        return Array.isArray(resolved?.enum) || (Array.isArray(resolved?.oneOf) && resolved.oneOf.every(o => o && 'const' in o));
    })
);

/** The selectable values of an enum array: the items' `enum` / `oneOf`, or the control's `titleMap`. */
export function enumArrayOptions(arraySchema: JsonSchema | undefined, rootSchema: JsonSchema | undefined, options: any): { name: string, value: any }[] {
    let items: any = arraySchema?.items ?? {};
    if (items.$ref && rootSchema) {
        items = resolveSchema(rootSchema, items.$ref, rootSchema) ?? {};
    }
    return buildTitleMap(options?.titleMap ? { ...items, titleMap: options.titleMap } : items);
}

/** Arrays of enum values: a checkbox per value. */
@Component({
    selector: 'app-jf-enum-array',
    imports: [FieldWrapperComponent],
    template: `
    @if (shown) {
      <app-jf-field [control]="this">
        @if (showEditor) {
          <div class="flex flex-wrap gap-x-5 gap-y-1.5 py-1" role="group" [attr.aria-labelledby]="id">
            @for (item of selectList; track $index) {
              <label class="label cursor-pointer gap-2 text-sm text-base-content">
                <input type="checkbox" class="checkbox checkbox-sm checkbox-primary" [checked]="isSelected(item.value)" (change)="toggle(item.value)">
                <span [innerHTML]="item.name"></span>
              </label>
            }
          </div>
        }
      </app-jf-field>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager
})
export class EnumArrayControlComponent extends TailwindControl {
    protected widget = 'checkboxes';
    selectList: { name: string, value: any }[] = [];

    override get readonlyText(): string {
        return this.selectedValues().map(value => textOf(this.selectList.find(item => item.value === value)?.name ?? `${value}`)).join(', ');
    }

    protected override onStateChange(): void {
        this.selectList = enumArrayOptions(this.scopedSchema, this.rootSchema, this.options);
    }

    isSelected(value: any): boolean {
        return this.selectedValues().includes(value);
    }

    toggle(value: any): void {
        const current = this.selectedValues();
        const next = current.includes(value) ? current.filter(v => v !== value) : [...current, value];
        // keep the order of the options, not the order of the clicks
        const ordered = this.selectList.map(item => item.value).filter(v => next.includes(v));
        this.setValue(ordered.length ? ordered : undefined);
    }

    private selectedValues(): any[] {
        return Array.isArray(this.data) ? this.data : [];
    }
}
