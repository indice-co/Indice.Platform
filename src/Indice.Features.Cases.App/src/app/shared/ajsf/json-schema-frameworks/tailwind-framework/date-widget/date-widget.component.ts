import { MenuOption } from '@indice/ng-components';
import { JsonSchemaFormService } from '@ajsf-extended/core';
import { Component, Input, OnInit } from '@angular/core';
import * as  moment from 'moment';

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
  min: string = '';
  max: string = '';

  readonly minDateDays = 0;
  readonly minDateMonths = 0;
  readonly minDateYears = -100;
  readonly maxDateDays = 0;
  readonly maxDateMonths = 0;
  readonly maxDateYears = 100;

  constructor(
    private jsf: JsonSchemaFormService
  ) { }

  ngOnInit() {
    this.options = this.layoutNode.options || {};

    this.min = this.setMinDate(this.options);
    this.max = this.setMaxDate(this.options);

    this.jsf.initializeControl(this);
  }

  updateValue(event: any) {
    this.jsf.updateValue(this, event.target.value);
  }

  private setMinDate(options: any): string {
    if (this.options.min !== undefined) {
      return this.options.min;
    }
    const currentDate = moment(new Date());
    currentDate.add(options.minDateYears ?? this.minDateYears, 'years').year();
    currentDate.add(options.minDateMonths ?? this.minDateMonths, 'months').month();
    currentDate.add(options.minDateDays ?? this.minDateDays, 'days').date();

    return currentDate.format("YYYY-MM-DD");
  }

  private setMaxDate(options: any): string {
    if (this.options.max) {
      return this.options.max
    }
    const currentDate = moment(new Date());
    currentDate.add(options.maxDateYears ?? this.maxDateYears, 'years').year();
    currentDate.add(options.maxDateMonths ?? this.maxDateMonths, 'months').month();
    currentDate.add(options.maxDateDays ?? this.maxDateDays, 'days').date();

    return currentDate.format("YYYY-MM-DD");
  }
}
