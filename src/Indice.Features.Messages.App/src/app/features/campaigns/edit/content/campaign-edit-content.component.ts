import { Component, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { CampaignDetails, Hyperlink, MessageChannelKind } from 'src/app/core/services/messages-api.service';
import { CampaignContentComponent } from '../../create/steps/content/campaign-content.component';
import { CampaignEditStore } from '../campaign-edit-store.service';

@Component({
    selector: 'app-campaign-content-edit',
    templateUrl: './campaign-edit-content.component.html'
})
export class CampaignContentEditComponent implements OnInit {
    private _campaignId: string | undefined;
    @ViewChild('contentStep', { static: true }) private _contentStep!: CampaignContentComponent;

    constructor(
        private _campaignStore: CampaignEditStore,
        private _activatedRoute: ActivatedRoute
    ) { }

    public basicInfoData: any = {};
    public campaign: CampaignDetails | undefined;

    public ngOnInit(): void {
        this._campaignId = this._activatedRoute.parent?.snapshot.params['campaignId'];
        if (this._campaignId) {
            this._campaignStore.getCampaign(this._campaignId!).subscribe((campaign: CampaignDetails) => {
                this.campaign = campaign;
                this.basicInfoData.title = campaign.title;
                this.basicInfoData.type = campaign.type?.name;
                this.basicInfoData.actionLink = new Hyperlink({
                    text: campaign.actionLink?.text,
                    href: campaign.actionLink?.href
                });
                this._contentStep.init([{ channel: MessageChannelKind.Inbox, checked: true }, { channel: MessageChannelKind.Email, checked: true }], campaign.content);
            });
        }
    }
}
