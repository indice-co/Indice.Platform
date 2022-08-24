import { ChangeDetectorRef, Component, Inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ModalService, ToasterService, ToastType } from '@indice/ng-components';

import { CampaignDetails, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { BasicModalComponent } from 'src/app/shared/components/basic-modal/basic-modal.component';
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
        private _router: Router,
        @Inject(ToasterService) private _toaster: ToasterService,
        private _modalService: ModalService,
        private _api: MessagesApiClient
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

    public deleteCampaign(): void {
        const modal = this._modalService.show(BasicModalComponent, {
            animated: true,
            initialState: {
                title: 'Διαγραφή',
                message: `Είστε σίγουρος ότι θέλετε να διαγράψετε την καμπάνια '${this.campaign?.title}';`,
                data: this.campaign
            },
            keyboard: true
        });
        modal.onHidden?.subscribe((response: any) => {
            if (response.result?.answer) {
                this._api.deleteCampaign(response.result.data.id).subscribe(() => {
                    this._toaster.show(ToastType.Success, 'Επιτυχής διαγραφή', `Η καμπάνια με τίτλο '${response.result.data.title}' διαγράφηκε με επιτυχία.`);
                    this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['campaigns']));
                });
            }
        });
    }

    public publishCampaign(): void {
        const modal = this._modalService.show(BasicModalComponent, {
            animated: true,
            initialState: {
                title: 'Δημοσίευση',
                message: `Είστε σίγουρος ότι θέλετε να δημοσιεύσετε την καμπάνια '${this.campaign?.title}';`,
                data: this.campaign
            },
            keyboard: true
        });
        modal.onHidden?.subscribe((response: any) => {
            if (response.result?.answer) {
                this._api.publishCampaign(response.result.data.id).subscribe(() => {
                    this._toaster.show(ToastType.Success, 'Επιτυχής δημοσίευση', `Η καμπάνια με τίτλο '${response.result.data.title}' δημοσιεύτηκε με επιτυχία.`);
                    this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['campaigns']));
                });
            }
        });
    }

    public ngAfterViewInit(): void {
        this._changeDetector.detectChanges();
    }
}
