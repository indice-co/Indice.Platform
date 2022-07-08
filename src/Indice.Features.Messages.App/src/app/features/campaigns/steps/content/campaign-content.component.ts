import { Component, Input, OnInit, ViewChild } from '@angular/core';
import { AbstractControl, UntypedFormControl, UntypedFormGroup, Validators } from '@angular/forms';

import * as Handlebars from 'handlebars/dist/cjs/handlebars';
import { LibTabComponent, LibTabGroupComponent } from '@indice/ng-components';
import { MessageChannelKind, MessagesApiClient, Template } from 'src/app/core/services/messages-api.service';
import { ValidationService } from 'src/app/core/services/validation.service';
import { UtilitiesService } from 'src/app/shared/utilities.service';
import { ChannelState } from './channel-state';

@Component({
    selector: 'app-campaign-content',
    templateUrl: './campaign-content.component.html'
})
export class CampaignContentComponent implements OnInit {
    private _samplePayload: any = {
        contact: {
            id: 'FA24F7D6-332F-4E17-8E43-A111DB32E7CC',
            recipientId: '05B7B29D-8969-425A-A1FC-D5222553336F',
            salutation: 'Mr.',
            firstName: 'John',
            lastName: 'Doe',
            fullName: 'John Doe',
            email: 'email@john-doe.com',
            phoneNumber: '(212)-456-7890'
        }
    };

    @ViewChild('tabGroup', { static: true }) private _tabGroup!: LibTabGroupComponent;
    private _currentValidDataObject: any = undefined;

    constructor(
        private _validationService: ValidationService,
        private _utilities: UtilitiesService,
        private _api: MessagesApiClient
    ) { }

    // Input & Output parameters
    @Input() public externalData: any = {};
    // Form Controls
    public get data(): AbstractControl { return this.form.get('data')!; }
    public get inboxBody(): AbstractControl { return this.form.get('inboxBody')!; }
    public get inboxSubject(): AbstractControl { return this.form.get('inboxSubject')!; }
    public get emailBody(): AbstractControl { return this.form.get('emailBody')!; }
    public get emailSubject(): AbstractControl { return this.form.get('emailSubject')!; }
    public get pushNotificationBody(): AbstractControl { return this.form.get('pushNotificationBody')!; }
    public get pushNotificationSubject(): AbstractControl { return this.form.get('pushNotificationSubject')!; }
    public get smsBody(): AbstractControl { return this.form.get('smsBody')!; }
    public get smsSubject(): AbstractControl { return this.form.get('smsSubject')!; }
    // Properties
    public form!: UntypedFormGroup;
    public subjectPreview: string = '';
    public MessageChannelKind = MessageChannelKind;
    public showInboxTab = false;
    public showPushNotificationTab = false;
    public showEmailTab = false;
    public showSmsTab = false;
    public bodyPreview: string = '';

    public get samplePayload(): any {
        return {
            ...this._samplePayload,
            data: this._currentValidDataObject ? { ...this._currentValidDataObject } : null,
            ...this.externalData
        };
    }

    public ngOnInit(): void {
        this._initForm();
    }

    public onContentTabChanged(tab: LibTabComponent): void {
        let subject, body = '';
        switch (tab.id) {
            case 'inbox-tab':
                subject = this.inboxSubject.value;
                body = this.inboxBody.value;
                break;
            case 'push-notification-tab':
                subject = this.pushNotificationSubject.value;
                body = this.pushNotificationBody.value;
                break;
            case 'email-tab':
                subject = this.emailSubject.value;
                body = this.emailBody.value;
                break;
            case 'sms-tab':
                subject = this.smsSubject.value;
                body = this.smsBody.value;
                break;
        }
        this._setContentSubjectPreview(subject);
        this._setContentBodyPreview(body);
    }

    public onSubjectInput(event: any, channel: MessageChannelKind): void {
        this._setContentSubject(event.target.value, channel, true);
    }

    public onBodyInput(event: any, channel: MessageChannelKind): void {
        this._setContentBody(event.target.value, channel, true);
    }

    public onCampaignMetadataInput(event: any): void {
        const value = event.target.value;
        if (this._utilities.isValidJson(value)) {
            this._currentValidDataObject = JSON.parse(value);
        }
    }

    public initStep(channelsState: ChannelState[], templateId: string | undefined): void {
        this._resetTabs();
        this._resetContentValidators();
        const showInboxTab = channelsState.find(x => x.value === MessageChannelKind.Inbox)!.checked;
        const showPushNotificationTab = channelsState.find(x => x.value === MessageChannelKind.PushNotification)!.checked;
        const showSmsTab = channelsState.find(x => x.value === MessageChannelKind.SMS)!.checked;
        const showEmailTab = channelsState.find(x => x.value === MessageChannelKind.Email)!.checked;
        if (showInboxTab) {
            this.showInboxTab = true;
            this.inboxSubject.setValidators(Validators.required);
            this.inboxBody.setValidators(Validators.required);
        }
        if (showPushNotificationTab) {
            this.showPushNotificationTab = true;
            this.pushNotificationSubject.setValidators(Validators.required);
            this.pushNotificationBody.setValidators(Validators.required);
        }
        if (showSmsTab) {
            this.showSmsTab = true;
            this.smsSubject.setValidators(Validators.required);
            this.smsBody.setValidators(Validators.required);
        }
        if (showEmailTab) {
            this.showEmailTab = true;
            this.emailSubject.setValidators(Validators.required);
            this.emailBody.setValidators(Validators.required);
        }
        if (templateId) {
            this._api.getTemplateById(templateId).subscribe((template: Template) => {
                this._setContent(template);
            });
        }
    }

    private _setContentBody(value: string | undefined, channel: MessageChannelKind, preview: boolean = false): void {
        if (!value) {
            return;
        }
        if (preview) {
            this._setContentBodyPreview(value);
        }
        switch (channel) {
            case MessageChannelKind.Email:
                this.emailBody.setValue(value);
                break;
            case MessageChannelKind.SMS:
                this.smsBody.setValue(value);
                break;
            case MessageChannelKind.PushNotification:
                this.pushNotificationBody.setValue(value);
                break;
            case MessageChannelKind.Inbox:
                this.inboxBody.setValue(value);
                break;
        }
    }

    private _setContentSubject(value: string | undefined, channel: MessageChannelKind, preview: boolean = false): void {
        if (!value) {
            return;
        }
        if (preview) {
            this._setContentSubjectPreview(value);
        }
        switch (channel) {
            case MessageChannelKind.Email:
                this.emailSubject.setValue(value);
                break;
            case MessageChannelKind.SMS:
                this.smsSubject.setValue(value);
                break;
            case MessageChannelKind.PushNotification:
                this.pushNotificationSubject.setValue(value);
                break;
            case MessageChannelKind.Inbox:
                this.inboxSubject.setValue(value);
                break;
        }
    }

    private _setContentSubjectPreview(value: string | undefined): void {
        try {
            const template = Handlebars.compile(value);
            this.subjectPreview = template(this.samplePayload);
        } catch (error) { }
    }

    private _setContentBodyPreview(value: string | undefined): void {
        try {
            const template = Handlebars.compile(value);
            this.bodyPreview = template(this.samplePayload);
        } catch (error) { }
    }

    private _initForm(): void {
        this.form = new UntypedFormGroup({
            data: new UntypedFormControl(null, this._validationService.invalidJsonValidator()),
            emailSubject: new UntypedFormControl(''),
            emailBody: new UntypedFormControl(''),
            inboxSubject: new UntypedFormControl(''),
            inboxBody: new UntypedFormControl(''),
            smsSubject: new UntypedFormControl(''),
            smsBody: new UntypedFormControl(''),
            pushNotificationSubject: new UntypedFormControl(''),
            pushNotificationBody: new UntypedFormControl('')
        });
    }

    private _setContent(template: Template | undefined): void {
        if (!template) {
            return;
        }
        this._setContentSubject(template.content?.inbox?.title, MessageChannelKind.Inbox, this._tabGroup.currentTab?.id === 'inbox-tab');
        this._setContentBody(template.content?.inbox?.body, MessageChannelKind.Inbox, this._tabGroup.currentTab?.id === 'inbox-tab');
        this._setContentSubject(template.content?.email?.title, MessageChannelKind.Email, this._tabGroup.currentTab?.id === 'email-tab');
        this._setContentBody(template.content?.email?.body, MessageChannelKind.Email, this._tabGroup.currentTab?.id === 'email-tab');
        this._setContentSubject(template.content?.sms?.title, MessageChannelKind.SMS, this._tabGroup.currentTab?.id === 'sms-tab');
        this._setContentBody(template.content?.sms?.body, MessageChannelKind.SMS, this._tabGroup.currentTab?.id === 'sms-tab');
        this._setContentSubject(template.content?.pushNotification?.title, MessageChannelKind.PushNotification, this._tabGroup.currentTab?.id === 'push-notification-tab');
        this._setContentBody(template.content?.pushNotification?.body, MessageChannelKind.PushNotification, this._tabGroup.currentTab?.id === 'push-notification-tab');
    }

    private _resetTabs(): void {
        this.showInboxTab = false;
        this.showEmailTab = false;
        this.showPushNotificationTab = false;
        this.showSmsTab = false;
    }

    private _resetContentValidators(): void {
        this.smsSubject.removeValidators(Validators.required);
        this.smsBody.removeValidators(Validators.required);
        this.emailSubject.removeValidators(Validators.required);
        this.emailBody.removeValidators(Validators.required);
        this.pushNotificationSubject.removeValidators(Validators.required);
        this.pushNotificationBody.removeValidators(Validators.required);
        this.inboxSubject.removeValidators(Validators.required);
        this.inboxBody.removeValidators(Validators.required);
    }
}
