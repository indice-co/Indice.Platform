import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import {  MessagesApiClient } from 'src/app/core/services/messages-api.service';

@Component({
  selector: 'app-campaign-message-timeline',
  templateUrl: './campaign-message-timeline.component.html'
})
export class CampaignMessageTimelineComponent  implements OnInit  {
  public _campaignId: string | undefined;
  public _messageId: string | undefined;

  constructor(
    route: ActivatedRoute,
    router: Router,
    private readonly _activatedRoute: ActivatedRoute,
    private readonly _api: MessagesApiClient
  ) {
      
  }
  ngOnInit(): void {
    this._campaignId = this._activatedRoute.parent?.snapshot.params['campaignId'];
    this._messageId = this._activatedRoute.parent?.snapshot.params['messageId'];
  }
}
