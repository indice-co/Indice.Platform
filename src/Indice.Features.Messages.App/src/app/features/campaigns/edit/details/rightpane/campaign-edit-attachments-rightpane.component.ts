import { Component, OnInit, inject, input, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { LucideInfo } from '@lucide/angular';
import { FileParameter } from 'src/app/core/services/messages-api.service';
import { FileUploadComponent, IAttachment } from 'src/app/shared/components/file-upload/file-upload.component';
import { AppTranslatedToaster, ToastType } from 'src/app/shared/services/app-translated-toaster';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { SidePaneLayoutComponent, SidePaneService } from 'src/app/ui/side-pane/side-pane';
import { CampaignEditStore } from '../../campaign-edit-store.service';

/** Side pane that replaces or removes the attachment of a draft campaign. */
@Component({
  selector: 'app-campaign-edit-attachments-rightpane',
  imports: [TranslatePipe, SidePaneLayoutComponent, FileUploadComponent, LucideInfo],
  template: `
    <app-side-pane-layout [title]="'Campaigns.EditAttachmentsTitle' | translate">
      <div class="space-y-5">
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
        @if (loading()) {
          <div class="skeleton h-14 w-full"></div>
        } @else {
          <app-file-upload [extendedView]="true" [existingFile]="existing()" [maxSize]="maxSize" (fileChange)="onFileChange($event)" />
        }
      </div>
      <div footer class="flex justify-end gap-2 border-t border-base-300 px-6 py-4">
        <button type="button" class="btn btn-ghost" (click)="pane.close()">{{ 'Campaigns.Close' | translate }}</button>
        <button type="button" class="btn btn-primary" [disabled]="loading() || saving() || !changed()" (click)="submit()">
          @if (saving()) { <span class="loading loading-spinner loading-sm"></span> }
          {{ 'Campaigns.Save' | translate }}
        </button>
      </div>
    </app-side-pane-layout>
  `
})
export class CampaignAttachmentsEditRightpaneComponent implements OnInit {
  private readonly _store = inject(CampaignEditStore);
  private readonly _toaster = inject(AppTranslatedToaster);
  private readonly _refresh = inject(RefreshBus);
  protected readonly pane = inject(SidePaneService);

  /** Matrix param of the `edit-campaign-attachments` pane route. */
  public readonly campaignId = input.required<string>();

  protected readonly maxSize = 6 * 1024 * 1024;
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly changed = signal(false);
  protected readonly existing = signal<IAttachment | undefined>(undefined);
  private _file?: IAttachment;

  public ngOnInit(): void {
    this._store.getCampaign(this.campaignId()).subscribe({
      next: campaign => {
        const attachment = campaign.attachment;
        this.existing.set(attachment
          ? { id: attachment.id, title: attachment.label, contentType: attachment.contentType, size: attachment.size, sizeText: attachment.sizeText }
          : undefined);
        this.loading.set(false);
      },
      error: () => this.pane.close()
    });
  }

  protected onFileChange(file: IAttachment | undefined): void {
    this._file = file;
    const existing = this.existing();
    this.changed.set((!existing && !!file) || file?.id !== existing?.id);
  }

  protected submit(): void {
    const existingId = this.existing()?.id;
    let request$: Observable<void>;
    let message: string;
    if (this._file) {
      request$ = this._store.uploadCampaignAttachment(this.campaignId(), { fileName: this._file.title, data: this._file.data } as FileParameter);
      message = 'Campaigns.AttachmentUpdateSuccessMessage';
    } else if (existingId) {
      request$ = this._store.deleteCampaignAttachment(this.campaignId(), existingId);
      message = 'Campaigns.AttachmentDeleteSuccessMessage';
    } else {
      return;
    }
    this.saving.set(true);
    request$.subscribe({
      next: () => {
        this._toaster.show(ToastType.Success, 'Campaigns.AttachmentUpdateSuccessTitle', message);
        this._refresh.notify('campaign');
        this.pane.close();
      },
      error: () => this.saving.set(false)
    });
  }
}
