import { DatePipe } from '@angular/common';
import { Component, input, model } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideExternalLink } from '@lucide/angular';
import { ChannelIconComponent, MessageTypeBadgeComponent } from 'src/app/ui/badges/badges';
import { contentChannel } from '../../../campaign-utils';
import { CampaignPreview } from './campaign-preview';

/** Wizard step 5: review everything and choose whether to publish right away. */
@Component({
  selector: 'app-campaign-preview',
  imports: [DatePipe, FormsModule, TranslatePipe, ChannelIconComponent, MessageTypeBadgeComponent, LucideExternalLink],
  template: `
    <div class="grid gap-6 lg:grid-cols-[1fr_20rem]">
      <dl class="grid gap-x-6 gap-y-5 sm:grid-cols-2">
        <div class="sm:col-span-2">
          <dt class="text-xs text-base-content/60">{{ 'Campaigns.TitleLabel' | translate }}</dt>
          <dd class="text-lg font-semibold">{{ data().title || ('Campaigns.EmptyValueIndicator' | translate) }}</dd>
        </div>
        <div>
          <dt class="text-xs text-base-content/60">{{ 'Campaigns.TypeLabel' | translate }}</dt>
          <dd class="mt-1"><app-message-type-badge [name]="data().type" [classification]="data().typeClassification" /></dd>
        </div>
        <div>
          <dt class="text-xs text-base-content/60">{{ 'Campaigns.DurationLabel' | translate }}</dt>
          <dd class="font-medium tabular">
            {{ data().period?.from ? (data().period?.from | date: 'medium') : '' }} –
            {{ data().period?.to ? (data().period?.to | date: 'medium') : ('Campaigns.InfiniteSymbol' | translate) }}
          </dd>
        </div>
        <div>
          <dt class="text-xs text-base-content/60">{{ 'Campaigns.CommunicationChannelsLabel' | translate }}</dt>
          <dd class="mt-1 flex flex-wrap gap-3">
            @for (key of data().channels ?? []; track key) {
              <app-channel-icon [kind]="kindOf(key)" [label]="true" />
            } @empty {
              <span class="text-base-content/40">—</span>
            }
          </dd>
        </div>
        <div>
          <dt class="text-xs text-base-content/60">{{ 'Campaigns.CallToActionSectionLabel' | translate }}</dt>
          <dd class="font-medium">
            @if (data().actionLink?.text && data().actionLink?.href) {
              <a class="link link-primary inline-flex items-center gap-1" target="_blank" rel="noopener" [href]="data().actionLink?.href">
                {{ data().actionLink?.text }}<svg lucideExternalLink class="size-3.5"></svg>
              </a>
            } @else {
              {{ data().actionLink?.text || ('Campaigns.EmptyValueIndicator' | translate) }}
            }
          </dd>
        </div>
        <div>
          <dt class="text-xs text-base-content/60">{{ 'Campaigns.TemplateLabel' | translate }}</dt>
          <dd class="font-medium">{{ data().template || ('Campaigns.EmptyValueIndicator' | translate) }}</dd>
        </div>
        <div>
          <dt class="text-xs text-base-content/60">{{ 'Campaigns.AttachmentsLabel' | translate }}</dt>
          <dd class="font-medium break-all">{{ data().attachment || ('Campaigns.EmptyValueIndicator' | translate) }}</dd>
        </div>
        <div class="sm:col-span-2">
          <dt class="text-xs text-base-content/60">{{ 'Campaigns.RecipientsLabel' | translate }}</dt>
          <dd class="font-medium">
            {{ (data().recipientsMode ?? 'Campaigns.EmptyValueIndicator') | translate }}
            @if (data().recipientsDetail) { <span class="text-base-content/70">· {{ data().recipientsDetail }}</span> }
          </dd>
        </div>
      </dl>

      <div class="space-y-3">
        <label class="flex cursor-pointer items-start justify-between gap-4 rounded-box border border-base-300 p-4 has-checked:border-success has-checked:bg-success/5">
          <span>
            <span class="block text-sm font-semibold">{{ 'Campaigns.PublishedLabel' | translate }}</span>
            <span class="block text-xs text-base-content/60">{{ 'Campaigns.PublishedHint' | translate }}</span>
          </span>
          <input type="checkbox" class="toggle toggle-success" [(ngModel)]="published" />
        </label>
        <label class="flex cursor-pointer items-start justify-between gap-4 rounded-box border border-base-300 p-4 has-checked:border-warning has-checked:bg-warning/5">
          <span>
            <span class="block text-sm font-semibold">{{ 'Campaigns.IgnoreUserPreferencesLabel' | translate }}</span>
            <span class="block text-xs text-base-content/60">{{ 'Campaigns.IgnoreUserPreferencesHint' | translate }}</span>
          </span>
          <input type="checkbox" class="toggle toggle-warning" [(ngModel)]="ignoreUserPreferences" />
        </label>
      </div>
    </div>
  `
})
export class CampaignPreviewComponent {
  public readonly data = input.required<CampaignPreview>();
  public readonly published = model(false);
  public readonly ignoreUserPreferences = model(false);

  protected kindOf(key: string): string {
    return contentChannel(key)?.kind ?? key;
  }
}
