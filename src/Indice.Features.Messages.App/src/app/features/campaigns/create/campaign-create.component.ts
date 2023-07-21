import { AfterViewChecked, ChangeDetectorRef, Component, Inject, OnInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';

import { HeaderMetaItem, Icons, LibStepperComponent, StepperType, ToasterService, ToastType } from '@indice/ng-components';
import { StepSelectedEvent } from '@indice/ng-components/lib/controls/stepper/types/step-selected-event';
import { CampaignBasicInfoComponent } from './steps/basic-info/campaign-basic-info.component';
import { CampaignContentComponent } from './steps/content/campaign-content.component';
import { CampaignPreview } from './steps/preview/campaign-preview';
import { CampaignPreviewComponent } from './steps/preview/campaign-preview.component';
import { CampaignRecipientsComponent } from './steps/recipients/campaign-recipients.component';
import { CreateCampaignRequest, MessagesApiClient, Period, Hyperlink, Campaign, MessageContent, Template, AttachmentLink } from 'src/app/core/services/messages-api.service';
import { CampaignAttachmentsComponent } from './steps/attachments/campaign-attachments.component';
import { map, mergeMap } from 'rxjs/operators';
import { of } from 'rxjs';

@Component({
    selector: 'app-campaign-create',
    templateUrl: './campaign-create.component.html'
})
export class CampaignCreateComponent implements OnInit, AfterViewChecked {
    @ViewChild('createCampaignStepper', { static: true }) private _stepper!: LibStepperComponent;
    @ViewChild('basicInfoStep', { static: true }) private _basicInfoStep!: CampaignBasicInfoComponent;
    @ViewChild('contentStep', { static: true }) private _contentStep!: CampaignContentComponent;
    @ViewChild('recipientsStep', { static: true }) private _recipientsStep!: CampaignRecipientsComponent;
    @ViewChild('previewStep', { static: true }) private _previewStep!: CampaignPreviewComponent;
    @ViewChild('attachmentsStep', { static: true }) private _attachmentsStep!: CampaignAttachmentsComponent;

    constructor(
        private _api: MessagesApiClient,
        private _router: Router,
        private _changeDetector: ChangeDetectorRef,
        @Inject(ToasterService) private _toaster: ToasterService
    ) { }

    public now: Date = new Date();
    public submitInProgress = false;
    public StepperType = StepperType;
    public previewData = new CampaignPreview();
    public basicInfoData: any = {};
    public templateId: string | undefined;
    public metaItems: HeaderMetaItem[] | null = [];
    public content: { [key: string]: MessageContent; } | undefined

    public get okLabel(): string {
        return this._stepper.currentStep?.isLast
            ? this._previewStep.published.value === true
                ? 'Αποθήκευση & Δημοσίευση'
                : 'Αποθήκευση'
            : 'Επόμενο';
    }

    public ngOnInit(): void {
        this.metaItems = [{
            key: 'info',
            icon: Icons.Details,
            text: 'Ακολουθήστε τα παρακάτω βήματα για να δημιουργήσετε ένα νέο campaign.'
        }];
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
            .pipe(mergeMap((campaign: Campaign) => {
                return this._attachmentsStep.attachment.value && campaign.id 
                    ? this._api.uploadCampaignAttachment(campaign.id, this._attachmentsStep.attachment.value).pipe(map(() => campaign))
                    : of(campaign)
            }))
            .subscribe({
                next: (campaign: Campaign) => {
                    this.submitInProgress = false;
                    this._router.navigate(['campaigns', campaign.id]);
                    this._toaster.show(ToastType.Success, 'Επιτυχής αποθήκευση', `Η καμπάνια με τίτλο '${campaign.title}' δημιουργήθηκε με επιτυχία.`);
                }
            });
    }

    public onStepperStepChanged(event: StepSelectedEvent) {
        if (event.selectedIndex === 1) {
            if (this.templateId) {
                this._api.getTemplateById(this.templateId).subscribe((template: Template) => {
                    this.content = template.content;
                });
            }
        }
        this.previewData.title = this._basicInfoStep.title.value;
        this.previewData.type = this._basicInfoStep.type.value?.text;
        this.previewData.template = this._basicInfoStep.template.value?.text;
        this.previewData.distributionList = this._recipientsStep.distributionList.value?.text;
        this.previewData.period = new Period({
            from: this._basicInfoStep.from.value,
            to: this._basicInfoStep.to.value
        });
        this.previewData.actionLink = new Hyperlink({
            text: this._basicInfoStep.actionLinkText.value,
            href: this._basicInfoStep.actionLinkHref.value
        });
        this.basicInfoData.title = this._basicInfoStep.title.value;
        this.basicInfoData.type = this._basicInfoStep.type.value?.text;
        this.basicInfoData.actionLink = new Hyperlink({
            text: this._basicInfoStep.actionLinkText.value,
            href: this._basicInfoStep.actionLinkHref.value
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
            isGlobal: this._recipientsStep.sendVia.value === 'user-base',
            published: this._previewStep.published.value,
            title: this._basicInfoStep.title.value,
            data: this._contentStep.data.value,
            typeId: this._basicInfoStep.type.value?.value || undefined,
            recipientIds: this._recipientsStep.recipientIds.value ? this._recipientsStep.recipientIds.value.split('\n') : null,
            recipientListId: this._recipientsStep.distributionList.value?.value || undefined,
            content: {}
        });
        const formContents = this._contentStep?.form.controls.content.value;
        let content: { [key: string]: MessageContent; } = {};
        for (const item of formContents) {
            content[item.channel] = new MessageContent({
                title: item.subject,
                body: item.body
            })
        }
        data.content = content;
        return data;
    }
}
