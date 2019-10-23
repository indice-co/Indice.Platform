import { Component } from '@angular/core';
import { AbstractControl } from '@angular/forms';

import { ApiResourceWizardModel } from '../../../../models/api-resource-wizard-model';
import { StepBaseComponent } from 'src/app/shared/components/step-base/step-base.component';

@Component({
  selector: 'app-basic-info-step',
  templateUrl: './basic-info-step.component.html'
})
export class BasicInfoStepComponent extends StepBaseComponent<ApiResourceWizardModel> {
  constructor() {
    super();
  }

  public get name(): AbstractControl {
    return this.data.form.get('name');
  }

  public get displayName(): AbstractControl {
    return this.data.form.get('displayName');
  }

  public get description(): AbstractControl {
    return this.data.form.get('description');
  }

  public isValid(): boolean {
    return this.name.valid && this.displayName.valid && this.description.valid;
  }
}
