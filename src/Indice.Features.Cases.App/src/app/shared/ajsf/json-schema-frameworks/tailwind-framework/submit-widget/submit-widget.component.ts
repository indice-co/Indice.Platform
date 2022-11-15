import { AbstractControl } from '@angular/forms';
import { Component, Input, OnInit } from '@angular/core';
import { hasOwn, JsonSchemaFormService } from '@ajsf-extended/core';

@Component({
  // tslint:disable-next-line:component-selector
  selector: 'app-submit-widget',
  template: `
    <div
      [class]="options?.htmlClass || ''">
      <button
       class="bg-blue-800 hover:bg-blue-700 text-white font-bold p-2 mt-2 inline-flex rounded"
        [attr.aria-describedby]="'control' + layoutNode?._id + 'Status'"
        [attr.readonly]="options?.readonly ? 'readonly' : null"
        [attr.required]="options?.required"
        [class]="options?.fieldHtmlClass || ''"
        [ngClass]="{'opacity-50 cursor-not-allowed': controlDisabled === true}"
        [disabled]="controlDisabled"
        [id]="'control' + layoutNode?._id"
        [name]="controlName"
        [type]="layoutNode?.type"
        (click)="updateValue($event)">
        <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M8 7H5a2 2 0 00-2 2v9a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-3m-1 4l-3 3m0 0l-3-3m3 3V4" />
                  </svg>&nbsp;{{isDraft ? 'Δημιουργία Νέας Αίτησης' : 'Αποθήκευση'}}
      </button>
    </div>`
})
export class SubmitWidgetComponent implements OnInit {
  formControl: AbstractControl | undefined;
  controlName: string | undefined;
  controlValue: any;
  controlDisabled = false;
  boundControl = false;
  isDraft = false;
  options: any;
  @Input() layoutNode: any;
  @Input() layoutIndex: number[] | undefined;
  @Input() dataIndex: number[] | undefined;

  constructor(
    private jsf: JsonSchemaFormService
  ) { }

  ngOnInit() {
    this.options = this.layoutNode.options || {};
    this.isDraft = this.jsf.formOptions.draft;
    this.jsf.initializeControl(this);
    if (hasOwn(this.options, 'disabled')) {
      this.controlDisabled = this.options.disabled;
    } else if (this.jsf.formOptions.disableInvalidSubmit) {
      this.controlDisabled = !this.jsf.isValid;
      this.jsf.isValidChanges.subscribe((isValid: boolean) => this.controlDisabled = !isValid);
    }
    if (this.controlValue === null || this.controlValue === undefined) {
      this.controlValue = this.options.title || 'Αποθήκευση';
    }
  }

  updateValue(event: any) {
    if (typeof this.options.onClick === 'function') {
      this.options.onClick(event);
    } else {
      // this erases the value of the input probably because its a button and not an input[type=submit] anymore
      //   this.jsf.updateValue(this, event.target.value);
    }
  }
}