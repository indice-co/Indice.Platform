import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { CampaignDetails } from 'src/app/core/services/messages-api.service';
import { CampaignEditStore } from '../campaign-edit-store.service';

@Component({
    selector: 'app-campaign-details-edit',
    templateUrl: './campaign-edit-details.component.html'
})
export class CampaignDetailsEditComponent implements OnInit {
    private _campaignId: string | undefined;

    constructor(
        private _campaignStore: CampaignEditStore,
        private _activatedRoute: ActivatedRoute
    ) { }

    public campaign: CampaignDetails | undefined;
    public deliveryChannels: string | undefined;

    public ngOnInit(): void {
        this._campaignId = this._activatedRoute.parent?.snapshot.params['campaignId'];
        if (this._campaignId) {
            this._campaignStore.getCampaign(this._campaignId!).subscribe((campaign: CampaignDetails) => {
                this.campaign = campaign;
                if (campaign.content) {
                    this.deliveryChannels = Object.keys(campaign.content).join(', ');
                }
            });
        }
    }
}