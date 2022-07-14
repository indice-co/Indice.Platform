import { AfterViewChecked, ChangeDetectorRef, Component, Inject, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { HeaderMetaItem, Icons, ModalService, ToasterService, ToastType, ViewLayoutComponent } from '@indice/ng-components';
import { CampaignDetails, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { BasicModalComponent } from 'src/app/shared/components/basic-modal/basic-modal.component';
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
        @Inject(ToasterService) private _toaster: ToasterService,
        private _modalService: ModalService,
        private _api: MessagesApiClient
    ) { }

    public submitInProgress = false;
    public campaign: CampaignDetails | undefined;
    public metaItems: HeaderMetaItem[] = [];

    public ngOnInit(): void {
        this._campaignId = this._activatedRoute.snapshot.params['campaignId'];
        if (this._campaignId) {
            this._campaignStore.getCampaign(this._campaignId!).subscribe((campaign: CampaignDetails) => {
                this.campaign = campaign;
                this._layout.title = `Καμπάνια - ${campaign.title}`;
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

    public isActive(commands: string[]): boolean {
        const url = this._router.createUrlTree(commands);
        return this._router.isActive(url, { paths: 'exact', queryParams: 'exact', fragment: 'ignored', matrixParams: 'ignored' });
    }
}