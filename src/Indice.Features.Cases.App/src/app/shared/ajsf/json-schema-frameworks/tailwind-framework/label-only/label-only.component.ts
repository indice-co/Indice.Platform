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

  constructor(
    private jsf: JsonSchemaFormService
  ) { }

  ngOnInit() {
    this.options = this.layoutNode.options || {};
    this.jsf.initializeControl(this);
    this.trySetEnumType();
    this.trySetCurrencyType();
  }

  /**
 * Configures label to be in currency format if passed as extraType option
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
  private trySetCurrencyType() {
    if (this.layoutNode.options.extraType == 'currency') {
      this.controlValue = parseFloat(this.formControl.value).toLocaleString('el');
      this.jsf.updateValue(this, this.controlValue);
    }
  }

  /** Attempts to set enumeration to corresponding values if exist*/
  private trySetEnumType() {
    if (this.hasEnumType()) {
      (this.options.enum as string[]).forEach((enumValue, index) => {
        if (this.controlValue === enumValue) {
          this.controlValue = this.options.enumNames[index];
          this.jsf.updateValue(this, this.controlValue);
        }
      });
    }
  }

/** Checks whether layoutNode contains enumeration values */
  private hasEnumType(): boolean {
    return this.options.enum &&
      this.options.enumNames &&
      Array.isArray(this.options.enum) &&
      Array.isArray(this.options.enumNames) &&
      this.options.enum.length === this.options.enumNames.length;
  }
}