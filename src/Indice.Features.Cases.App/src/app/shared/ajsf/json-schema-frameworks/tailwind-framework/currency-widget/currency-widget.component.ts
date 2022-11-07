import { JsonSchemaFormService } from '@ajsf-extended/core';
import { Component, Input, OnInit } from '@angular/core';

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
    this.formControl.valueChanges.subscribe(
      (value: string) => {
        const number = parseFloat(value);
        this.displayValue = number.toLocaleString('el');
      }
    );
  }

  updateValue(event: any) {
    // force replace masked value input into global decimal format (eg 5.125.000,03 --> 5125000.03)
    const controlValue = parseFloat(event.target.value.replace(/[.]/g, '').replace(/[,]/g, '.')); // do we really need to parseFloat?
    this.displayValue = event.target.value;
    this.jsf.updateValue(this, controlValue);
  }
}
