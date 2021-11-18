import { Component, OnInit, ViewChild } from '@angular/core';
import { NgForm } from '@angular/forms';

import { CreateCampaignRequest, Period } from 'src/app/core/services/campaigns-api.services';

@Component({
    selector: 'app-campaigns',
    templateUrl: './campaign-create.component.html'
})
export class CampaignCreateComponent implements OnInit {
    @ViewChild('campaignForm', { static: false }) private campaignForm!: NgForm;

    constructor() { }

    public now: Date = new Date();
    public model: CreateCampaignRequest = new CreateCampaignRequest({ activePeriod: new Period({ from: this.now }) });

    public ngOnInit(): void { }

    public onSubmit(): void {
        console.log(this.campaignForm);
    }

    public toDate(event: any): Date | undefined {
        var value = event.target.value
        if (value) {
            return new Date(value);
        }
        return undefined;
    }
}
