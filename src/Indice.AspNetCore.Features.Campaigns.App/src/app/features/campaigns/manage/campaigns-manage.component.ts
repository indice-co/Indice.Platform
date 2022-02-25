import { Campaign } from '../../../core/services/campaigns-api.services';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CampaignsApiService } from 'src/app/core/services/campaigns-api.services';

@Component({
  selector: 'app-campaigns',
  templateUrl: './campaigns-manage.component.html'
})
export class CampaignsManageComponent implements OnInit {
  public model: Campaign | null | undefined = null;
  constructor(private _api: CampaignsApiService, private route: ActivatedRoute) {
  }

  ngOnInit(): void {
    this.route.params.subscribe(p => {
      const id = p.campaignId;
      this._api.getCampaignById(id).subscribe(campaign => {
        this.model = campaign;
      });
    });
  }

}
