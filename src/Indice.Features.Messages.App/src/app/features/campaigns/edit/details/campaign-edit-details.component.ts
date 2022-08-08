import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { CampaignDetails } from 'src/app/core/services/messages-api.service';
import { CampaignEditStore } from '../campaign-edit-store.service';

@Component({
    selector: 'app-campaign-details-edit',
    templateUrl: './campaign-edit-details.component.html'
})
export class CampaignDetailsEditComponent implements OnInit {
    private _campaignId: string | undefined;

    constructor(
        private _campaignStore: CampaignEditStore,
        private _activatedRoute: ActivatedRoute,
        private _changeDetector: ChangeDetectorRef,
        private _router: Router
    ) { }

    public campaign: CampaignDetails | undefined;
    public deliveryChannels: string | undefined;

    public ngOnInit(): void {
        this._campaignId = this._activatedRoute.parent?.snapshot.params['campaignId'];
        if (this._campaignId) {
            this._campaignStore.getCampaign(this._campaignId!).subscribe((campaign: CampaignDetails) => {
                this.campaign = campaign;
                if (campaign.content) {
                    this.deliveryChannels = Object.keys(campaign.content).join(', ');
                }
            });
        }
    }

    public openEditPane(action: string): void {
        this._router.navigate(['', { outlets: { rightpane: ['edit-campaign'] } }], { queryParams: { action: action } });
    }

    public ngAfterViewInit(): void {
        this._changeDetector.detectChanges();
    }
}
