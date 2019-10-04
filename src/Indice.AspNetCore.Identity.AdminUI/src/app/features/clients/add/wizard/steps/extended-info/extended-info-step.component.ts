import { Component, OnInit } from '@angular/core';
import { AbstractControl } from '@angular/forms';

import { StepBaseComponent } from '../step-base.component';
import { UtilitiesService } from 'src/app/core/services/utilities.services';

@Component({
  selector: 'app-extended-info-step',
  templateUrl: './extended-info-step.component.html'
})
export class ExtendedInfoStepComponent extends StepBaseComponent implements OnInit {
  constructor(private _utilities: UtilitiesService) {
    super();
  }

  public get clientId(): AbstractControl {
    return this.data.form.get('clientId');
  }

  public get clientName(): AbstractControl {
    return this.data.form.get('clientName');
  }

  public get clientUrl(): AbstractControl {
    return this.data.form.get('clientUrl');
  }

  public get logoUrl(): AbstractControl {
    return this.data.form.get('logoUrl');
  }

  public get description(): AbstractControl {
    return this.data.form.get('description');
  }

  public get requireConsent(): AbstractControl {
    return this.data.form.get('requireConsent');
  }

  public ngOnInit(): void { }

  public generateName(): void {
    this.clientId.setValue(this._utilities.newGuid());
  }

  public isValid(): boolean {
    return this.clientId.valid && this.clientName.valid && this.clientUrl.valid && this.logoUrl.valid && this.description.valid && this.requireConsent.valid;
  }
}
