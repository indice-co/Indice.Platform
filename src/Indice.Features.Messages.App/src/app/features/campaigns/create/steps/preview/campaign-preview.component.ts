import { Component, Input, OnInit } from '@angular/core';
import { UntypedFormControl, UntypedFormGroup } from '@angular/forms';

import { CampaignPreview } from './campaign-preview';


export interface IPreviewModel {
  published: boolean;
  ignoreUserPreferences: boolean;
}

@Component({
  selector: 'app-campaign-preview',
  templateUrl: './campaign-preview.component.html'
})
export class CampaignPreviewComponent implements OnInit {
  constructor() { }

  // Input & Output parameters
  @Input() public data!: CampaignPreview;

  public model: IPreviewModel = { published: false, ignoreUserPreferences: false };
  // Properties
  public form!: UntypedFormGroup;

  public ngOnInit(): void {
  }

}
