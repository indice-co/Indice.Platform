import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import {  CampaignMessageDetailsResponse, MessagesApiClient } from 'src/app/core/services/messages-api.service';

@Component({
  selector: 'app-campaign-message-timeline',
  templateUrl: './campaign-message-timeline.component.html'
})
export class CampaignMessageTimelineComponent  implements OnInit  {
  public _campaignId: string | undefined;
  public _contactId: string | undefined;
  public campaignTimeline: CampaignMessageDetailsResponse[] | undefined;

  constructor(
      private _router: Router,
      private _activatedRoute: ActivatedRoute,
      private _api: MessagesApiClient
  ) { }
  
  public ngOnInit(): void {
    this._campaignId = this._activatedRoute.parent?.parent?.snapshot.params['campaignId'];
    this._contactId = this._activatedRoute.snapshot.params['contactId'];
    this._api.getCampaignMessageDetails(this._campaignId!, this._contactId!).subscribe(data => {
      this.campaignTimeline =data;
    });
  }
}
