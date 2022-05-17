import { AfterViewInit, ChangeDetectorRef, Component, Inject, OnInit, ViewChild } from '@angular/core';
import { DatePipe, DOCUMENT } from '@angular/common';
import { AbstractControl, FormControl, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';

import { MenuOption, ToasterService, ToastType } from '@indice/ng-components';
import { map } from 'rxjs/operators';
import { CreateCampaignRequest, MessagesApiClient, MessageChannelKind, MessageTypeResultSet, Period, Hyperlink, TemplateResultSet, Campaign, ValidationProblemDetails } from 'src/app/core/services/messages-api.service';
import { invalidJsonValidator } from 'src/app/shared/validators/jsonValidator';
import { UtilitiesService } from 'src/app/shared/utilities.service';
import { LibStepperComponent } from 'src/app/shared/components/stepper/lib-stepper.component';

@Component({
    selector: 'app-campaign-create',
    templateUrl: './campaign-create.component.html'
})
export class CampaignCreateComponent implements OnInit, AfterViewInit {
    @ViewChild('createCampaignStepper', { static: true }) public _stepper!: LibStepperComponent;

    constructor(
        private _api: MessagesApiClient,
        private _router: Router,
        private _utilities: UtilitiesService,
        private _changeDetector: ChangeDetectorRef,
        private _datePipe: DatePipe,
        @Inject(ToasterService) private _toaster: ToasterService,
        @Inject(DOCUMENT) private _document: Document
    ) { }

    public now: Date = new Date();
    public basicDetailsForm!: FormGroup;
    public contentForm!: FormGroup;
    public messageTypes: MenuOption[] = [new MenuOption('Παρακαλώ επιλέξτε...', null)];
    public templates: MenuOption[] = [new MenuOption('Παρακαλώ επιλέξτε...', null)];
    public submitInProgress = false;

    public get title(): AbstractControl {
        return this.basicDetailsForm.get('title')!;
    }

    public get from(): AbstractControl {
        return this.basicDetailsForm.get('from')!;
    }

    public get to(): AbstractControl {
        return this.basicDetailsForm.get('to')!;
    }

    public get actionLinkText(): AbstractControl {
        return this.basicDetailsForm.get('actionLinkText')!;
    }

    public get actionLinkHref(): AbstractControl {
        return this.basicDetailsForm.get('actionLinkHref')!;
    }

    public get typeId(): AbstractControl {
        return this.basicDetailsForm.get('typeId')!;
    }

    public get templateId(): AbstractControl {
        return this.basicDetailsForm.get('templateId')!;
    }

    public get needsTemplate(): AbstractControl {
        return this.basicDetailsForm.get('needsTemplate')!;
    }

    public get data(): AbstractControl {
        return this.contentForm.get('data')!;
    }

    public get okLabel(): string {
        return this._stepper.currentStep?.isLast ? 'Αποθήκευση' : 'Επόμενο';
    }

    public ngOnInit(): void {
        setTimeout(() => {
            const sidePane = this._document.getElementsByClassName('side-pane-box-size')[0] as HTMLElement;
            sidePane.style.maxWidth = '84rem';
        }, 0);
        this._initFormGroups();
        this._loadMessageTypes();
        this._loadTemplates();
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
                    // This is to force reload campaigns page when a new campaign is successfully saved. 
                    this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['campaigns']));
                    this._toaster.show(ToastType.Success, 'Επιτυχής αποθήκευση', `Η καμπάνια με τίτλο '${campaign.title}' δημιουργήθηκε με επιτυχία.`);
                },
                error: (problemDetails: ValidationProblemDetails) => {
                    this._toaster.show(ToastType.Error, 'Αποτυχής αποθήκευση', `${this._utilities.getValidationProblemDetails(problemDetails)}`, 6000);
                }
            });
    }

    public onCampaignStartInput(event: any): void {
        this.from.setValue(this._datePipe.transform(event.target.value, 'yyyy-MM-ddThh:mm'));
    }

    public onCampaignEndInput(event: any): void {
        this.to.setValue(this._datePipe.transform(event.target.value, 'yyyy-MM-ddThh:mm'));
    }

    public onNeedsTemplateChanged(event: any): void {
        const value = event.target.value;
        if (value === 'yes') {
            this.templateId.setValidators(Validators.required);
        } else {
            this.templateId.removeValidators(Validators.required);
        }
        this.templateId.updateValueAndValidity();
        this.needsTemplate.setValue(value);
    }

    public onTemplateSelectedChanged(event: MenuOption): void {
        this.templateId.setValue(event.value !== null ? event : null);
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

    private _initFormGroups(): void {
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
            typeId: new FormControl(),
            templateId: new FormControl(),
            needsTemplate: new FormControl('no')
        });
        this.contentForm = new FormGroup({
            data: new FormControl(undefined, invalidJsonValidator())
        });
    }
}
