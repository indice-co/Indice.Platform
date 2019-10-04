import { Component, OnInit } from '@angular/core';

import { StepBaseComponent } from '../step-base.component';

@Component({
  selector: 'app-basic-info-step',
  templateUrl: './basic-info-step.component.html'
})
export class BasicInfoStepComponent extends StepBaseComponent implements OnInit {
  constructor() {
    super();
  }

  public ngOnInit(): void { }

  public isValid(): boolean {
    return false;
  }
}
