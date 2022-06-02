import { AfterViewInit, ChangeDetectorRef, Component, Inject, OnInit, ViewChild } from '@angular/core';
import { DatePipe, DOCUMENT } from '@angular/common';
import { AbstractControl, FormArray, FormControl, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';

import { map } from 'rxjs/operators';
import { MenuOption, ToasterService, ToastType } from '@indice/ng-components';
import { CreateCampaignRequest, MessagesApiClient, MessageChannelKind, MessageTypeResultSet, Period, Hyperlink, Campaign, DistributionListResultSet, TemplateListItemResultSet, PreviewItem, PreviewItemResult } from 'src/app/core/services/messages-api.service';
import { LibStepperComponent } from 'src/app/shared/components/stepper/lib-stepper.component';
import { StepperType } from 'src/app/shared/components/stepper/types/stepper-type';
import { StepSelectedEvent } from 'src/app/shared/components/stepper/types/step-selected-event';
import { UtilitiesService } from 'src/app/shared/utilities.service';
import { ValidationService } from 'src/app/core/services/validation.service';

@Component({
    selector: 'app-campaign-create',
    templateUrl: './campaign-create.component.html'
})
export class CampaignCreateComponent implements OnInit, AfterViewInit {
    constructor(
        private _api: MessagesApiClient,
        private _router: Router,
        private _changeDetector: ChangeDetectorRef,
        private _datePipe: DatePipe,
        private _validationService: ValidationService,
        private _utilities: UtilitiesService,
        @Inject(ToasterService) private _toaster: ToasterService,
        @Inject(DOCUMENT) private _document: Document
    ) { }

    private _currentValidDataObject: any = undefined;
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
    public now: Date = new Date();
    public basicDetailsForm!: FormGroup;
    public contentForm!: FormGroup;
    public recipientsForm!: FormGroup;
    public previewForm!: FormGroup;
    public messageTypes: MenuOption[] = [new MenuOption('Παρακαλώ επιλέξτε...', null)];
    public templates: MenuOption[] = [new MenuOption('Παρακαλώ επιλέξτε...', null)];
    public distributionLists: MenuOption[] = [new MenuOption('Παρακαλώ επιλέξτε...', null)];
    public submitInProgress = false;
    public bodyPreview: string | undefined;
    public get title(): AbstractControl { return this.basicDetailsForm.get('title')!; }
    public get from(): AbstractControl { return this.basicDetailsForm.get('from')!; }
    public get to(): AbstractControl { return this.basicDetailsForm.get('to')!; }
    public get actionLinkText(): AbstractControl { return this.basicDetailsForm.get('actionLinkText')!; }
    public get actionLinkHref(): AbstractControl { return this.basicDetailsForm.get('actionLinkHref')!; }
    public get type(): AbstractControl { return this.basicDetailsForm.get('type')!; }
    public get template(): AbstractControl { return this.basicDetailsForm.get('template')!; }
    public get needsTemplate(): AbstractControl { return this.basicDetailsForm.get('needsTemplate')!; }
    public get channels(): AbstractControl { return this.basicDetailsForm.get('channels')!; }
    public get data(): AbstractControl { return this.contentForm.get('data')!; }
    public get sendVia(): AbstractControl { return this.recipientsForm.get('sendVia')!; }
    public get distributionList(): AbstractControl { return this.recipientsForm.get('distributionList')!; }
    public get recipientIds(): AbstractControl { return this.recipientsForm.get('recipientIds')!; }
    public get published(): AbstractControl { return this.previewForm.get('published')!; }
    public get emailBody(): AbstractControl { return this.contentForm.get('emailBody')!; }
    @ViewChild('createCampaignStepper', { static: true }) private _stepper!: LibStepperComponent;
    public StepperType = StepperType;
    public channelsArray = [
        { name: 'Inbox', description: 'Ειδοποίηση μέσω πρoσωπικού μήνυμα.', value: MessageChannelKind.Inbox, checked: true },
        { name: 'Push Notification', description: 'Ειδοποίηση μέσω push notification στις εγγεγραμμένες συσκευές.', value: MessageChannelKind.PushNotification, checked: false },
        { name: 'Email', description: 'Ειδοποίηση μέσω ηλεκτρονικού ταχυδρομείου', value: MessageChannelKind.Email, checked: false },
        { name: 'SMS', description: 'Ειδοποίηση μέσω σύντομου γραπτού μηνύματος.', value: MessageChannelKind.SMS, checked: false }
    ];

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
            data: this._currentValidDataObject ? { ...this._currentValidDataObject } : null
        };
    }

    public ngOnInit(): void {
        setTimeout(() => {
            const sidePane = this._document.getElementsByClassName('side-pane-box-size')[0] as HTMLElement;
            sidePane.style.maxWidth = '84rem';
        }, 0);
        this._initForms();
        this._loadMessageTypes();
    }

    public ngAfterViewInit(): void {
        this._changeDetector.detectChanges();
    }

    public onSideViewOk(): void {
        const isLastStep = this._stepper.currentStep?.isLast;
        if (!isLastStep) {
            this._stepper.goToNextStep();
            return;
        }
        this.submitInProgress = true;
        const data = new CreateCampaignRequest({
            actionLink: new Hyperlink({
                href: this.actionLinkHref.value,
                text: this.actionLinkText.value
            }),
            activePeriod: new Period({
                from: this.from.value,
                to: this.to.value
            }),
            isGlobal: true,
            messageChannelKind: this.channels.value,
            published: false,
            templateId: undefined,
            title: this.title.value,
            data: this.data.value
        });
        debugger
        this._api
            .createCampaign(data)
            .subscribe({
                next: (campaign: Campaign) => {
                    this.submitInProgress = false;
                    this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['campaigns']));
                    this._toaster.show(ToastType.Success, 'Επιτυχής αποθήκευση', `Η καμπάνια με τίτλο '${campaign.title}' δημιουργήθηκε με επιτυχία.`);
                }
            });
    }

    public onCampaignStartInput(event: any): void {
        this.from.setValue(this._datePipe.transform(event.target.value, 'yyyy-MM-ddThh:mm'));
    }

    public onEmailBodyInput(event: any): void {
        const value = event.target.value;
        this.emailBody.setValue(value);
        const emailData = new PreviewItem({
            code: '1',
            text: value,
            data: this.samplePayload
        });
        this._api.previewCampaign([emailData]).subscribe((results: PreviewItemResult[]) => {
            this.bodyPreview = results[0].text;
        });
    }

    public onCampaignEndInput(event: any): void {
        this.to.setValue(this._datePipe.transform(event.target.value, 'yyyy-MM-ddThh:mm'));
    }

    public onCampaignMetadataInput(event: any): void {
        const value = event.target.value;
        if (this._utilities.isValidJson(value)) {
            this._currentValidDataObject = JSON.parse(value);
        }
    }

    public onStepperStepChanged(event: StepSelectedEvent) {
        if (event.selectedIndex === 2 && this.distributionLists.length <= 1) {
            this._loadDistributionLists();
        }
        if (event.selectedIndex == 3) {
            const x = { ...this.basicDetailsForm.value, ...this.contentForm.value, ...this.recipientsForm.value };
        }
    }

    public onNeedsTemplateChanged(event: any): void {
        const value = event.target.value;
        if (value === 'yes') {
            if (this.templates.length <= 1) {
                this._loadTemplates();
            }
            this.template.setValidators(Validators.required);
        } else {
            this.template.removeValidators(Validators.required);
            this.template.setValue(null);
        }
        this.template.updateValueAndValidity();
        this.needsTemplate.setValue(value);
    }

    public onInfoIconClicked() { }

    public onChannelCheckboxChange(event: any): void {
        const channelsFormArray: FormArray = this.channels as FormArray;
        const value = event.target.value;
        const checkbox = this.channelsArray.find(x => x.value === value);
        if (event.target.checked) {
            channelsFormArray.push(new FormControl(value));
            checkbox!.checked = true;
        } else {
            let i: number = 0;
            channelsFormArray.controls.forEach((control: AbstractControl) => {
                if (control.value == value) {
                    channelsFormArray.removeAt(i);
                    checkbox!.checked = false;
                    return;
                }
                i++;
            });
        }
    }

    public onSendViaChanged(event: any): void {
        const value = event.target.value;
        if (value === 'distribution-list') {
            this.distributionList.setValidators(Validators.required);
            this.recipientIds.removeValidators(Validators.required);
            this.recipientIds.setValue(null);
        } else {
            this.recipientIds.setValidators(Validators.required);
            this.distributionList.removeValidators(Validators.required);
            this.distributionList.setValue(null);
        }
        this.distributionList.updateValueAndValidity();
        this.recipientIds.updateValueAndValidity();
        this.sendVia.setValue(value);
    }

    public onTemplateSelectionChanged(event: any): void {
        if (event.value) {
            this.template.setValue(event);
            const channelsFormArray: FormArray = this.channels as FormArray;
            channelsFormArray.clear();
            event.data.forEach((channel: string) => channelsFormArray.push(new FormControl(channel)));
            this.channelsArray.forEach((channel: any) => channel.checked = this.channels.value.indexOf(channel.value) > -1);
        } else {
            this.template.setValue(null);
        }
    }

    public toRecipientIdsArray(recipientIds: string | undefined): string[] {
        return recipientIds ? [...new Set(recipientIds.split('\n').filter(x => x !== ''))] : [];
    }

    public toRecipientIdsString(recipientIds: string[] | undefined): string {
        return recipientIds ? recipientIds.join('\n') : '';
    }

    private _loadMessageTypes(): void {
        this._api
            .getMessageTypes()
            .pipe(map((messageTypes: MessageTypeResultSet) => {
                if (messageTypes.items) {
                    this.messageTypes.push(...messageTypes.items.map(type => new MenuOption(type.name || '', type.id)));
                }
            }))
            .subscribe();
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

    private _loadTemplates(): void {
        this._api
            .getTemplates()
            .pipe(map((templates: TemplateListItemResultSet) => {
                if (templates.items) {
                    this.templates.push(...templates.items.map(template => new MenuOption(template.name || '', template.id, undefined, template.channels)))
                }
            }))
            .subscribe();
    }

    private _initForms(): void {
        this.basicDetailsForm = new FormGroup({
            title: new FormControl(undefined, [
                Validators.required,
                Validators.maxLength(128)
            ]),
            from: new FormControl(this._datePipe.transform(this.now, 'yyyy-MM-ddThh:mm')),
            to: new FormControl(),
            actionLinkText: new FormControl(undefined, [Validators.maxLength(128)]),
            actionLinkHref: new FormControl(undefined, [
                Validators.pattern(/^(?:http(s)?:\/\/)?[\w.-]+(?:\.[\w\.-]+)+[\w\-\._~:?#[\]@!\$&'\(\)\*\+,;=.]+$/),
                Validators.maxLength(2048)
            ]),
            type: new FormControl(),
            template: new FormControl(),
            needsTemplate: new FormControl('no'),
            channels: new FormArray([new FormControl('Inbox')], [Validators.required])
        });
        this.contentForm = new FormGroup({
            data: new FormControl(undefined, this._validationService.invalidJsonValidator()),
            emailSubject: new FormControl(''),
            emailBody: new FormControl('')
        });
        this.recipientsForm = new FormGroup({
            sendVia: new FormControl('distribution-list'),
            distributionList: new FormControl(undefined, [Validators.required]),
            recipientIds: new FormControl()
        });
        this.previewForm = new FormGroup({
            published: new FormControl(false)
        })
    }
}
