import { Injectable } from '@angular/core';

import { AsyncSubject, Observable } from 'rxjs';
import { CampaignDetails, MessagesApiClient } from 'src/app/core/services/messages-api.service';

@Injectable()
export class CampaignEditStore {
    private _campaign: AsyncSubject<CampaignDetails> | undefined;

    constructor(private _api: MessagesApiClient) { }

    public getCampaign(campaignId: string): Observable<CampaignDetails> {
        if (!this._campaign) {
            this._campaign = new AsyncSubject<CampaignDetails>();
            this._api.getCampaignById(campaignId).subscribe((campaign: CampaignDetails) => {
                this._campaign!.next(campaign);
                this._campaign!.complete();
            });
        }
        return this._campaign;
    }
}
