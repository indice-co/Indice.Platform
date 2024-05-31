import { ChangeDetectorRef, Component, Inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ModalService, ToasterService, ToastType } from '@indice/ng-components';

import { CampaignDetails, MessageSender, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { BasicModalComponent } from 'src/app/shared/components/basic-modal/basic-modal.component';
import { CampaignEditStore } from '../campaign-edit-store.service';
import { HttpClient } from '@angular/common/http';
import { settings } from 'src/app/core/models/settings';
import { TranslateService } from '@ngx-translate/core';

@Component({
    selector: 'app-campaign-details-edit',
    templateUrl: './campaign-edit-details.component.html'
})
export class CampaignDetailsEditComponent implements OnInit {
    private _campaignId: string | undefined;

    constructor(
        private _campaignStore: CampaignEditStore,
        private _activatedRoute: ActivatedRoute,
        private _translate: TranslateService,
        private _changeDetector: ChangeDetectorRef,
        private _router: Router,
        @Inject(ToasterService) private _toaster: ToasterService,
        private _modalService: ModalService,
        private _api: MessagesApiClient,
        private _httpClient: HttpClient
    ) { }

    public campaign: CampaignDetails | undefined;
    public deliveryChannels: string | undefined;
    public defaultSender: MessageSender | undefined;

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
        this._api.getMessageSenders(undefined, undefined, undefined, undefined, true)
            .subscribe((result) => {
                this.defaultSender = result?.items?.[0]
            });
    }

    public openEditPane(action: string): void {
        this._router.navigate(['', { outlets: { rightpane: ['edit-campaign'] } }], { queryParams: { action: action } });
    }

    public openEditAttachmentsPane(): void {
        this._router.navigate(['', { outlets: { rightpane: ['edit-campaign-attachments'] } }]);
    }

    public deleteCampaign(): void {
        const modal = this._modalService.show(BasicModalComponent, {
            animated: true,
            initialState: {
                title: this._translate.instant('campaigns-edit.delete'),
                message: `'${this._translate.instant('campaigns-edit.delete-warning')}' '${this.campaign?.title}';`,
                data: this.campaign
            },
            keyboard: true
        });
        modal.onHidden?.subscribe((response: any) => {
            if (response.result?.answer) {
                this._api.deleteCampaign(response.result.data.id).subscribe(() => {
                    this._toaster.show(ToastType.Success, this._translate.instant('campaigns-edit.success-delete'), `'${this._translate.instant('campaigns-edit.success-delete-message')}' '${response.result.data.title}'`);
                    this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['campaigns']));
                });
            }
        });
    }

    public publishCampaign(): void {
        const modal = this._modalService.show(BasicModalComponent, {
            animated: true,
            initialState: {
                title: this._translate.instant('campaigns-edit.publish'),
                message: `'${this._translate.instant('campaigns-edit.publish-warning')}' '${this.campaign?.title}';`,
                data: this.campaign,
                acceptText: this._translate.instant('campaigns-edit.publish'),
                type: 'success'
            },
            keyboard: true
        });
        modal.onHidden?.subscribe((response: any) => {
            if (response.result?.answer) {
                this._campaignStore.publishCampaign(response.result.data.id).subscribe(() => {
                    this._toaster.show(ToastType.Success, this._translate.instant('campaigns-edit.success-publish'), `'${this._translate.instant('campaigns-edit.success-publish-message')}' '${response.result.data.title}'`);
                    this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['campaigns', this._campaignId]));
                });
            }
        });
    }

    public downloadAttachment() {
        if (!this.campaign?.attachment?.permaLink || !this.campaign?.attachment?.label) {
            return;
        }
        var url = `${settings.api_url}/${this.campaign?.attachment?.permaLink}`;
        this._httpClient.get(url, { responseType: 'arraybuffer' })
            .subscribe((blob) => {
                const url = window.URL.createObjectURL(new Blob([blob]));
                const a = document.createElement('a');
                a.style.display = 'none';
                a.href = url;
                a.download = this.campaign?.attachment?.label ?? 'download';
                document.body.appendChild(a);
                a.click();
                window.URL.revokeObjectURL(url);
            })
    }

    public ngAfterViewInit(): void {
        this._changeDetector.detectChanges();
    }
}
