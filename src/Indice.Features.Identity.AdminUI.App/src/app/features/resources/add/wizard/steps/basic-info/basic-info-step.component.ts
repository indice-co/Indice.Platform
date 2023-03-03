import { Component, OnInit } from '@angular/core';
import { AbstractControl } from '@angular/forms';

import { StepBaseComponent } from 'src/app/shared/components/step-base/step-base.component';
import { ResourceWizardModel } from '../../../models/resource-wizard-model';

@Component({
  selector: 'app-basic-info-step',
  templateUrl: './basic-info-step.component.html'
})
export class BasicInfoStepComponent extends StepBaseComponent<ResourceWizardModel> implements OnInit {
  constructor() {
    super();
  }

  public get displayType(): boolean {
    return this.data.displayType;
  }

  public get type(): AbstractControl {
    return this.data.form.get('type');
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

  public ngOnInit(): void {
    if (this.data.navigationOrigin) {
      this.data.form.get('type').setValue(this.data.navigationOrigin);
    }
  }

  public isValid(): boolean {
    return this.name.valid && this.displayName.valid && this.description.valid && this.type.valid;
  }
}
