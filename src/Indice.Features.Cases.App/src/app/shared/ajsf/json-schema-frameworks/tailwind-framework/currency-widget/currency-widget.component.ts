import { JsonSchemaFormService } from '@ajsf-extended/core';
import { Component, Input, OnInit, OnDestroy } from '@angular/core';

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

  thousandSeparator: string = ".";
  // this is the placeholder for the mask input. The actual control value is a hidden input
  displayValue = '';

  constructor(
    private jsf: JsonSchemaFormService
  ) { }

  ngOnInit() {
    this.options = this.layoutNode.options || {};
    this.jsf.initializeControl(this);
    if (this.formControl.value) {
      const number = parseFloat(this.formControl.value);
      this.formControl.value = number.toLocaleString('el');
      this.displayValue = this.formControl.value;
    }
  }

  updateValue(event: any) {
    // force replace masked value input into global decimal format (eg 5.125.000,03 --> 5125000.03)
    const controlValue = parseFloat(event.target.value.replace(/[.]/g, '').replace(/[,]/g, '.'));
    this.displayValue = event.target.value;
    this.jsf.updateValue(this, controlValue);    
  }
}
