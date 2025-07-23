import { JsonSchemaFormService } from '@ajsf-extended/core';
import { Component, Input, OnDestroy, OnInit } from '@angular/core';
import { FormControl } from '@angular/forms';
import { Subject } from 'rxjs';

@Component({
  selector: 'app-currency-widget',
  templateUrl: './currency-widget.component.html',
  styleUrls: ['./currency-widget.component.scss']
})
export class CurrencyWidgetComponent implements OnInit, OnDestroy {
  formControl!: FormControl;
  controlName: string | undefined;
  controlValue: string | undefined;
  controlDisabled = false;
  boundControl = false;
  options: any;
  autoCompleteList: string[] = [];
  @Input() layoutNode: any;
  @Input() layoutIndex: number[] = [];
  @Input() dataIndex: number[] = [];

  /*---- Options/Flags that come from the layout ----*/
  /**
    * The separator used for thousands in the formatted value.
    * Defaults to ".", using a dot as the thousands separator.
  */
  thousandSeparator = ".";
  /**
    * Whether to allow negative numbers in the input.
    * Defaults to true, allowing negative values.
   */
  allowNegativeNumbers = true;
  /**
    * The number of decimal places to display.
    * Defaults to 2, showing two decimal places in the formatted value.
  */
  decimalPlaces = 2;
  /**
    * Whether to disable the default value.
    * Defaults to false, meaning the input will show a default value of 0 if not set by the user.
  */
  disableDefaultValue = false;
  /**
    * The default value to show in the input if no value is set.
    * Defaults to 0, meaning the input will show '0' if no other value is provided.
  */
  defaultValue = 0;
  /**
    * Whether the input is required.
    * Defaults to false, meaning the input is not mandatory.
  */
  required = false;
  /*--------*/

  private get allowedCharRegex(): RegExp {
    // ^[\d,]$   → a single digit or comma
    // ^[-\d,]$  → a minus OR a digit OR a comma
    return this.allowNegativeNumbers
      ? /^[-\d,]$/   // hyphen first or escaped so it’s not a range
      : /^[\d,]$/;
  }

  // This is the placeholder for the mask input. The actual control value is a hidden input
  displayValue = '';
  private lastValue = '';
  // Specify type parameter for better type safety
  private destroy$ = new Subject<void>();
  constructor(
    private jsf: JsonSchemaFormService
  ) { }

  ngOnInit() {
    this.optionsFromLayout();
    this.jsf.initializeControl(this);

    const controlValue = this.formControl.value;
    // Initialize displayValue if necessary
    if (controlValue == null && !this.disableDefaultValue) {
      this.formControl.setValue(this.defaultValue);
    }

    if (controlValue != null) {
      this.displayValue = this.formatForDisplay(controlValue);
    }
    this.lastValue = this.displayValue;
  }

  ngOnDestroy() {
    // Emit undefined to ensure type safety
    this.destroy$.next(undefined);
    this.destroy$.complete();
  }

  updateValue(event: any) {
    const inputEl = event.target as HTMLInputElement;
    const inputValue = inputEl.value;
    if(this.allowNegativeNumbers && inputValue === '-') {
      this.lastValue = inputValue; // keep as last valid state
      this.jsf.updateValue(this, (this.required ? 0 : undefined)); // clear the value
      return; // allow single '-' character
    }
    // allowed characters validation
    if (event.data !== null && !this.allowedCharRegex.test(event.data)) {
      inputEl.value = this.lastValue; // revert to last valid value
      return;
    }

    // business validation
    // Early exit – empty or non‑numeric input (after removing separators)
    const normalisedForNaNCheck = this.getNormalizedNumberOrDefault(inputValue);
    // If the normalized value is undefined, revert to last valid value
    if (normalisedForNaNCheck === undefined) {
      inputEl.value = this.lastValue;
      return;
    }
    // Check decimal precision
    const precision = normalisedForNaNCheck.split('.')?.[1]?.length ?? 0;
    if (precision > this.decimalPlaces) {
      // Revert visual field to previous value and bail out
      inputEl.value = this.lastValue;
      return;
    }
    // All good -> parse & propagate
    const floatValue = parseFloat(normalisedForNaNCheck);

    if (!this.allowNegativeNumbers && floatValue < 0) {
      inputEl.value = this.lastValue;
      return;
    }

    this.lastValue = inputValue; // keep as last valid state
    this.jsf.updateValue(this, floatValue);
  }
  private getNormalizedNumberOrDefault(value: string): string | undefined {
    if (value == null) {
      return;
    }
    const normalized = value.replace(/[.]/g, '').replace(/[,]/g, '.');
    return isNaN(Number(normalized)) ? undefined : normalized;
  }

  private optionsFromLayout(): void {
    this.options = this.layoutNode.options || {};
    const model = this.jsf.getData();

    // Boolean options
    this.allowNegativeNumbers = this.resolveOption<boolean>('allowNegativeNumbers');
    this.disableDefaultValue  = this.resolveOption<boolean>('disableDefaultValue');
    this.required = this.resolveOption<boolean>('required');

    // Numeric options
    this.decimalPlaces = this.resolveOption<number>('decimalPlaces');
    this.defaultValue = this.resolveOption<number>('defaultValue');

    // String options
    this.thousandSeparator = this.resolveOption<string>('thousandSeparator');
  }

  /** Formats a numeric value for display, respecting locale & separators */
  private formatForDisplay(value: number): string {
    return value
      .toLocaleString('el', {
        minimumFractionDigits: this.decimalPlaces,
        maximumFractionDigits: this.decimalPlaces,
      })
      .replace(/\./g, this.thousandSeparator);
  }

  /**
   * Resolves the final value for a single option.
   *
   * The option can be:
   *  - A literal value (`true`, `10`, `'|'`, …)
   *  - A function receiving the current model (`model => model.taxRate`)
   *  - A JS expression in a string (`"model.amount * 1.24"`)
   *
   * If the option is missing we fall back to the default declared as a
   * class‑level property.
   */
  private resolveOption<T>(name: keyof CurrencyWidgetComponent): T {
    const raw = this.options[name as string] as OptionValue<T>;
    if (raw == null) {                       // handles undefined & null
      return (this as any)[name];
    }

    const model = this.jsf.getData();

    // 1. Raw is a real function object
    if (typeof raw === 'function') {
      return (raw as (m: any) => T)(model);
    }

    // 2. Raw is a string – evaluate once
    if (typeof raw === 'string') {
      try {
        const result = (new Function('model', `return (${raw});`))(model);

        // If the result is still a function, call it
        return typeof result === 'function'
          ? (result as (m: any) => T)(model)
          : result as T;
      } catch {
        // Ignore errors in evaluation, fallback to default
      }
    }

    // 3. Fallback literal
    return raw as T;
  }
}
/**
 * Helper type describing what we accept as an option value:
 * - a literal value (number | string | boolean)
 * - a function receiving the model
 * - a string containing an evaluable JS expression
 */
type OptionValue<T> = T | ((model: any) => T) | string | null | undefined;