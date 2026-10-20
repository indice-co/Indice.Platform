import { ChangeDetectionStrategy, Component } from '@angular/core';
import { CaseFormControl } from './case-form-control';
import { FieldWrapperComponent } from './field-wrapper.component';

/** ajsf `currency` widget: a number typed and shown in the Greek format (`1.234,56`). */
@Component({
    selector: 'app-jf-currency',
    imports: [FieldWrapperComponent],
    template: `
    @if (shown) {
      <app-jf-field [control]="this">
        <input
          type="text"
          inputmode="decimal"
          [class]="fieldClass + ' jf-numeric'"
          [attr.maxlength]="options.maxLength"
          [attr.minlength]="options.minLength"
          [attr.placeholder]="options.placeholder"
          [attr.required]="options.required"
          [attr.aria-invalid]="invalid"
          [attr.aria-describedby]="describedBy"
          [id]="id"
          [readonly]="readonly"
          [value]="displayValue"
          (focus)="editing = true"
          (input)="onInput($event)"
          (blur)="onBlur($event)">
      </app-jf-field>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager
})
export class CurrencyControlComponent extends CaseFormControl {
    protected widget = 'currency';
    private readonly allowNegativeNumbers = true;
    private readonly decimalPlaces = 2;
    private readonly locale = 'el-GR';
    displayValue = '';
    editing = false;

    protected override onStateChange(): void {
        if (!this.editing) {
            this.displayValue = CurrencyControlComponent.toLocaleString(this.locale, this.data, this.decimalPlaces);
        }
    }

    onInput(event: Event): void {
        const text = (event.target as HTMLInputElement).value;
        // Allow a single '-' character: accept but don't commit.
        if (this.allowNegativeNumbers && text === '-') {
            return;
        }
        const inputNumber = CurrencyControlComponent.fromLocaleString(this.locale, text, this.decimalPlaces, this.allowNegativeNumbers);
        // Not a number: don't update.
        if (inputNumber === undefined && text !== '') {
            return;
        }
        // store the number as a string when the schema types the property as string (and not number), so it passes validation
        const types: string[] = ([] as string[]).concat(this.scopedSchema?.type ?? []);
        const isString = types.includes('string') && !types.includes('number') && !types.includes('integer');
        this.setValue(isString && inputNumber !== undefined ? inputNumber.toString() : inputNumber);
    }

    onBlur(event: FocusEvent): void {
        this.editing = false;
        this.displayValue = CurrencyControlComponent.toLocaleString(this.locale, this.data, this.decimalPlaces);
        (event.target as HTMLInputElement).value = this.displayValue;
    }

    private static toLocaleString(locale: string, value: number | string, decimalPlaces: number): string {
        if (typeof value === 'string') {
            value = value.trim() === '' || isNaN(Number(value)) ? value as any : Number(value);
        }
        return value?.toLocaleString(locale, {
            minimumFractionDigits: decimalPlaces,
            maximumFractionDigits: decimalPlaces
        }) || '';
    }

    private static fromLocaleString(locale: string, inputText: string, decimalPlaces: number, allowNegativeNumbers: boolean = true): number | undefined {
        if (!inputText?.trim()) {
            return;
        }
        // This is locale dependent, e.g. in Greek it is a comma (,) while in US it is a dot (.)
        const decimalSeparator = (1.1).toLocaleString(locale).replace(/\d/g, '');
        const isNegative = allowNegativeNumbers && inputText[0] === '-';
        const sanitizedInput = inputText.replace(new RegExp(`[^\\${decimalSeparator}|\\d]`, 'g'), '').replace(decimalSeparator, '.');
        let result = parseFloat(sanitizedInput);
        if (isNaN(result)) {
            return;
        }
        // Only process fractional part if decimal places > 0 and there is one
        if (decimalPlaces > 0 && sanitizedInput.includes('.')) {
            const [intStr, fracStr = ''] = sanitizedInput.split('.');
            const truncatedFrac = fracStr.slice(0, decimalPlaces);
            result = truncatedFrac.length > 0 ? Number(`${intStr}.${truncatedFrac}`) : Number(intStr);
        } else {
            // ensure it's an integer if no decimals
            result = Math.trunc(result);
        }
        if (isNegative) {
            result = -result;
        }
        return isNaN(result) ? undefined : result;
    }
}
