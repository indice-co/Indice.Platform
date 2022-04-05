import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ToasterService, ToastType } from '@indice/ng-components';
import { Campaign, CampaignsApiService, ValidationProblemDetails } from 'src/app/core/services/campaigns-api.services';
import { UtilitiesService } from 'src/app/shared/utilities.service';

@Component({
  selector: 'app-campaigns-remove',
  templateUrl: './campaigns-remove.component.html'
})
export class CampaignsRemoveComponent implements OnInit {

  constructor(
    private _api: CampaignsApiService,
    private _route: ActivatedRoute,
    private _router: Router,
    private _toaster: ToasterService,
    public _utilities: UtilitiesService) { }

  public model: Campaign | null | undefined = null;
  private _campaignId: string = '';

  ngOnInit(): void {
    this._route.parent?.params.subscribe(p => {
      this._campaignId = p.campaignId;
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
      }, (problemDetails: ValidationProblemDetails) => {
        this._toaster.show(ToastType.Error, 'Αποτυχία επεξεργασίας', `${this._utilities.getValidationProblemDetails(problemDetails)}`, 6000);
      });
    }
  }

}
