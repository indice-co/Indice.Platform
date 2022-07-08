import { AfterViewChecked, ChangeDetectorRef, Component, Inject, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { HeaderMetaItem, Icons, LibStepperComponent, StepperType, ToasterService, ToastType } from '@indice/ng-components';
import { StepSelectedEvent } from '@indice/ng-components/lib/controls/stepper/types/step-selected-event';
import { CampaignBasicInfoComponent } from '../steps/basic-info/campaign-basic-info.component';
import { CampaignContentComponent } from '../steps/content/campaign-content.component';
import { CampaignPreview } from '../steps/preview/campaign-preview';
import { CampaignDetails, Hyperlink, MessageChannelKind, MessageContent, MessagesApiClient, Period, UpdateCampaignRequest } from 'src/app/core/services/messages-api.service';

@Component({
    selector: 'app-campaign-edit',
    templateUrl: './campaign-edit.component.html'
})
export class CampaignEditComponent implements OnInit, AfterViewChecked {
    @ViewChild('updateCampaignStepper', { static: true }) private _stepper!: LibStepperComponent;
    @ViewChild('basicInfoStep', { static: true }) private _basicInfoStep!: CampaignBasicInfoComponent;
    @ViewChild('contentStep', { static: true }) private _contentStep!: CampaignContentComponent;
    private _campaignId?: string;

    constructor(
        private _activatedRoute: ActivatedRoute,
        private _api: MessagesApiClient,
        private _router: Router,
        private _changeDetector: ChangeDetectorRef,
        @Inject(ToasterService) private _toaster: ToasterService
    ) { }

    public templateId: string | undefined;
    public basicInfoData: any = {};
    public previewData = new CampaignPreview();
    public submitInProgress = false;
    public StepperType = StepperType;
    public campaign: CampaignDetails = new CampaignDetails();

    public primary = [
        { text: 'Πληροφορίες δικτύου', link: 'display-settings' },
        { text: 'Σταθμοί φόρτισης', link: 'charge-points' },
        { text: 'Φορτίσεις', link: 'charge-sessions' },
    ];

    public secondary = [];
    public metaItems: HeaderMetaItem[] = [];

    public ngOnInit(): void {
        this._campaignId = this._activatedRoute.snapshot.params['campaignId'];
        if (this._campaignId) {
            this._api.getCampaignById(this._campaignId).subscribe((campaign: CampaignDetails) => {
                this.campaign = campaign;
                if (campaign.published) {
                    this.metaItems.push({ key: 'status', icon: Icons.Heart, text: `Δημοσιεύτηκε στις ${new Date()}` });
                } else {
                    this.metaItems.push({ key: 'status', icon: Icons.HeartBroken, text: `Μη δημοσιευμένη` });
                }
            });
        } else {
        }
    }

    public ngAfterViewChecked(): void {
        this._changeDetector.detectChanges();
    }

    public onUpdateCampaign(): void {
        const isLastStep = this._stepper.currentStep?.isLast;
        if (!isLastStep) {
            this._stepper.goToNextStep();
            return;
        }
        this.submitInProgress = true;
        const data = this._prepareDataToSubmit();
        this._api
            .updateCampaign(this._campaignId!, data)
            .subscribe({
                next: () => {
                    this.submitInProgress = false;
                    this._router.navigate(['campaigns']);
                    this._toaster.show(ToastType.Success, 'Επιτυχής αποθήκευση', `Η καμπάνια με τίτλο '${this.campaign!.title}' δημιουργήθηκε με επιτυχία.`);
                }
            });
    }

    public onStepperStepChanged(event: StepSelectedEvent) {
        if (event.selectedIndex === 1) {
            this._contentStep.initStep(this._basicInfoStep.channelsState, this.templateId);
        }
        this.previewData.title = this._basicInfoStep.title.value;
        this.previewData.type = this._basicInfoStep.type.value?.text;
        this.previewData.template = this._basicInfoStep.template.value?.text;
        this.previewData.distributionList = this.campaign!.distributionList?.name;
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

    private _prepareDataToSubmit(): UpdateCampaignRequest {
        const data = new UpdateCampaignRequest({
            actionLink: new Hyperlink({
                href: this._basicInfoStep.actionLinkHref.value,
                text: this._basicInfoStep.actionLinkText.value
            }),
            activePeriod: new Period({
                from: this._basicInfoStep.from.value ? new Date(this._basicInfoStep.from.value) : undefined,
                to: this._basicInfoStep.to.value ? new Date(this._basicInfoStep.to.value) : undefined
            }),
            title: this._basicInfoStep.title.value,
            data: this._contentStep.data.value,
            typeId: this._basicInfoStep.type.value?.value || undefined,
            content: {}
        });
        for (const channel of this._basicInfoStep.channels.value) {
            switch (channel) {
                case MessageChannelKind.Inbox:
                    data.content![channel] = new MessageContent({ title: this._contentStep.inboxSubject.value, body: this._contentStep.inboxBody.value });
                    break;
                case MessageChannelKind.Email:
                    data.content![channel] = new MessageContent({ title: this._contentStep.emailSubject.value, body: this._contentStep.emailBody.value });
                    break;
                case MessageChannelKind.PushNotification:
                    data.content![channel] = new MessageContent({ title: this._contentStep.pushNotificationSubject.value, body: this._contentStep.pushNotificationBody.value });
                    break;
                case MessageChannelKind.SMS:
                    data.content![channel] = new MessageContent({ title: this._contentStep.smsSubject.value, body: this._contentStep.smsBody.value });
                    break;
            }
        }
        return data;
    }
}