import { Component, Inject, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ToasterService, ToastType } from '@indice/ng-components';

import { CampaignDetails, Hyperlink, MessageContent } from 'src/app/core/services/messages-api.service';
import { CampaignContentComponent } from '../../create/steps/content/campaign-content.component';
import { CampaignEditStore } from '../campaign-edit-store.service';

@Component({
    selector: 'app-campaign-content-edit',
    templateUrl: './campaign-edit-content.component.html'
})
export class CampaignContentEditComponent implements OnInit {
    @ViewChild('contentStep', { static: false }) public _contentComponent: CampaignContentComponent | undefined;
    private _campaignId: string = '';

    constructor(
        private _campaignStore: CampaignEditStore,
        private _activatedRoute: ActivatedRoute,
        @Inject(ToasterService) private _toaster: ToasterService
    ) { }

    public basicInfoData: any = {};
    public campaign = new CampaignDetails();
    public campaignData: any;
    public content: { [key: string]: MessageContent; } | undefined = undefined;
    public updateInProgress = false;

    public ngOnInit(): void {
        this._campaignId = this._activatedRoute.parent?.snapshot.params['campaignId'];
        if (this._campaignId) {
            this._campaignStore.getCampaign(this._campaignId!).subscribe((campaign: CampaignDetails) => {
                this.campaign = campaign;
                this.basicInfoData.title = campaign.title;
                this.basicInfoData.type = campaign.type?.name;
                this.basicInfoData.actionLink = new Hyperlink({
                    text: campaign.actionLink?.text ?? "Click me",
                    href: campaign.actionLink?.href ?? "https://www.indice.gr"
                });
                if (this.campaign.mediaBaseHref) {
                    this.basicInfoData.mediaBaseHref = this.campaign.mediaBaseHref;
                }
                this.campaignData = campaign.data;
                this.content = campaign.content;
            });
        }
    }

    public updateContent(): void {
        this.updateInProgress = true;
        const formContents = this._contentComponent?.form.controls.content.value;
        let content: { [key: string]: MessageContent; } = {};
        for (const item of formContents) {
            content[item.channel] = new MessageContent({
                title: item.subject,
                sender: item.sender,
                body: item.body
            })
        }
        this.campaign.mediaBaseHref = this._contentComponent?.additionalData?.mediaBaseHref;
        this.campaign.content = content;
        const data = this._contentComponent?.form.controls.data.value;
        this.campaign.data = data ? data : null;
        this._campaignStore
            .updateCampaign(this._campaignId, this.campaign)
            .subscribe(_ => {
                this.updateInProgress = false;
                this._toaster.show(ToastType.Success, 'Επιτυχής ενημέρωση', `Το περιεχόμενο της καμπάνιας με τίτλο '${this.campaign.title}' ενημερώθηκε με επιτυχία.`);
            });
    }
}
