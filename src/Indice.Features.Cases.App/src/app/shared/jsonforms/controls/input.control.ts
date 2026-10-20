import { ChangeDetectionStrategy, Component } from '@angular/core';
import { FieldWrapperComponent } from './field-wrapper.component';
import { TailwindControl } from './tailwind-control';

// ajsf input type from the schema format (ajsf `getInputType`).
const formatInputTypes: { [format: string]: string } = { 'color': 'color', 'date-time': 'datetime-local', 'email': 'email', 'uri': 'url' };

/** Text and number inputs (ajsf `text` / `number` widgets, and every string or number field without a widget). */
@Component({
    selector: 'app-jf-input',
    imports: [FieldWrapperComponent],
    template: `
    @if (shown) {
      <app-jf-field [control]="this">
        @if (showEditor) {
          <input
            [class]="fieldClass"
            [attr.list]="options.typeahead?.source ? id + 'Autocomplete' : null"
            [attr.maxlength]="options.maxLength"
            [attr.minlength]="options.minLength"
            [attr.pattern]="options.pattern"
            [attr.placeholder]="options.placeholder"
            [attr.required]="options.required"
            [id]="id"
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
        }
      </app-jf-field>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager
})
export class InputControlComponent extends TailwindControl {
    protected widget = 'text';
    inputType = 'text';

    protected override onStateChange(): void {
        const schemaType = this.scopedSchema?.type;
        const isNumber = schemaType === 'number' || schemaType === 'integer';
        this.inputType = this.options.widget ?? (isNumber ? 'number' : formatInputTypes[this.options.format] ?? 'text');
        this.widget = this.inputType;
        // colour swatches read better as a swatch than as a hex code
        this.readonlyAsText = this.inputType !== 'color';
    }

    onInput(event: Event): void {
        const value = (event.target as HTMLInputElement).value;
        const schemaType = this.scopedSchema?.type;
        this.setValue((schemaType === 'number' || schemaType === 'integer') && value !== '' ? Number(value) : value);
    }
}
