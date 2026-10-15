import { ChangeDetectionStrategy, Component } from '@angular/core';
import { buildTitleMap } from '../jsonforms-utilities';
import { TailwindControl } from './tailwind-control';

/** ajsf `select` widget, also used for every field with `enum`, `titleMap` or a `oneOf` of constants. */
@Component({
    selector: 'app-jf-select',
    template: `
    @if (shown) {
      <app-jf-field [control]="this">
        <select
          [class]="fieldClass"
          [attr.readonly]="readonly ? 'readonly' : null"
          [disabled]="readonly"
          [attr.required]="options.required"
          [id]="id"
          (change)="onSelect($event)">
          <option value="" [selected]="data === undefined || data === null"></option>
          @for (item of selectList; track $index) {
            <option [value]="$index" [selected]="item.value === data" [innerHTML]="item.name"></option>
          }
        </select>
      </app-jf-field>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class SelectControlComponent extends TailwindControl {
    protected widget = 'select';
    selectList: { name: string, value: any }[] = [];

    protected override onStateChange(): void {
        this.selectList = buildTitleMap(this.options);
    }

    onSelect(event: Event): void {
        const index = (event.target as HTMLSelectElement).value;
        this.setValue(index === '' ? undefined : this.selectList[+index].value);
    }
}
