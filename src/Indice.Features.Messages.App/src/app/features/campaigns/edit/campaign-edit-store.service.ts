import { Injectable } from '@angular/core';

import { AsyncSubject, Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { CampaignDetails, FileParameter, MessagesApiClient, UpdateCampaignRequest } from 'src/app/core/services/messages-api.service';

@Injectable({
    providedIn: 'root'
})
export class CampaignEditStore {
    private _campaign: AsyncSubject<CampaignDetails> | undefined;
    private _idChanged = false;
    private _currentId = '';

    constructor(
        private _api: MessagesApiClient
    ) { }

    public getCampaign(campaignId: string): Observable<CampaignDetails> {
        this._idChanged = this._currentId !== campaignId;
        this._currentId = campaignId;
        if (!this._campaign || this._idChanged) {
            this._campaign = new AsyncSubject<CampaignDetails>();
            this._api
                .getCampaignById(campaignId)
                .subscribe((campaign: CampaignDetails) => {
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
        return this._api
            .updateCampaign(campaignId, body)
            .pipe(
                map(_ => this._campaign = undefined)
            );
    }

    public uploadCampaignAttachment(campaignId: string, attachment?: FileParameter) {
        return this._api
            .uploadCampaignAttachment(campaignId, attachment)
            .pipe(
                map(_ => this._campaign = undefined)
            );
    }

    public deleteCampaignAttachment(campaignId: string, attachmentId: string) {
        return this._api
            .deleteCampaignAttachment(campaignId, attachmentId)
            .pipe(
                map(_ => this._campaign = undefined)
            );
    }

    public publishCampaign(campaignId: string): Observable<void> {
        return this._api
            .publishCampaign(campaignId)
            .pipe(
                map(_ => this._campaign = undefined)
            );
    }
}
