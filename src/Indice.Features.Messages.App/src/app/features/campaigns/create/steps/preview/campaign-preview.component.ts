import { Component, Input, OnInit } from '@angular/core';
import { UntypedFormControl, UntypedFormGroup } from '@angular/forms';

import { CampaignPreview } from './campaign-preview';


export interface IPreviewModel {
  published: boolean;
}

@Component({
  selector: 'app-campaign-preview',
  templateUrl: './campaign-preview.component.html'
})
export class CampaignPreviewComponent implements OnInit {
    constructor() { }
    
    // Input & Output parameters
    @Input() public data!: CampaignPreview;

    public model: IPreviewModel = { published: false };
    // Properties
    public form!: UntypedFormGroup;

    public ngOnInit(): void {
        this._initForm();
    }

    private _initForm(): void {
        this.form = new UntypedFormGroup({
          published: new UntypedFormControl(false)
        });
    }

}
