import { JsonSchemaFormService } from '@ajsf-extended/core';
import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-date-widget',
  templateUrl: './date-widget.component.html',
  styleUrls: ['./date-widget.component.scss']
})
export class DateWidgetComponent implements OnInit {
  formControl: any;
  controlName: string | undefined;
  controlValue: string | undefined;
  controlDisabled = false;
  boundControl = false;
  options: any;
  autoCompleteList: string[] = [];
  @Input() layoutNode: any;
  @Input() layoutIndex: any[] = [];
  @Input() dataIndex: any[] = [];
  min: string = ''
  max: string = ''
  displayValue = '';

  constructor(
    private jsf: JsonSchemaFormService
  ) { }

  ngOnInit() {
    this.options = this.layoutNode.options || {};
    if (this.options.min !== undefined) {
      this.min = this.options.min;
    }
    if (this.options.max !== undefined) {
      this.max = this.options.max;
    }
    this.jsf.initializeControl(this);
    if (this.formControl.value) {
      this.displayValue = this.formControl.value;
    }
  }

  updateValue(event: any) {
    this.displayValue = event.target.value;
    this.jsf.updateValue(this, event.target.value);
  }

}
