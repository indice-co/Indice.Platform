import { Component, OnInit } from '@angular/core';
import { AbstractControl } from '@angular/forms';

import { StepBaseComponent } from '../step-base.component';
import { UtilitiesService } from 'src/app/core/services/utilities.services';

@Component({
  selector: 'app-basic-info-step',
  templateUrl: './basic-info-step.component.html'
})
export class BasicInfoStepComponent extends StepBaseComponent implements OnInit {
  constructor(private _utilities: UtilitiesService) {
    super();
  }

  public get clientId(): AbstractControl {
    return this.data.form.get('clientId');
  }

  public get clientName(): AbstractControl {
    return this.data.form.get('clientName');
  }

  public get description(): AbstractControl {
    return this.data.form.get('description');
  }

  public ngOnInit(): void { }

  public generateName(): void {
    this.clientId.setValue(this._utilities.newGuid());
  }

  public isValid(): boolean {
    return this.clientId.valid && this.clientName.valid && this.description.valid;
  }
}
