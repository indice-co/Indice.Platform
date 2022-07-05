import { AfterViewChecked, ChangeDetectorRef, Component, Inject, OnInit, ViewChild } from '@angular/core';
import { AbstractControl, UntypedFormControl, UntypedFormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';

import { LibStepperComponent, LibTabComponent, LibTabGroupComponent, MenuOption, StepperType, ToasterService, ToastType } from '@indice/ng-components';
import { map } from 'rxjs/operators';
import { StepSelectedEvent } from '@indice/ng-components/lib/controls/stepper/types/step-selected-event';
import * as Handlebars from 'handlebars/dist/cjs/handlebars';
import { CampaignBasicInfoComponent } from '../steps/basic-info/campaign-basic-info.component';
import { CreateCampaignRequest, MessagesApiClient, MessageChannelKind, Period, Hyperlink, Campaign, DistributionListResultSet, Template, MessageContent } from 'src/app/core/services/messages-api.service';
import { UtilitiesService } from 'src/app/shared/utilities.service';
import { ValidationService } from 'src/app/core/services/validation.service';

@Component({
    selector: 'app-campaign-create',
    templateUrl: './campaign-create.component.html'
})
export class CampaignCreateComponent implements OnInit, AfterViewChecked {
    @ViewChild('createCampaignStepper', { static: true }) private _stepper!: LibStepperComponent;
    @ViewChild('basicInfoStep', { static: true }) private _basicInfoStep!: CampaignBasicInfoComponent;
    @ViewChild('tabGroup', { static: true }) private _tabGroup!: LibTabGroupComponent;
    private _currentValidDataObject: any = undefined;
    private _contentStepInitialized: boolean = false;
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

    constructor(
        private _api: MessagesApiClient,
        private _router: Router,
        private _changeDetector: ChangeDetectorRef,
        private _validationService: ValidationService,
        private _utilities: UtilitiesService,
        @Inject(ToasterService) private _toaster: ToasterService
    ) { }

    public now: Date = new Date();
    public basicDetailsForm!: UntypedFormGroup;
    public contentForm!: UntypedFormGroup;
    public recipientsForm!: UntypedFormGroup;
    public previewForm!: UntypedFormGroup;
    public distributionLists: MenuOption[] = [new MenuOption('Παρακαλώ επιλέξτε...', null)];
    public submitInProgress = false;
    public subjectPreview: string = '';
    public bodyPreview: string = '';
    public get data(): AbstractControl { return this.contentForm.get('data')!; }
    public get sendVia(): AbstractControl { return this.recipientsForm.get('sendVia')!; }
    public get distributionList(): AbstractControl { return this.recipientsForm.get('distributionList')!; }
    public get recipientIds(): AbstractControl { return this.recipientsForm.get('recipientIds')!; }
    public get published(): AbstractControl { return this.previewForm.get('published')!; }
    public get inboxBody(): AbstractControl { return this.contentForm.get('inboxBody')!; }
    public get inboxSubject(): AbstractControl { return this.contentForm.get('inboxSubject')!; }
    public get emailBody(): AbstractControl { return this.contentForm.get('emailBody')!; }
    public get emailSubject(): AbstractControl { return this.contentForm.get('emailSubject')!; }
    public get pushNotificationBody(): AbstractControl { return this.contentForm.get('pushNotificationBody')!; }
    public get pushNotificationSubject(): AbstractControl { return this.contentForm.get('pushNotificationSubject')!; }
    public get smsBody(): AbstractControl { return this.contentForm.get('smsBody')!; }
    public get smsSubject(): AbstractControl { return this.contentForm.get('smsSubject')!; }
    public showInboxTab = false;
    public showPushNotificationTab = false;
    public showEmailTab = false;
    public showSmsTab = false;
    public StepperType = StepperType;
    public MessageChannelKind = MessageChannelKind;

    public get okLabel(): string {
        return this._stepper.currentStep?.isLast
            ? this.published.value === true
                ? 'Αποθήκευση & Δημοσίευση'
                : 'Αποθήκευση'
            : 'Επόμενο';
    }

    public get recipientsCount(): number {
        return this.recipientIds.value?.split('\n').filter((x: string) => x !== '').length || 0;
    }

    public get samplePayload(): any {
        return {
            ...this._samplePayload,
            data: this._currentValidDataObject ? { ...this._currentValidDataObject } : null,
            actionLink: {
                href: this._basicInfoStep.actionLinkHref.value || null,
                text: this._basicInfoStep.actionLinkText.value || null
            },
            title: this._basicInfoStep.title.value,
            type: this._basicInfoStep.type.value?.text || null
        };
    }

    public ngOnInit(): void {
        this._initForms();
    }

    public ngAfterViewChecked(): void {
        this._changeDetector.detectChanges();
    }

    public onSubmitCampaign(): void {
        const isLastStep = this._stepper.currentStep?.isLast;
        if (!isLastStep) {
            this._stepper.goToNextStep();
            return;
        }
        this.submitInProgress = true;
        const data = this._prepareDataToSubmit();
        this._api
            .createCampaign(data)
            .subscribe({
                next: (campaign: Campaign) => {
                    this.submitInProgress = false;
                    this._router.navigate(['campaigns']);
                    this._toaster.show(ToastType.Success, 'Επιτυχής αποθήκευση', `Η καμπάνια με τίτλο '${campaign.title}' δημιουργήθηκε με επιτυχία.`);
                }
            });
    }

    private _prepareDataToSubmit(): CreateCampaignRequest {
        const data = new CreateCampaignRequest({
            actionLink: new Hyperlink({
                href: this._basicInfoStep.actionLinkHref.value,
                text: this._basicInfoStep.actionLinkText.value
            }),
            activePeriod: new Period({
                from: this._basicInfoStep.from.value ? new Date(this._basicInfoStep.from.value) : undefined,
                to: this._basicInfoStep.to.value ? new Date(this._basicInfoStep.to.value) : undefined
            }),
            isGlobal: this.sendVia.value === 'user-base',
            messageChannelKind: this._basicInfoStep.channels.value,
            published: this.published.value,
            title: this._basicInfoStep.title.value,
            data: this.data.value,
            typeId: this._basicInfoStep.type.value?.value || undefined,
            recipientIds: this.recipientIds.value ? this.recipientIds.value.split('\n') : null,
            recipientListId: this.distributionList.value?.value || undefined,
            content: {}
        });
        for (const channel of this._basicInfoStep.channels.value) {
            switch (channel) {
                case MessageChannelKind.Inbox:
                    data.content![channel] = new MessageContent({ title: this.inboxSubject.value, body: this.inboxBody.value });
                    break;
                case MessageChannelKind.Email:
                    data.content![channel] = new MessageContent({ title: this.emailSubject.value, body: this.emailBody.value });
                    break;
                case MessageChannelKind.PushNotification:
                    data.content![channel] = new MessageContent({ title: this.pushNotificationSubject.value, body: this.pushNotificationBody.value });
                    break;
                case MessageChannelKind.SMS:
                    data.content![channel] = new MessageContent({ title: this.smsSubject.value, body: this.smsBody.value });
                    break;
            }
        }
        return data;
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

    public onStepperStepChanged(event: StepSelectedEvent) {
        if (event.selectedIndex === 1) {
            this._initSecondStep();
        }
        if (event.selectedIndex === 2 && this.distributionLists.length <= 1) {
            this._loadDistributionLists();
        }
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

    public toRecipientIdsArray(recipientIds: string | undefined): string[] {
        return recipientIds ? [...new Set(recipientIds.split('\n').filter(x => x !== ''))] : [];
    }

    public toRecipientIdsString(recipientIds: string[] | undefined): string {
        return recipientIds ? recipientIds.join('\n') : '';
    }

    private _initForms(): void {
        this.contentForm = new UntypedFormGroup({
            data: new UntypedFormControl(undefined, this._validationService.invalidJsonValidator()),
            emailSubject: new UntypedFormControl(''),
            emailBody: new UntypedFormControl(''),
            inboxSubject: new UntypedFormControl(''),
            inboxBody: new UntypedFormControl(''),
            smsSubject: new UntypedFormControl(''),
            smsBody: new UntypedFormControl(''),
            pushNotificationSubject: new UntypedFormControl(''),
            pushNotificationBody: new UntypedFormControl('')
        });
        this.recipientsForm = new UntypedFormGroup({
            sendVia: new UntypedFormControl('distribution-list'),
            distributionList: new UntypedFormControl(undefined, [Validators.required]),
            recipientIds: new UntypedFormControl()
        });
        this.previewForm = new UntypedFormGroup({
            published: new UntypedFormControl(false)
        })
    }

    private _initSecondStep(): void {
        const selectedTemplate = this._basicInfoStep.template.value;
        this._resetTabs();
        this._resetContentValidators();
        const showInboxTab = this._basicInfoStep.channelsArray.find(x => x.value === MessageChannelKind.Inbox)!.checked;
        const showPushNotificationTab = this._basicInfoStep.channelsArray.find(x => x.value === MessageChannelKind.PushNotification)!.checked;
        const showSmsTab = this._basicInfoStep.channelsArray.find(x => x.value === MessageChannelKind.SMS)!.checked;
        const showEmailTab = this._basicInfoStep.channelsArray.find(x => x.value === MessageChannelKind.Email)!.checked;
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
        if (this._contentStepInitialized) {
            return;
        }
        this._contentStepInitialized = true;
        if (selectedTemplate) {
            this._api.getTemplateById(selectedTemplate.value).subscribe((template: Template) => {
                this._setContent(template);
            });
        }
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
}
