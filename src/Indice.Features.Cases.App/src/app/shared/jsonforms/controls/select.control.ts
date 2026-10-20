import { ChangeDetectionStrategy, Component } from '@angular/core';
import { buildTitleMap } from '../jsonforms-utilities';
import { CaseFormControl, FieldKind } from './case-form-control';
import { FieldWrapperComponent } from './field-wrapper.component';

/** ajsf `select` widget, also used for every field with `enum`, `titleMap` or a `oneOf` of constants. */
@Component({
    selector: 'app-jf-select',
    imports: [FieldWrapperComponent],
    template: `
    @if (shown) {
      <app-jf-field [control]="this">
        <select
          [class]="fieldClass"
          [attr.readonly]="readonly ? 'readonly' : null"
          [disabled]="readonly"
          [attr.required]="options.required"
          [attr.aria-invalid]="invalid"
          [attr.aria-describedby]="describedBy"
          [id]="id"
          (change)="onSelect($event)">
          <option value="" [selected]="data === undefined || data === null"></option>
          @for (item of selectList; track $index) {
            <option [value]="$index" [selected]="item.value === data" [innerHTML]="item.name"></option>
          }
        </select>
      </app-jf-field>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager
})
export class SelectControlComponent extends CaseFormControl {
    protected widget = 'select';
    protected override fieldKind: FieldKind = 'select';
    selectList: { name: string, value: any }[] = [];

    protected override onStateChange(): void {
        this.selectList = buildTitleMap(this.options);
    }

    onSelect(event: Event): void {
        const index = (event.target as HTMLSelectElement).value;
        this.setValue(index === '' ? undefined : this.selectList[+index].value);
    }
}
