import { JsonSchemaFormService } from '@ajsf-extended/core';
import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-input-widget',
  templateUrl: './text-area-widget.component.html',
  styleUrls: ['./text-area-widget.component.scss']
})
export class TextAreaWidgetComponent implements OnInit {
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
  }

  updateValue(event: any) {
    this.jsf.updateValue(this, event.target.value);
  }
}
