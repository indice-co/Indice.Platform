import { AfterViewChecked, ChangeDetectorRef, Component, Inject, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { HeaderMetaItem, Icons, ToasterService, ViewLayoutComponent } from '@indice/ng-components';
import { CampaignDetails } from 'src/app/core/services/messages-api.service';
import { CampaignEditStore } from './campaign-edit-store.service';

@Component({
    selector: 'app-campaign-edit',
    templateUrl: './campaign-edit.component.html',
    providers: [CampaignEditStore]
})
export class CampaignEditComponent implements OnInit, AfterViewChecked {
    @ViewChild('layout', { static: true }) private _layout!: ViewLayoutComponent;
    private _campaignId?: string;

    constructor(
        private _activatedRoute: ActivatedRoute,
        private _campaignStore: CampaignEditStore,
        private _router: Router,
        private _changeDetector: ChangeDetectorRef,
        @Inject(ToasterService) private _toaster: ToasterService
    ) { }

    public templateId: string | undefined;
    public submitInProgress = false;
    public campaign: CampaignDetails = new CampaignDetails();
    public metaItems: HeaderMetaItem[] = [];

    public ngOnInit(): void {
        this._campaignId = this._activatedRoute.snapshot.params['campaignId'];
        if (this._campaignId) {
            this._campaignStore.getCampaign(this._campaignId!).subscribe((campaign: CampaignDetails) => {
                this.campaign = campaign;
                this._layout.title = `Campaign - ${campaign.title}`;
                if (campaign.published) {
                    this.metaItems.push({ key: 'status', icon: Icons.Heart, text: `Δημοσιεύτηκε στις ${new Date()}` });
                } else {
                    this.metaItems.push({ key: 'status', icon: Icons.HeartBroken, text: `Μη δημοσιευμένη` });
                }
            });
        }
    }

    public ngAfterViewChecked(): void {
        this._changeDetector.detectChanges();
    }
}