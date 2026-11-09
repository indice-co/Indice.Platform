import { Component, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideInfo } from '@lucide/angular';
import { FileParameter } from 'src/app/core/services/messages-api.service';
import { FileUploadComponent, IAttachment } from 'src/app/shared/components/file-upload/file-upload.component';

/** Wizard step 3: optional attachment, uploaded once the campaign is created. */
@Component({
  selector: 'app-campaign-attachments',
  imports: [TranslatePipe, FileUploadComponent, LucideInfo],
  template: `
    <div class="max-w-2xl space-y-4">
      <div role="alert" class="alert alert-soft alert-info text-sm">
        <svg lucideInfo class="size-4"></svg>
        <span>
          {{ 'Campaigns.AttachmentsUsagePrefix' | translate }}
          <strong>{{ 'Campaigns.ChannelInbox' | translate }}</strong>
          {{ 'Campaigns.AttachmentsUsageConnectorOr' | translate }}
          <strong>{{ 'Campaigns.ChannelEmail' | translate }}</strong>
          {{ 'Campaigns.AttachmentsUsageSuffix' | translate }}
        </span>
      </div>
      <app-file-upload [extendedView]="true" [maxSize]="maxSize" (fileChange)="onFileChange($event)" />
    </div>
  `
})
export class CampaignAttachmentsComponent {
  protected readonly maxSize = 6 * 1024 * 1024;
  /** The picked file, ready for `uploadCampaignAttachment`. */
  public readonly attachment = signal<FileParameter | undefined>(undefined);
  public readonly fileName = signal<string | undefined>(undefined);

  protected onFileChange(file: IAttachment | undefined): void {
    this.attachment.set(file ? { fileName: file.title ?? 'attachment', data: file.data } as FileParameter : undefined);
    this.fileName.set(file?.title);
  }
}
