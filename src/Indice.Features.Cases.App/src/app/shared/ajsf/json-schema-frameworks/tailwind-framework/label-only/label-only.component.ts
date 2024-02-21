import { JsonSchemaFormService } from '@ajsf-extended/core';
import { Component, Input, OnInit } from '@angular/core';

/**
 * Cases UI widget component for usage in Layout.json.
 * Displays form input data as a simple label.
 * 
 * @example
 * ```
  {
    "title": "Field",
    "labelHtmlClass": "font-bold",
    "type": "label-only",
    "flex": "1 1 50px"
  },
 * ```
 */
@Component({
  selector: 'app-label-only',
  templateUrl: './label-only.component.html',
  styleUrls: ['./label-only.component.scss']
})
export class LabelOnlyComponent implements OnInit {
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

  displayValue: any;

  constructor(
    private jsf: JsonSchemaFormService
  ) { }

  ngOnInit() {
    this.options = this.layoutNode.options || {};
    this.jsf.initializeControl(this);
    this.tryResolveValue();
  }

  private tryResolveValue() {
    if (this.shouldDisplay() && !this.hasValue()) {
      this.resolveNullOrEmptyValue();
    } else if (this.layoutNode.options.extraType == 'currency') {
      this.resolveCurrencyValue();
    } else if (typeof this.controlValue === 'boolean') {
      this.resolveBoolValue();
    } else if (this.hasEnumType()) {
      this.resolveEnumValue();
    }

    if (!this.hasEnumType()) {
      this.displayValue = this.shouldDisplay() && !this.hasValue() ? "-" : this.controlValue;
    }
    this.jsf.updateValue(this, this.controlValue);
  }

  /** Resolves null, undefined or empty*/
  private resolveNullOrEmptyValue() {
    if (this.layoutNode.dataType === 'number') {
      return;
    }

    this.controlValue = "-";
  }

  /**
 * Resolves currency
 * 
 * @example
 * ```
  {
    "key": "amount",
    "labelHtmlClass": "font-bold",
    "type": "label-only",
    "extraType": "currency",
    "flex": "1 1 280px",
    "title": "Amount"
  }
 * ```
 */
  private resolveCurrencyValue() {
    this.controlValue = parseFloat(this.formControl.value).toLocaleString('el');
  }

  /** Resolves bool */
  private resolveBoolValue() {
    this.controlValue = this.controlValue ? "Ναι" : "Όχι";
  }

  /** Resolves enumeration*/
  private resolveEnumValue() {
    (this.options.enum as string[]).forEach((enumValue, index) => {
      if (this.controlValue === enumValue) {
        this.displayValue = this.options.enumNames[index];
      }
    });
  }

  /** Checks whether layoutNode contains enumeration values */
  private hasEnumType(): boolean {
    return this.options.enum &&
      this.options.enumNames &&
      Array.isArray(this.options.enum) &&
      Array.isArray(this.options.enumNames) &&
      this.options.enum.length === this.options.enumNames.length;
  }

  /** Checks whether controlValue has Value */
  private hasValue(): boolean {
    return this.controlValue !== null && this.controlValue !== undefined && this.controlValue !== '';
  } 

  /** Checks whether should display current node */
  private shouldDisplay(): boolean {
    return this.options.extraType !== 'blankspace' && !this.options.notitle;
  }
}