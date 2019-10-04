import { Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';

import { StepBaseComponent } from '../step-base.component';
import { CreateClientRequest, IdentityApiService } from 'src/app/core/services/identity-api.service';
import { ToastService } from 'src/app/layout/services/app-toast.service';

@Component({
    selector: 'app-summary-step',
    templateUrl: './summary-step.component.html'
})
export class SummaryStepComponent extends StepBaseComponent implements OnInit {
    constructor(private _api: IdentityApiService, private _router: Router, private _route: ActivatedRoute, public _toast: ToastService) {
        super();
    }

    public summary: CreateClientRequest = new CreateClientRequest();

    public ngOnInit(): void {
        this.summary = this.getSummary();
    }

    public isValid(): boolean {
        return true;
    }
}
