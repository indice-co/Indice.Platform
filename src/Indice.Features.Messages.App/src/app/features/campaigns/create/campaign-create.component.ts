import { AfterViewInit, ChangeDetectorRef, Component, Inject, OnInit, ViewChild } from '@angular/core';
import { DatePipe, DOCUMENT } from '@angular/common';
import { AbstractControl, FormControl, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';

import { MenuOption, ToasterService, ToastType } from '@indice/ng-components';
import { map } from 'rxjs/operators';
import { CreateCampaignRequest, MessagesApiClient, MessageChannelKind, MessageTypeResultSet, Period, Hyperlink, TemplateResultSet, Campaign, DistributionListResultSet } from 'src/app/core/services/messages-api.service';
import { invalidJsonValidator } from 'src/app/shared/validators/jsonValidator';
import { LibStepperComponent } from 'src/app/shared/components/stepper/lib-stepper.component';
import { StepperType } from 'src/app/shared/components/stepper/types/stepper-type';
import { StepSelectedEvent } from 'src/app/shared/components/stepper/types/step-selected-event';

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
        @Inject(ToasterService) private _toaster: ToasterService,
        @Inject(DOCUMENT) private _document: Document
    ) { }


    public now: Date = new Date();
    public basicDetailsForm!: FormGroup;
    public contentForm!: FormGroup;
    public recipientsForm!: FormGroup;
    public previewForm!: FormGroup;
    public messageTypes: MenuOption[] = [new MenuOption('Παρακαλώ επιλέξτε...', null)];
    public templates: MenuOption[] = [new MenuOption('Παρακαλώ επιλέξτε...', null)];
    public distributionLists: MenuOption[] = [new MenuOption('Παρακαλώ επιλέξτε...', null)];
    public submitInProgress = false;
    public get title(): AbstractControl { return this.basicDetailsForm.get('title')!; }
    public get from(): AbstractControl { return this.basicDetailsForm.get('from')!; }
    public get to(): AbstractControl { return this.basicDetailsForm.get('to')!; }
    public get actionLinkText(): AbstractControl { return this.basicDetailsForm.get('actionLinkText')!; }
    public get actionLinkHref(): AbstractControl { return this.basicDetailsForm.get('actionLinkHref')!; }
    public get type(): AbstractControl { return this.basicDetailsForm.get('type')!; }
    public get template(): AbstractControl { return this.basicDetailsForm.get('template')!; }
    public get needsTemplate(): AbstractControl { return this.basicDetailsForm.get('needsTemplate')!; }
    public get data(): AbstractControl { return this.contentForm.get('data')!; }
    public get sendVia(): AbstractControl { return this.recipientsForm.get('sendVia')!; }
    public get distributionList(): AbstractControl { return this.recipientsForm.get('distributionList')!; }
    public get recipientIds(): AbstractControl { return this.recipientsForm.get('recipientIds')!; }
    public get published(): AbstractControl { return this.previewForm.get('published')!; }
    @ViewChild('createCampaignStepper', { static: true }) private _stepper!: LibStepperComponent;
    public StepperType = StepperType;

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
            messageChannelKind: [MessageChannelKind.Inbox],
            published: false,
            templateId: undefined,
            title: this.title.value,
            data: this.data.value
        });
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

    public onCampaignEndInput(event: any): void {
        this.to.setValue(this._datePipe.transform(event.target.value, 'yyyy-MM-ddThh:mm'));
    }

    public onStepperStepChanged(event: StepSelectedEvent) {
        if (event.selectedIndex === 2 && this.distributionLists.length <= 1) {
            this._loadDistributionList();
        }
        if (event.selectedIndex == 3) {
            const x = { ...this.basicDetailsForm.value, ...this.contentForm.value, ...this.recipientsForm.value };
            // debugger;
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

    private _loadDistributionList(): void {
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
            .pipe(map((templates: TemplateResultSet) => {
                if (templates.items) {
                    this.templates.push(...templates.items.map(template => new MenuOption(template.name || '', template.id)))
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
            needsTemplate: new FormControl('no')
        });
        this.contentForm = new FormGroup({
            data: new FormControl(undefined, invalidJsonValidator())
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
