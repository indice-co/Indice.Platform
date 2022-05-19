import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Params, Router } from '@angular/router';

import { ToasterService, ToastType } from '@indice/ng-components';
import { Campaign, MessagesApiClient } from 'src/app/core/services/messages-api.service';

@Component({
  selector: 'app-campaigns-remove',
  templateUrl: './campaigns-remove.component.html'
})
export class CampaignsRemoveComponent implements OnInit {
  private _campaignId: string = '';

  constructor(
    private _api: MessagesApiClient,
    private _route: ActivatedRoute,
    private _router: Router,
    private _toaster: ToasterService
  ) { }

  public model: Campaign | null | undefined = null;

  public ngOnInit(): void {
    this._route.parent?.params.subscribe((params: Params) => {
      this._campaignId = params.campaignId;
      this._api.getCampaignById(this._campaignId).subscribe(campaign => {
        this.model = campaign;
      });
    });
  }

  public delete() {
    if (this.model?.id) {
      this._api.deleteCampaign(this.model.id).subscribe(() => {
        this._toaster.show(ToastType.Warning, 'Επιτυχής διαγραφή', `Η καμπάνια #${this.model?.title} διαγράφηκε με επιτυχία`, 5000);
        this._router.navigate(["campaigns"]);
      });
    }
  }
}
