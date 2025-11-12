import { Component, ElementRef, OnInit, ViewChild, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { ToastType } from '@indice/ng-components';
import { finalize } from 'rxjs/operators';
import { FileParameter, CampaignDetails } from 'src/app/core/services/messages-api.service';
import { FileUploadComponent, IAttachment } from 'src/app/shared/components/file-upload/file-upload.component';
import { CampaignEditStore } from '../../campaign-edit-store.service';
import { AppLanguagesService } from 'src/app/shared/services/app-languages.service'; // localization
import { Subscription } from 'rxjs';
import { AppTranslatedToaster } from '../../../../../shared/services/app-translated-toaster';

@Component({
    selector: 'app-campaign-edit-attachments-rightpane',
    templateUrl: './campaign-edit-attachments-rightpane.component.html',
    standalone: false
})
export class CampaignAttachmentsEditRightpaneComponent implements OnInit, OnDestroy {

  @ViewChild('submitBtn', { static: false }) public submitButton!: ElementRef;
  @ViewChild('fileUploadComponent') public fileUploadComponent!: FileUploadComponent;

  public isLoading = false;
  public file: IAttachment | undefined;
  public campaignAttachment?: IAttachment;
  public attachmentChanged = false;

  private _campaignId: string = '';
  private _subs: Subscription[] = []; // track transient subscriptions

  constructor(
    private _campaignStore: CampaignEditStore,
    private _toaster: AppTranslatedToaster,
    private _router: Router,
    private _lang: AppLanguagesService // injected language service
  ) { }

  ngOnInit(): void {
    this._campaignId = this._router.url.split('/')[2];
    if (this._campaignId) {
      this._campaignStore.getCampaign(this._campaignId!)
        .pipe(finalize(() => {
          this.isLoading = false;
        }))
        .subscribe((campaign: CampaignDetails) => {
          if (!campaign.attachment) {
            this.campaignAttachment = undefined;
            return;
          }
          this.campaignAttachment = <IAttachment>{
            id: campaign.attachment.id,
            title: campaign.attachment.label,
            contentType: campaign.attachment.contentType,
            size: campaign.attachment.size,
            sizeText: campaign.attachment.sizeText
          }
        });
    }
  }

  public onFileChange(file: IAttachment | undefined): void {
    this.file = file;
    this.attachmentChanged = (this.campaignAttachment == undefined && this.file !== undefined) || this.file?.id !== this.campaignAttachment?.id;
  }

  public onSubmit(): void {
    if (this.file) {
      const attachment = <FileParameter>{
        fileName: this.file.title,
        data: this.file.data
      };
      this._campaignStore.uploadCampaignAttachment(this._campaignId, attachment)
        .subscribe(() => {

          this._toaster.show(ToastType.Success, 'Campaigns.AttachmentUpdateSuccessTitle', 'Campaigns.AttachmentUpdateSuccessMessage', undefined);

          this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['campaigns', this._campaignId]));
        }, (error) => {
          this._toaster.show(ToastType.Error, 'Campaigns.AttachmentUpdateErrorTitle', 'Campaigns.AttachmentUpdateErrorMessage', undefined);
          this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['campaigns', this._campaignId]));
        });
    }
    else if (this.campaignAttachment?.id) {
      this._campaignStore.deleteCampaignAttachment(this._campaignId, this.campaignAttachment.id)
        .subscribe(() => {
          this._toaster.show(ToastType.Success, 'Campaigns.AttachmentUpdateSuccessTitle', 'Campaigns.AttachmentDeleteSuccessMessage', undefined);
          this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['campaigns', this._campaignId]));
        }, (error) => {
          this._toaster.show(ToastType.Error, 'Campaigns.AttachmentUpdateSuccessTitle', 'Campaigns.AttachmentUpdateErrorMessage', undefined);
          this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['campaigns', this._campaignId]));
        });
    }
  }

  public ngOnDestroy(): void {
    this._subs.forEach(s => s.unsubscribe());
  }
}
