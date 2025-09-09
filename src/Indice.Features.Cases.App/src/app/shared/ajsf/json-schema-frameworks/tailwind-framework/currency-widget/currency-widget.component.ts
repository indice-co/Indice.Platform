import { JsonSchemaFormService } from '@ajsf-extended/core';
import { Component, Input, OnDestroy, OnInit } from '@angular/core';
import { Subject } from 'rxjs';
import { takeUntil, map, tap } from 'rxjs/operators';

@Component({
  selector: 'app-currency-widget',
  templateUrl: './currency-widget.component.html',
  styleUrls: ['./currency-widget.component.scss']
})
export class CurrencyWidgetComponent implements OnInit {
  formControl: any;
  controlName: string | undefined;
  controlValue: string | undefined;
  controlDisabled = false;
  boundControl = false;
  options: any;
  autoCompleteList: string[] = [];
  @Input() layoutNode: any;
  @Input() layoutIndex: number[] = [];
  @Input() dataIndex: number[] = [];

  /*--- custom properties ---*/
  allowNegativeNumbers = true;
  decimalPlaces = 2;
  enableDefaultValue = false;
  defaultValue = 0;
  locale = 'el-GR';
  /*------------------------*/
  displayValue = '';

  constructor(
    private jsf: JsonSchemaFormService
  ) { }

  ngOnInit() {
    this.options = this.layoutNode.options || {};
    this.jsf.initializeControl(this);

    const value = this.formControl.value;
    if (value == null && this.enableDefaultValue) {
      this.formControl.setValue(this.defaultValue);
    }
    if (value != null) {
      this.displayValue = CurrencyWidgetComponent.toLocaleString(this.locale, this.formControl.value, this.decimalPlaces);
    }
  }

  updateValue(event: any) {
    // allow single '-' character
    // Accept but don't commit
    if (this.allowNegativeNumbers && event.target.value === '-') {
      return;
    }
    // business validation
    // Early exit – empty or non‑numeric input (after removing separators)
    const inputNumber = CurrencyWidgetComponent.fromLocaleString(this.locale, event.target.value, this.decimalPlaces, this.allowNegativeNumbers);
    // If the value is undefined we do not update
    if (inputNumber === undefined && event.target.value !== '') {
      return;
    }
    this.jsf.updateValue(this, inputNumber);
  }

  onBlur($event: FocusEvent) {
    const inputEl = $event.target as HTMLInputElement;
    inputEl.value = CurrencyWidgetComponent.toLocaleString(this.locale, this.formControl.value, this.decimalPlaces);
  }

  private static toLocaleString(locale: string, value: number, decimalPlaces: number) {
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
      result = -result; // Apply negative sign if applicable
    }
    return isNaN(result) ? undefined : result;
  }

}
