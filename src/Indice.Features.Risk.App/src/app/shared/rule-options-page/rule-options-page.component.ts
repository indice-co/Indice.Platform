import { Component, Input, OnInit } from '@angular/core';
import { DataService } from 'src/app/core/services/data.service';
import { RiskApiService, RuleOptions } from 'src/app/core/services/risk-api.service';
import { ToasterService, ToastType } from "@indice/ng-components";

@Component({
    selector: 'app-rule-options-page',
    templateUrl: './rule-options-page.component.html',
    styleUrls: ['./rule-options-page.component.scss']
})

export class RuleOptionsPageComponent implements OnInit {
    @Input() ruleOptions: { [key: string]: string }

    public title?: string;
    public basicOptions: { [key: string]: string }
    public extraOptions: { [key: string]: string }

    private eligibleEventsKey: string = 'eligibleEvents';
    private ruleIdKey: string = 'name'
    private enabledKey: string = 'enabled'
    private baseOptionsKeys = ['friendlyName', 'description', this.enabledKey];

    constructor(
        private dataService: DataService,
        private _api: RiskApiService,
        private _toaster: ToasterService) {
    }

    saveRuleOptions() {
        // set the type discriminator
        this.ruleOptions[this.ruleIdKey] = `${this.ruleOptions[this.ruleIdKey]}RuleOptions`;
        
        const request: RuleOptions = JSON.parse(JSON.stringify(this.ruleOptions)) as RuleOptions;
        if (this.ruleOptions[this.eligibleEventsKey].trim() !== '') {
            request.eligibleEvents = this.ruleOptions[this.eligibleEventsKey].split(',');
        } else {
            request.eligibleEvents = [];
        }
        console.log(request);
        this._api
            .updateRiskRuleOptions(this.title, request)
            .pipe()
            .subscribe({
                next: (response) => {
                    this._toaster.show(ToastType.Success, 'Successfully saved', 'Your options have been saved successfully.', 10000);
                },
                error: (error) => {
                    let errorMessage = 'An errored occured.'
                    if (error && error.errors) {
                        errorMessage = Object.values(error.errors).flatMap((message: any) => message).join('\n');
                    }
                    this._toaster.show(ToastType.Error, 'Failed to save', errorMessage, 10000);
                } 
            });
    }

    onOptionValueChanged(event: any, key: string) {
        this.ruleOptions[key] = event;
    }

    onToggleChanged(event: any) {
        this.ruleOptions[this.enabledKey] = (event === true) ? 'True' : 'False';
    }

    ngOnInit(): void {
        this.dataService.inputData$.subscribe(data => {
            this.ruleOptions = data;

            this.basicOptions = Object.fromEntries(
                Object.entries(this.ruleOptions).filter(([key, value]) => !key.startsWith(this.eligibleEventsKey) && key !== this.ruleIdKey)
            );

            this.splitOptions(this.basicOptions);

            this.title = this.ruleOptions[this.ruleIdKey]
        });
    }

    private splitOptions(obj: { [key: string]: string }): any {
        const baseOptions: { [key: string]: string } = {};
        const extraOptions: { [key: string]: string } = {};
        Object.entries(obj).forEach(([key, value]) => {
            if (this.baseOptionsKeys.includes(key)) {
                baseOptions[key] = value;
            } else {
                extraOptions[key] = value;
            }
        });
        this.basicOptions = baseOptions;
        this.extraOptions = extraOptions;
    }
}