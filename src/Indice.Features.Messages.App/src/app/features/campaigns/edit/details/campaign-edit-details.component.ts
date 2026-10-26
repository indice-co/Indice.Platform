import { DatePipe, NgTemplateOutlet } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideDownload, LucideExternalLink, LucidePaperclip, LucidePencil } from '@lucide/angular';
import { settings } from 'src/app/core/models/settings';
import { MessageSender, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { BoolIconComponent, CHANNEL_KINDS, ChannelIconComponent, MessageTypeBadgeComponent } from 'src/app/ui/badges/badges';
import { SidePaneService } from 'src/app/ui/side-pane/side-pane';
import { contentChannel } from '../../campaign-utils';
import { CampaignEditStore } from '../campaign-edit-store.service';

/** Details tab: read-only overview with per field edit shortcuts while the campaign is a draft. */
@Component({
  selector: 'app-campaign-details-edit',
  imports: [
    DatePipe, NgTemplateOutlet, RouterLink, TranslatePipe, ChannelIconComponent, MessageTypeBadgeComponent, BoolIconComponent,
    LucideDownload, LucideExternalLink, LucidePaperclip, LucidePencil
  ],
  template: `
    @let campaign = store.campaign();
    <section class="panel p-5 sm:p-6">
      <div class="mb-5">
        <h2 class="text-lg font-semibold">{{ 'Campaigns.BasicInformationHeader' | translate }}</h2>
        <p class="text-sm text-base-content/70">{{ 'Campaigns.BasicInformationDescription' | translate }}</p>
      </div>
      @if (!campaign) {
        <div class="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
          @for (row of [1, 2, 3, 4, 5, 6]; track row) { <div class="skeleton h-10 w-full"></div> }
        </div>
      } @else {
        <dl class="grid gap-x-6 gap-y-5 sm:grid-cols-2 lg:grid-cols-3">
          <div>
            <dt class="text-xs text-base-content/60">{{ 'Campaigns.StatusLabel' | translate }}</dt>
            <dd class="mt-1">
              @if (campaign.published) {
                <span class="badge badge-sm badge-soft badge-success">{{ 'Campaigns.StatusPublished' | translate }}</span>
              } @else {
                <span class="badge badge-sm badge-ghost">{{ 'Campaigns.StatusUnpublished' | translate }}</span>
              }
            </dd>
          </div>
          <div>
            <dt class="flex items-center gap-1 text-xs text-base-content/60">
              {{ 'Campaigns.TitleLabel' | translate }}<ng-container *ngTemplateOutlet="edit; context: { $implicit: 'editTitle' }" />
            </dt>
            <dd class="font-medium">{{ campaign.title || ('Campaigns.EmptyValueIndicator' | translate) }}</dd>
          </div>
          <div>
            <dt class="flex items-center gap-1 text-xs text-base-content/60">
              {{ 'Campaigns.TypeLabel' | translate }}<ng-container *ngTemplateOutlet="edit; context: { $implicit: 'editType' }" />
            </dt>
            <dd class="mt-1"><app-message-type-badge [name]="campaign.type?.name" [classification]="campaign.type?.classification" /></dd>
          </div>
          <div>
            <dt class="flex items-center gap-1 text-xs text-base-content/60">
              {{ 'Campaigns.DurationLabel' | translate }}<ng-container *ngTemplateOutlet="edit; context: { $implicit: 'editActivePeriod' }" />
            </dt>
            <dd class="font-medium tabular">
              {{ campaign.activePeriod?.from | date: 'medium' }} –
              {{ campaign.activePeriod?.to ? (campaign.activePeriod?.to | date: 'medium') : ('Campaigns.InfiniteSymbol' | translate) }}
            </dd>
          </div>
          <div>
            <dt class="flex items-center gap-1 text-xs text-base-content/60">
              {{ 'Campaigns.CallToActionSectionLabel' | translate }}<ng-container *ngTemplateOutlet="edit; context: { $implicit: 'editCta' }" />
            </dt>
            <dd class="font-medium">
              @if (campaign.actionLink?.text && campaign.actionLink?.href) {
                <a class="link link-primary inline-flex items-center gap-1" target="_blank" rel="noopener" [href]="campaign.actionLink?.href">
                  {{ campaign.actionLink?.text }}<svg lucideExternalLink class="size-3.5"></svg>
                </a>
              } @else {
                {{ campaign.actionLink?.text || ('Campaigns.EmptyValueIndicator' | translate) }}
              }
            </dd>
          </div>
          <div>
            <dt class="text-xs text-base-content/60">{{ 'Campaigns.CommunicationChannelsLabel' | translate }}</dt>
            <dd class="mt-1 flex items-center gap-1">
              @for (kind of channelKinds; track kind) {
                <app-channel-icon [kind]="kind" [muted]="!usedChannels().includes(kind)" />
              }
            </dd>
          </div>
          <div>
            <dt class="text-xs text-base-content/60">{{ 'Campaigns.TargetLabel' | translate }}</dt>
            <dd class="font-medium">{{ (campaign.distributionList ? 'Campaigns.TargetDistributionListValue' : 'Campaigns.TargetAllValue') | translate }}</dd>
          </div>
          <div>
            <dt class="flex items-center gap-1 text-xs text-base-content/60">
              {{ 'Campaigns.DistributionListLabel' | translate }}<ng-container *ngTemplateOutlet="edit; context: { $implicit: 'editList' }" />
            </dt>
            <dd class="font-medium">
              @if (campaign.distributionList) {
                <a class="link link-primary" [routerLink]="['/distribution-lists', campaign.distributionList.id, 'distribution-list-contacts']">{{ campaign.distributionList.name }}</a>
              } @else {
                {{ 'Campaigns.EmptyValueIndicator' | translate }}
              }
            </dd>
          </div>
          @if (campaign.content?.['email']) {
            <div>
              <dt class="flex items-center gap-1 text-xs text-base-content/60">
                {{ 'Campaigns.EmailSenderLabel' | translate }}<ng-container *ngTemplateOutlet="edit; context: { $implicit: 'editSender' }" />
              </dt>
              @let sender = campaign.content?.['email']?.sender ?? defaultSender();
              <dd class="font-medium break-all">{{ sender ? senderLabel(sender) : ('Campaigns.EmptyValueIndicator' | translate) }}</dd>
            </div>
          }
          <div>
            <dt class="text-xs text-base-content/60">{{ 'Campaigns.IgnoreUserPreferencesLabel' | translate }}</dt>
            <dd class="mt-1"><app-bool-icon [value]="campaign.ignoreUserPreferences" /></dd>
          </div>
          <div>
            <dt class="flex items-center gap-1 text-xs text-base-content/60">
              {{ 'Campaigns.AttachmentsLabel' | translate }}
              @if (!campaign.published) {
                <a class="btn btn-ghost btn-xs btn-square" [routerLink]="pane.link('edit-campaign-attachments', { campaignId: campaign.id! })"
                  [attr.aria-label]="'Campaigns.EditAttachmentsTitle' | translate"><svg lucidePencil class="size-3.5"></svg></a>
              }
            </dt>
            <dd class="font-medium">
              @if (campaign.attachment) {
                <button type="button" class="link link-primary inline-flex items-center gap-1.5 break-all text-left" (click)="downloadAttachment()">
                  <svg lucidePaperclip class="size-3.5 shrink-0"></svg>{{ campaign.attachment.label }}
                  @if (downloading()) { <span class="loading loading-spinner loading-xs"></span> } @else { <svg lucideDownload class="size-3.5 shrink-0"></svg> }
                </button>
              } @else {
                {{ 'Campaigns.EmptyValueIndicator' | translate }}
              }
            </dd>
          </div>
        </dl>

        <div class="mt-6 grid gap-x-6 gap-y-4 border-t border-base-300 pt-5 text-sm sm:grid-cols-2 lg:grid-cols-4">
          <div>
            <p class="text-xs text-base-content/60">{{ 'Campaigns.CreatedByLabel' | translate }}</p>
            <p class="font-medium break-all">{{ campaign.createdBy || ('Campaigns.EmptyValueIndicator' | translate) }}</p>
          </div>
          <div>
            <p class="text-xs text-base-content/60">{{ 'Campaigns.CreatedAtLabel' | translate }}</p>
            <p class="font-medium tabular">{{ campaign.createdAt ? (campaign.createdAt | date: 'medium') : ('Campaigns.EmptyValueIndicator' | translate) }}</p>
          </div>
          <div>
            <p class="text-xs text-base-content/60">{{ 'Campaigns.UpdatedByLabel' | translate }}</p>
            <p class="font-medium break-all">{{ campaign.updatedBy || ('Campaigns.EmptyValueIndicator' | translate) }}</p>
          </div>
          <div>
            <p class="text-xs text-base-content/60">{{ 'Campaigns.UpdatedAtLabel' | translate }}</p>
            <p class="font-medium tabular">{{ campaign.updatedAt ? (campaign.updatedAt | date: 'medium') : ('Campaigns.EmptyValueIndicator' | translate) }}</p>
          </div>
        </div>
      }
    </section>

    <ng-template #edit let-action>
      @if (store.campaign(); as campaign) {
        @if (!campaign.published) {
          <a class="btn btn-ghost btn-xs btn-square" [routerLink]="pane.link('edit-campaign', { campaignId: campaign.id!, action })"
            [attr.aria-label]="'Campaigns.EditCampaignTitle' | translate"><svg lucidePencil class="size-3.5"></svg></a>
        }
      }
    </ng-template>
  `
})
export class CampaignDetailsEditComponent implements OnInit {
  private readonly _api = inject(MessagesApiClient);
  private readonly _http = inject(HttpClient);
  protected readonly store = inject(CampaignEditStore);
  protected readonly pane = inject(SidePaneService);

  protected readonly channelKinds = CHANNEL_KINDS;
  protected readonly defaultSender = signal<MessageSender | undefined>(undefined);
  protected readonly downloading = signal(false);
  protected readonly usedChannels = computed<string[]>(() =>
    Object.keys(this.store.campaign()?.content ?? {}).map(key => contentChannel(key)?.kind ?? key));
  private readonly _apiOrigin = (settings.api_url || '').substring(0, 4) === 'http' ? new URL(settings.api_url).origin : '';

  public ngOnInit(): void {
    this._api.getMessageSenders(undefined, undefined, undefined, undefined, true)
      .subscribe(result => this.defaultSender.set(result?.items?.[0]));
  }

  protected senderLabel(sender: MessageSender): string {
    return `${sender.displayName} <${sender.sender}>`;
  }

  protected downloadAttachment(): void {
    const attachment = this.store.campaign()?.attachment;
    if (!attachment?.permaLink || !attachment.label) {
      return;
    }
    this.downloading.set(true);
    this._http.get(`${this._apiOrigin}/${attachment.permaLink}`, { responseType: 'arraybuffer' }).subscribe({
      next: data => {
        const url = window.URL.createObjectURL(new Blob([data]));
        const link = document.createElement('a');
        link.style.display = 'none';
        link.href = url;
        link.download = attachment.label ?? 'download';
        document.body.appendChild(link);
        link.click();
        link.remove();
        window.URL.revokeObjectURL(url);
        this.downloading.set(false);
      },
      error: () => this.downloading.set(false)
    });
  }
}
