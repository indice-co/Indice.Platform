import { Component, OnInit } from '@angular/core';
import { AbstractControl, UntypedFormControl, UntypedFormGroup, Validators } from '@angular/forms';

import { MenuOption } from '@indice/ng-components';
import { map } from 'rxjs/operators';
import { DistributionListResultSet, MessagesApiClient } from 'src/app/core/services/messages-api.service';

@Component({
    selector: 'app-campaign-recipients',
    templateUrl: './campaign-recipients.component.html'
})
export class CampaignRecipientsComponent implements OnInit {
    constructor(private _api: MessagesApiClient) { }

    // Form Controls
    public get sendVia(): AbstractControl { return this.form.get('sendVia')!; }
    public get distributionList(): AbstractControl { return this.form.get('distributionList')!; }
    public get recipientIds(): AbstractControl { return this.form.get('recipientIds')!; }
    // Properties
    public get recipientsCount(): number {
        return this.recipientIds.value?.split('\n').filter((x: string) => x !== '').length || 0;
    }

    public distributionLists: MenuOption[] = [new MenuOption('Παρακαλώ επιλέξτε...', null)];
    public form!: UntypedFormGroup;

    public ngOnInit(): void {
        this._initForm();
        this._loadDistributionLists();
    }

    public onSendViaChanged(event: any): void {
        const value = event.target.value;
        this.recipientIds.removeValidators(Validators.required);
        this.distributionList.removeValidators(Validators.required);
        if (value === 'distribution-list') {
            this.distributionList.setValidators(Validators.required);
            this.recipientIds.setValue(null);
        } else if (value === 'recipient-ids') {
            this.recipientIds.setValidators(Validators.required);
            this.distributionList.setValue(null);
        }
        this.distributionList.updateValueAndValidity();
        this.recipientIds.updateValueAndValidity();
        this.sendVia.setValue(value);
    }

    private _loadDistributionLists(): void {
        this._api
            .getDistributionLists()
            .pipe(map((distributionLists: DistributionListResultSet) => {
                if (distributionLists.items) {
                    this.distributionLists.push(...distributionLists.items.map(list => new MenuOption(list.name || '', list.id)));
                }
            }))
            .subscribe();
    }

    private _initForm(): void {
        this.form = new UntypedFormGroup({
            sendVia: new UntypedFormControl('distribution-list'),
            distributionList: new UntypedFormControl(undefined, [Validators.required]),
            recipientIds: new UntypedFormControl()
        });
    }
}
