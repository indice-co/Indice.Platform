import { Injectable, inject, signal } from '@angular/core';
import { Observable, map, shareReplay, tap } from 'rxjs';
import { CampaignDetails, FileParameter, MessagesApiClient, UpdateCampaignRequest } from 'src/app/core/services/messages-api.service';

/**
 * The campaign shown by the campaign edit page and its tabs / side panes. `campaign` holds the last loaded
 * campaign; `getCampaign` loads it once per id until a change invalidates the cache.
 */
@Injectable({ providedIn: 'root' })
export class CampaignEditStore {
  private readonly _api = inject(MessagesApiClient);
  private _cache?: { id: string; request: Observable<CampaignDetails> };

  public readonly campaign = signal<CampaignDetails | undefined>(undefined);

  public getCampaign(campaignId: string): Observable<CampaignDetails> {
    if (this._cache?.id !== campaignId) {
      if (this.campaign()?.id !== campaignId) {
        this.campaign.set(undefined);
      }
      const request = this._api.getCampaignById(campaignId).pipe(
        tap(campaign => this.campaign.set(campaign)),
        shareReplay({ bufferSize: 1, refCount: false })
      );
      this._cache = { id: campaignId, request };
      request.subscribe({ error: () => this._cache = undefined });
    }
    return this._cache.request;
  }

  public invalidate(): void {
    this._cache = undefined;
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
      recipientListId: campaign.distributionList?.id,
      mediaBaseHref: campaign.mediaBaseHref
    });
    return this._api.updateCampaign(campaignId, body).pipe(map(() => this.invalidate()));
  }

  public uploadCampaignAttachment(campaignId: string, attachment?: FileParameter): Observable<void> {
    return this._api.uploadCampaignAttachment(campaignId, attachment).pipe(map(() => this.invalidate()));
  }

  public deleteCampaignAttachment(campaignId: string, attachmentId: string): Observable<void> {
    return this._api.deleteCampaignAttachment(campaignId, attachmentId).pipe(map(() => this.invalidate()));
  }

  public publishCampaign(campaignId: string): Observable<void> {
    return this._api.publishCampaign(campaignId).pipe(map(() => this.invalidate()));
  }
}
