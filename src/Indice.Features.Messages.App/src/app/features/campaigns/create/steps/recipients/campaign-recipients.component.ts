import { Component, OnInit, ViewChild } from '@angular/core';
import { AbstractControl, UntypedFormControl, UntypedFormGroup, Validators } from '@angular/forms';

import { MenuOption, SidePaneComponent } from '@indice/ng-components';
import { map } from 'rxjs/operators';
import { settings } from 'src/app/core/models/settings';
import { Contact, DistributionListResultSet, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { ListContactCreateComponent } from 'src/app/shared/components/list-contact-create/list-contact-create.component';

@Component({
    selector: 'app-campaign-recipients',
    templateUrl: './campaign-recipients.component.html'
})
export class CampaignRecipientsComponent implements OnInit {
    @ViewChild('contactCreateComponent', { static: false }) public contactCreateComponent!: ListContactCreateComponent;
    @ViewChild('rightPane', { static: false }) public rightPaneComponent!: SidePaneComponent;
    constructor(private _api: MessagesApiClient) { }

    // Form Controls
    public get sendVia(): AbstractControl { return this.form.get('sendVia')!; }
    public get distributionList(): AbstractControl { return this.form.get('distributionList')!; }
    public get recipientIds(): AbstractControl { return this.form.get('recipientIds')!; }
    public get recipients(): AbstractControl { return this.form.get('recipients')!; }
    // Properties
    public get recipientsCount(): number {
        return this.recipientIds.value?.split('\n').filter((x: string) => x !== '').length || 0;
    }

    public distributionLists: MenuOption[] = [new MenuOption('Παρακαλώ επιλέξτε...', null)];
    public form!: UntypedFormGroup;
    public apiUrl = settings.api_url;

    public ngOnInit(): void {
        this._initForm();
        this._loadDistributionLists();
    }

    public onSendViaChanged(event: any): void {
        const value = event.target.value;
        this.recipients.reset();
        this.recipientIds.reset();
        this.distributionList.reset();
        this.recipientIds.removeValidators(Validators.required);
        this.distributionList.removeValidators(Validators.required);
        this.recipients.removeValidators(Validators.required);
        this.contactCreateComponent.reset();
        if (value === 'distribution-list') {
            this.distributionList.setValidators(Validators.required);
            this.recipientIds.setValue(null);
            this.recipients.setValue(null);
        } else if (value === 'recipient-ids') {
            this.recipientIds.setValidators(Validators.required);
            this.distributionList.setValue(null);
            this.recipients.setValue(null);
        } else if (value === 'recipients') {
            this.recipientIds.setValue(null);
            this.distributionList.setValue(null);
            this.recipients.setValidators(Validators.required);
        }
        this.distributionList.updateValueAndValidity();
        this.recipientIds.updateValueAndValidity();
        this.recipients.updateValueAndValidity();
        this.sendVia.setValue(value);
    }

    public setRecipients(recipients: Contact[]) {
        this.recipients.setValue(recipients);
        this.recipients.markAsTouched();
    }

    public openSidePane() {
        this.rightPaneComponent.show();
    }

    public closeSidePane() {
        this.rightPaneComponent.hide();
    }

    private _loadDistributionLists(): void {
        this._api
            .getDistributionLists(undefined, undefined, undefined, undefined, false)
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
            recipientIds: new UntypedFormControl(),
            recipients: new UntypedFormControl()
        });
    }
}
