import { DatePipe } from '@angular/common';
import { Component, effect, inject, input, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { filter, switchMap } from 'rxjs';
import { LucideArrowLeft, LucideCalendarRange, LucideChartPie, LucideCode, LucideInfo, LucideRocket, LucideTrash, LucideUsers } from '@lucide/angular';
import { MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { AppTranslatedToaster, ToastType } from 'src/app/shared/services/app-translated-toaster';
import { MessageTypeBadgeComponent } from 'src/app/ui/badges/badges';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';
import { RouteTab, RouteTabsComponent } from 'src/app/ui/tabs/route-tabs.component';
import { CampaignEditStore } from './campaign-edit-store.service';

/** Campaign detail page: header with status and actions, then the details / content / reports / messages tabs. */
@Component({
  selector: 'app-campaign-edit',
  imports: [
    DatePipe, RouterLink, RouterOutlet, TranslatePipe, PageHeaderComponent, RouteTabsComponent, MessageTypeBadgeComponent,
    LucideArrowLeft, LucideCalendarRange, LucideRocket, LucideTrash
  ],
  template: `
    @let campaign = store.campaign();
    <app-page-header [title]="campaign?.title ?? ''">
      <a leading class="btn btn-ghost btn-xs -ml-2 mb-2" routerLink="/campaigns">
        <svg lucideArrowLeft class="size-4"></svg>{{ 'Campaigns.Title' | translate }}
      </a>
      @if (campaign) {
        <div class="mt-2 flex flex-wrap items-center gap-2 text-sm">
          @if (campaign.published) {
            <span class="badge badge-sm badge-soft badge-success">{{ 'Campaigns.StatusPublished' | translate }}</span>
          } @else {
            <span class="badge badge-sm badge-ghost">{{ 'Campaigns.StatusDraft' | translate }}</span>
          }
          <app-message-type-badge [name]="campaign.type?.name" [classification]="campaign.type?.classification" />
          @if (campaign.activePeriod?.from) {
            <span class="inline-flex items-center gap-1.5 text-base-content/70 tabular">
              <svg lucideCalendarRange class="size-4"></svg>
              {{ campaign.activePeriod?.from | date: 'medium' }} –
              {{ campaign.activePeriod?.to ? (campaign.activePeriod?.to | date: 'medium') : ('Campaigns.InfiniteSymbol' | translate) }}
            </span>
          }
        </div>
      } @else {
        <div class="skeleton mt-2 h-5 w-64"></div>
      }
      <div actions>
        @if (campaign && !campaign.published) {
          <button type="button" class="btn btn-success btn-sm" [disabled]="busy()" (click)="publish()">
            @if (busy()) { <span class="loading loading-spinner loading-xs"></span> } @else { <svg lucideRocket class="size-4"></svg> }
            {{ 'Campaigns.Publish' | translate }}
          </button>
        }
        <button type="button" class="btn btn-ghost btn-sm text-error" [disabled]="!campaign || busy()" (click)="delete()">
          <svg lucideTrash class="size-4"></svg>{{ 'Campaigns.Delete' | translate }}
        </button>
      </div>
    </app-page-header>

    <app-route-tabs class="mb-6" [tabs]="tabs" />
    <router-outlet />
  `
})
export class CampaignEditComponent {
  private readonly _api = inject(MessagesApiClient);
  private readonly _router = inject(Router);
  private readonly _dialog = inject(DialogService);
  private readonly _toaster = inject(AppTranslatedToaster);
  private readonly _refresh = inject(RefreshBus);
  protected readonly store = inject(CampaignEditStore);

  /** Bound from the `:campaignId` route param. */
  public readonly campaignId = input.required<string>();

  protected readonly busy = signal(false);
  protected readonly tabs: RouteTab[] = [
    { label: 'Campaigns.BasicInformationNav', link: 'campaign-details', icon: LucideInfo },
    { label: 'Campaigns.ContentNav', link: 'campaign-content', icon: LucideCode },
    { label: 'Campaigns.ReportsNav', link: 'campaign-reports', icon: LucideChartPie },
    { label: 'Campaigns.RecipientsNav', link: 'campaign-messages', icon: LucideUsers }
  ];

  constructor() {
    effect(() => {
      const id = this.campaignId();
      untracked(() => this.reload(id));
    });
    this._refresh.on('campaign').pipe(takeUntilDestroyed()).subscribe(() => this.reload(this.campaignId()));
  }

  private reload(id: string): void {
    this.store.invalidate();
    this.store.getCampaign(id).subscribe();
  }

  protected publish(): void {
    const campaign = this.store.campaign();
    if (!campaign?.id) {
      return;
    }
    this._dialog
      .confirm({ title: 'Campaigns.Publish', message: 'Campaigns.PublishConfirmMessage', messageParams: { title: campaign.title }, confirmLabel: 'Campaigns.Publish' })
      .pipe(filter(Boolean), switchMap(() => {
        this.busy.set(true);
        return this.store.publishCampaign(campaign.id!);
      }))
      .subscribe({
        next: () => {
          this.busy.set(false);
          this._toaster.show(ToastType.Success, 'Campaigns.PublishSuccessTitle', 'Campaigns.PublishSuccessMessage', undefined, { title: campaign.title });
          this._refresh.notify('campaign');
        },
        error: () => this.busy.set(false)
      });
  }

  protected delete(): void {
    const campaign = this.store.campaign();
    if (!campaign?.id) {
      return;
    }
    this._dialog
      .confirm({ title: 'Campaigns.Delete', message: 'Campaigns.DeleteConfirmMessage', messageParams: { title: campaign.title }, confirmLabel: 'Common.Delete', tone: 'danger' })
      .pipe(filter(Boolean), switchMap(() => {
        this.busy.set(true);
        return this._api.deleteCampaign(campaign.id!);
      }))
      .subscribe({
        next: () => {
          this.store.invalidate();
          this._toaster.show(ToastType.Success, 'Campaigns.DeleteSuccessTitle', 'Campaigns.DeleteSuccessMessage', undefined, { title: campaign.title });
          this._refresh.notify('campaigns');
          this._router.navigate(['/campaigns']);
        },
        error: () => this.busy.set(false)
      });
  }
}
