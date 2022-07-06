import { AbstractControl } from '@angular/forms';
import { Component, Input, OnInit } from '@angular/core';
import { buildTitleMap, isArray, JsonSchemaFormService } from '@ajsf/core';

@Component({
  selector: 'app-select-widget',
  templateUrl: './select-widget.component.html',
  styleUrls: ['./select-widget.component.scss']
})
export class SelectWidgetComponent implements OnInit {
  formControl: any;
  controlName: string | undefined;
  controlValue: any;
  controlDisabled = false;
  boundControl = false;
  options: any;
  selectList: any[] = [];
  isArray = isArray;
  @Input() layoutNode: any;
  @Input() layoutIndex: number[] | undefined;
  @Input() dataIndex: number[] | undefined;

  constructor(
    private jsf: JsonSchemaFormService
  ) { }

  ngOnInit() {
    this.options = this.layoutNode.options || {};
    this.selectList = buildTitleMap(
      this.options.titleMap || this.options.enumNames,
      this.options.enum, !!this.options.required, !!this.options.flatList
    );
    this.jsf.initializeControl(this);
  }

  updateValue(event: any) {
    this.jsf.updateValue(this, event.target.value);
  }
}
