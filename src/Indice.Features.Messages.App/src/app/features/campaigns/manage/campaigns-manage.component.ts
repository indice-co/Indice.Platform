import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Params } from '@angular/router';

import { Campaign, MessagesApiClient } from '../../../core/services/campaigns-api.services';
@Component({
  selector: 'app-campaigns',
  templateUrl: './campaigns-manage.component.html'
})
export class CampaignsManageComponent implements OnInit {
  constructor(
    private _api: MessagesApiClient,
    private _route: ActivatedRoute
  ) { }

  public model: Campaign | null | undefined = null;

  public ngOnInit(): void {
    this._route.params.subscribe((params: Params) => {
      const id = params.campaignId;
      this._api.getCampaignById(id).subscribe(campaign => {
        this.model = campaign;
      });
    });
  }
}
