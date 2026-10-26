import { Component, computed, inject, signal, viewChild } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideLock, LucideSave } from '@lucide/angular';
import { CampaignDetails, Hyperlink } from 'src/app/core/services/messages-api.service';
import { AppTranslatedToaster, ToastType } from 'src/app/shared/services/app-translated-toaster';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { CampaignContentComponent } from '../../create/steps/content/campaign-content.component';
import { CampaignEditStore } from '../campaign-edit-store.service';

/** Content tab: edit the per channel content of a draft campaign (Ctrl+S saves). */
@Component({
  selector: 'app-campaign-content-edit',
  imports: [TranslatePipe, CampaignContentComponent, LucideLock, LucideSave],
  host: { '(document:keydown.control.s)': 'onSaveShortcut($event)' },
  template: `
    @let campaign = store.campaign();
    <section class="panel">
      <div class="flex flex-col gap-3 border-b border-base-300 px-5 py-4 sm:flex-row sm:items-center sm:justify-between sm:px-6">
        <div>
          <h2 class="text-lg font-semibold">{{ 'Campaigns.ContentLabel' | translate }}</h2>
          <p class="text-sm text-base-content/70">{{ 'Campaigns.ContentDescription' | translate }}</p>
        </div>
        @if (campaign && !campaign.published) {
          <button type="button" class="btn btn-primary btn-sm" [disabled]="!canSave() || saving()" (click)="save()">
            @if (saving()) { <span class="loading loading-spinner loading-xs"></span> } @else { <svg lucideSave class="size-4"></svg> }
            {{ 'Campaigns.Save' | translate }} <kbd class="kbd kbd-xs hidden sm:inline-flex">Ctrl S</kbd>
          </button>
        } @else if (campaign) {
          <span class="badge badge-ghost gap-1.5"><svg lucideLock class="size-3.5"></svg>{{ 'Campaigns.StatusPublished' | translate }}</span>
        }
      </div>
      <div class="p-5 sm:p-6">
        @if (campaign) {
          <app-campaign-content [additionalData]="sampleData()" [content]="campaign.content" [data]="campaign.data" [readonly]="!!campaign.published" />
        } @else {
          <div class="space-y-4">
            <div class="skeleton h-10 w-full"></div>
            <div class="skeleton h-64 w-full"></div>
          </div>
        }
      </div>
    </section>
  `
})
export class CampaignContentEditComponent {
  private readonly _toaster = inject(AppTranslatedToaster);
  private readonly _refresh = inject(RefreshBus);
  private readonly _content = viewChild(CampaignContentComponent);
  protected readonly store = inject(CampaignEditStore);

  protected readonly saving = signal(false);
  protected readonly sampleData = computed(() => {
    const campaign = this.store.campaign();
    return {
      title: campaign?.title,
      type: campaign?.type?.name,
      actionLink: campaign?.actionLink?.text || campaign?.actionLink?.href ? new Hyperlink(campaign.actionLink) : undefined,
      mediaBaseHref: campaign?.mediaBaseHref
    };
  });

  protected canSave(): boolean {
    const content = this._content();
    return !!content && content.form.valid && content.channelsContent.length > 0;
  }

  protected onSaveShortcut(event: Event): void {
    event.preventDefault();
    if (this.store.campaign() && !this.store.campaign()!.published) {
      this.save();
    }
  }

  protected save(): void {
    const campaign = this.store.campaign();
    const content = this._content();
    if (!campaign?.id || !content || !this.canSave() || this.saving()) {
      return;
    }
    const model = new CampaignDetails(campaign);
    model.mediaBaseHref = content.mediaBaseHref();
    model.content = content.buildContent();
    model.data = content.form.controls.data.value || null;
    this.saving.set(true);
    this.store.updateCampaign(campaign.id, model).subscribe({
      next: () => {
        this.saving.set(false);
        this._toaster.show(ToastType.Success, 'Campaigns.ContentUpdateSuccessTitle', 'Campaigns.ContentUpdateSuccessMessage', undefined, { title: model.title });
        this._refresh.notify('campaign');
      },
      error: () => this.saving.set(false)
    });
  }
}
