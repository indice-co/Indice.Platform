import { ChangeDetectionStrategy, Component } from '@angular/core';
import { CaseFormControl } from './case-form-control';
import { FieldWrapperComponent } from './field-wrapper.component';

// ajsf input type from the schema format (ajsf `getInputType`).
const formatInputTypes: { [format: string]: string } = { 'color': 'color', 'date-time': 'datetime-local', 'email': 'email', 'uri': 'url' };

/** Text and number inputs (ajsf `text` / `number` widgets, and every string or number field without a widget). */
@Component({
    selector: 'app-jf-input',
    imports: [FieldWrapperComponent],
    template: `
    @if (shown) {
      <app-jf-field [control]="this">
        <input
          [class]="fieldClass"
          [attr.list]="options.typeahead?.source ? id + 'Autocomplete' : null"
          [attr.maxlength]="options.maxLength"
          [attr.minlength]="options.minLength"
          [attr.pattern]="options.pattern"
          [attr.placeholder]="options.placeholder"
          [attr.required]="options.required"
          [attr.aria-invalid]="invalid"
          [attr.aria-describedby]="describedBy"
          [id]="id"
          [readonly]="readonly"
          [type]="inputType"
          [value]="data ?? ''"
          (input)="onInput($event)">
        @if (options.typeahead?.source) {
          <datalist [id]="id + 'Autocomplete'">
            @for (word of options.typeahead.source; track word) {
              <option [value]="word"></option>
            }
          </datalist>
        }
      </app-jf-field>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager
})
export class InputControlComponent extends CaseFormControl {
    protected widget = 'text';
    inputType = 'text';

    protected override onStateChange(): void {
        const schemaType = this.scopedSchema?.type;
        const isNumber = schemaType === 'number' || schemaType === 'integer';
        this.inputType = this.options.widget ?? (isNumber ? 'number' : formatInputTypes[this.options.format] ?? 'text');
        this.widget = this.inputType;
    }

    onInput(event: Event): void {
        const value = (event.target as HTMLInputElement).value;
        const schemaType = this.scopedSchema?.type;
        this.setValue((schemaType === 'number' || schemaType === 'integer') && value !== '' ? Number(value) : value);
    }
}
