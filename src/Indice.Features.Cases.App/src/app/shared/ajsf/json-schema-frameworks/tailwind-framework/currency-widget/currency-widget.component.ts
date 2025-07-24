import { TranslateService } from '@ngx-translate/core';
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
  /*--------*/

  private locale = 'el-GR'; // Default to Greek locale

  // This is the placeholder for the mask input. The actual control value is a hidden input
  public displayValue = '';
  private lastValue = '';
  // Specify type parameter for better type safety
  private destroy$ = new Subject<void>();
  constructor(
    private jsf: JsonSchemaFormService,
    private translateService: TranslateService
  ) { }

  ngOnInit() {
    this.optionsFromLayout();
    this.jsf.initializeControl(this);
    this.locale = this.translateService.currentLang || this.locale; // Use current language or default to Greek
    const controlValue = this.formControl.value;
    // Initialize displayValue if necessary
    if (controlValue == null && !this.disableDefaultValue) {
      this.formControl.setValue(this.defaultValue);
    }

    if (controlValue != null) {
      this.displayValue = controlValue.toLocaleString(this.locale, {
        minimumFractionDigits: this.decimalPlaces,
        maximumFractionDigits: this.decimalPlaces
      });
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
    // allow single '-' character
    // Accept but don't commit
    if (this.allowNegativeNumbers && inputValue === '-') {
      return;
    }

    // business validation
    // Early exit – empty or non‑numeric input (after removing separators)
    const inputNumber = CurrencyWidgetComponent.fromLocaleString(this.locale, inputValue, this.decimalPlaces, this.allowNegativeNumbers);
    // If the value is undefined we reset
    if (inputNumber === undefined) {
      this.lastValue = '';
      return;
    }
    this.lastValue = inputNumber.toLocaleString(this.locale, {
      minimumFractionDigits: this.decimalPlaces,
      maximumFractionDigits: this.decimalPlaces
    });
    this.jsf.updateValue(this, inputNumber);
  }

  onBlur($event: FocusEvent) {
    const inputEl = $event.target as HTMLInputElement;
    inputEl.value = this.lastValue;
  }

  private static fromLocaleString(locale: string, inputText: string, decimalPlaces: number, allowNegativeNumbers: boolean = true, round: boolean = false): number | undefined {
    if (!inputText?.trim()) {
      return;
    }
    // This is locale dependent, e.g. in Greek it is a comma (,) while in US it is a dot (.)
    const decimalSeparator = (1.1).toLocaleString(locale).replace(/\d/g, '');
    const isNegative = allowNegativeNumbers && inputText[0] === '-';
    const sanitizedInput = inputText.replace(new RegExp(`[^\\${decimalSeparator}|\\d]`, 'g'), '').replace(decimalSeparator, '.');
    let result = parseFloat(sanitizedInput);

    if (!round) {
      result = Math.floor(result * Math.pow(10, decimalPlaces)) / Math.pow(10, decimalPlaces); // Truncate to the specified decimal places
    }
    if (isNegative) {
      result = -result; // Apply negative sign if applicable
    }
    return isNaN(result) ? undefined : result;
  }

  /**
   * Extracts options from the layout node and sets the component properties.
   */
  private optionsFromLayout(): void {
    this.options = this.layoutNode.options || {};

    // Boolean options
    this.allowNegativeNumbers = this.resolveOption<boolean>('allowNegativeNumbers');
    this.disableDefaultValue = this.resolveOption<boolean>('disableDefaultValue');

    // Numeric options
    this.decimalPlaces = this.resolveOption<number>('decimalPlaces');
    this.defaultValue = this.resolveOption<number>('defaultValue');
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