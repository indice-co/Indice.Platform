import { Injectable } from '@angular/core';

import { AsyncSubject, Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { CampaignDetails, MessagesApiClient, UpdateCampaignRequest } from 'src/app/core/services/messages-api.service';

@Injectable({
    providedIn: 'root'
})
export class CampaignEditStore {
    private _campaign: AsyncSubject<CampaignDetails> | undefined;
    private _idChanged = false;
    private _currentId = '';

    constructor(private _api: MessagesApiClient) { }

    public getCampaign(campaignId: string): Observable<CampaignDetails> {
        this._idChanged = this._currentId !== campaignId;
        this._currentId = campaignId;
        if (!this._campaign || this._idChanged) {
            this._campaign = new AsyncSubject<CampaignDetails>();
            this._api.getCampaignById(campaignId).subscribe((campaign: CampaignDetails) => {
                this._campaign?.next(campaign);
                this._campaign?.complete();
            });
        }
        return this._campaign;
    }

    public updateCampaign(campaignId: string, campaign: CampaignDetails): Observable<void> {
        const body = new UpdateCampaignRequest({
            actionLink: campaign.actionLink,
            activePeriod: campaign.activePeriod,
            content: campaign.content,
            data: campaign.data,
            title: campaign.title,
            typeId: campaign.type?.id,
            isGlobal: campaign.isGlobal,
            recipientListId: campaign.distributionList?.id
        });
        return this._api.updateCampaign(campaignId, body).pipe(
            map(_ => this._campaign = undefined)
        );
    }
}
