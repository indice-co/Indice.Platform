import { ChangeDetectorRef, Component, Inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ModalService, ToasterService, ToastType } from '@indice/ng-components';

import { CampaignDetails, MessageSender, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { BasicModalComponent } from 'src/app/shared/components/basic-modal/basic-modal.component';
import { CampaignEditStore } from '../campaign-edit-store.service';
import { HttpClient } from '@angular/common/http';
import { settings } from 'src/app/core/models/settings';
import { AppLanguagesService } from 'src/app/shared/services/app-languages.service';
import { combineLatest } from 'rxjs';
import { AppTranslatedToaster } from '../../../../shared/services/app-translated-toaster';

@Component({
    selector: 'app-campaign-details-edit',
    templateUrl: './campaign-edit-details.component.html',
    standalone: false
})
export class CampaignDetailsEditComponent implements OnInit {
  private _campaignId: string | undefined;

  constructor(
    private _campaignStore: CampaignEditStore,
    private _activatedRoute: ActivatedRoute,
    private _changeDetector: ChangeDetectorRef,
    private _router: Router,
    @Inject(AppTranslatedToaster) private _toaster: AppTranslatedToaster,
    private _modalService: ModalService,
    private _api: MessagesApiClient,
    private _httpClient: HttpClient,
    private _lang: AppLanguagesService
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
    const campaignTitle = this.campaign?.title ?? '';
    combineLatest([
      this._lang.translateKey('Campaigns.Delete'),
      this._lang.translateKey('Campaigns.DeleteConfirmMessage', { title: campaignTitle })
    ]).subscribe(([translatedModalTitle, translatedModalMessage]) => {
      const modal = this._modalService.show(BasicModalComponent, {
        animated: true,
        initialState: {
          title: translatedModalTitle || 'Campaigns.Delete',
          message: translatedModalMessage || `Campaigns.DeleteConfirmMessage`,
          data: this.campaign,
          acceptText: translatedModalTitle || 'Campaigns.Delete',
        },
        keyboard: true
      });
      modal.onHidden?.subscribe((response: any) => {
        if (response.result?.answer) {
            this._api.deleteCampaign(response.result.data.id).subscribe(() => {
            this._toaster.show(ToastType.Success, 'Campaigns.DeleteSuccessTitle', 'Campaigns.DeleteSuccessMessage', undefined, { title: response.result.data.title });
            this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['campaigns']));
          });
        }
      });
    }).unsubscribe();
  }

  public publishCampaign(): void {
    const campaignTitle = this.campaign?.title ?? '';
    combineLatest([
      this._lang.translateKey('Campaigns.Publish'),
      this._lang.translateKey('Campaigns.PublishConfirmMessage', { title: campaignTitle })
    ]).subscribe(([translatedPublishTitle, translatedPublishMessage]) => {
      const modal = this._modalService.show(BasicModalComponent, {
        animated: true,
        initialState: {
          title: translatedPublishTitle || 'Campaigns.Publish',
          message: translatedPublishMessage || 'Campaigns.PublishConfirmMessage',
          data: this.campaign,
          acceptText: translatedPublishTitle || 'Campaigns.Publish',
          type: 'success'
        },
        keyboard: true
      });
      modal.onHidden?.subscribe((response: any) => {
        if (response.result?.answer) {
          this._campaignStore.publishCampaign(response.result.data.id).subscribe(() => {
            this._toaster.show(ToastType.Success, 'Campaigns.PublishSuccessTitle', 'Campaigns.PublishSuccessMessage', undefined, { title: response.result.data.title });
            this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['campaigns', this._campaignId]));

          });
        }
      });
    }).unsubscribe();
  }

  public apiOrigin = (settings.api_url || "").substring(0, 4) === "http" ? new URL(settings.api_url).origin : "";

  public downloadAttachment() {
    if (!this.campaign?.attachment?.permaLink || !this.campaign?.attachment?.label) {
      return;
    }
    var url = `${this.apiOrigin}/${this.campaign?.attachment?.permaLink}`;
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
