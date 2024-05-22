import { AfterViewChecked, ChangeDetectorRef, Component, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { HeaderMetaItem, Icons, ViewLayoutComponent } from '@indice/ng-components';
import { CampaignDetails } from 'src/app/core/services/messages-api.service';
import { CampaignEditStore } from './campaign-edit-store.service';

@Component({
    selector: 'app-campaign-edit',
    templateUrl: './campaign-edit.component.html'
})
export class CampaignEditComponent implements OnInit, AfterViewChecked {
    @ViewChild('layout', { static: true }) private _layout!: ViewLayoutComponent;
    private _campaignId?: string;

    constructor(
        private _activatedRoute: ActivatedRoute,
        private _campaignStore: CampaignEditStore,
        private _router: Router,
        private _changeDetector: ChangeDetectorRef
    ) { }

    public submitInProgress = false;
    public campaign: CampaignDetails | undefined;
    public metaItems: HeaderMetaItem[] = [];

    public ngOnInit(): void {
        this._campaignId = this._activatedRoute.snapshot.params['campaignId'];
        if (this._campaignId) {
            this._campaignStore.getCampaign(this._campaignId!).subscribe((campaign: CampaignDetails) => {
                this.campaign = campaign;
                this._layout.title = `'general.campaign' - ${campaign.title}`;
                if (campaign.published) {
                    this.metaItems.push({
                        key: 'status',
                        icon: Icons.Heart,
                        text: `'general.published-at' ${new Date()}`
                    });
                } else {
                    this.metaItems.push({
                        key: 'status',
                        icon: Icons.HeartBroken,
                        text: `'campaigns.status.unpublished'`
                    });
                }
            });
        }
    }

    public ngAfterViewChecked(): void {
        this._changeDetector.detectChanges();
    }
}