import { Component, Input, OnInit } from '@angular/core';
import { AbstractControl, UntypedFormControl, UntypedFormGroup } from '@angular/forms';

import { CampaignPreview } from './campaign-preview';

@Component({
    selector: 'app-campaign-preview',
    templateUrl: './campaign-preview.component.html'
})
export class CampaignPreviewComponent implements OnInit {
    constructor() { }
    
    // Input & Output parameters
    @Input() public form!: UntypedFormGroup;
    @Input() public data!: CampaignPreview;
    // Form Controls
    public get published(): AbstractControl { return this.form.get('published')!; }

    public ngOnInit(): void {
        this._initForm();
    }

    private _initForm(): void {
        this.form = new UntypedFormGroup({
            published: new UntypedFormControl(false)
        });
    }
}
