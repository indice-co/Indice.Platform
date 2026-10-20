import { ChangeDetectionStrategy, Component } from '@angular/core';
import { buildTitleMap } from '../jsonforms-utilities';
import { FieldWrapperComponent } from './field-wrapper.component';
import { TailwindControl } from './tailwind-control';

/** ajsf `select` widget, also used for every field with `enum`, `titleMap` or a `oneOf` of constants. */
@Component({
    selector: 'app-jf-select',
    imports: [FieldWrapperComponent],
    template: `
    @if (shown) {
      <app-jf-field [control]="this">
        @if (showEditor) {
          <select
            [class]="fieldClass"
            [attr.required]="options.required"
            [id]="id"
            (change)="onSelect($event)">
            <option value="" [selected]="data === undefined || data === null"></option>
            @for (item of selectList; track $index) {
              <option [value]="$index" [selected]="item.value === data" [innerHTML]="item.name"></option>
            }
          </select>
        }
      </app-jf-field>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager
})
export class SelectControlComponent extends TailwindControl {
    protected widget = 'select';
    protected override fieldBase = 'select';
    selectList: { name: string, value: any }[] = [];

    override get readonlyText(): string {
        return textOf(this.selectList.find(item => item.value === this.data)?.name ?? super.readonlyText);
    }

    protected override onStateChange(): void {
        this.selectList = buildTitleMap(this.options);
    }

    onSelect(event: Event): void {
        const index = (event.target as HTMLSelectElement).value;
        this.setValue(index === '' ? undefined : this.selectList[+index].value);
    }
}

/** Option names may contain markup; read-only mode shows them as plain text. */
export function textOf(html: string | undefined | null): string {
    if (!html) {
        return '';
    }
    const element = document.createElement('div');
    element.innerHTML = html;
    return element.textContent ?? '';
}
