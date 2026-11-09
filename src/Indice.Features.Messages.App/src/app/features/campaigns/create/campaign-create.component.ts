import { Component, computed, inject, signal, viewChild } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { of, switchMap, map } from 'rxjs';
import {
  LucideArrowLeft, LucideCheck, LucideChevronLeft, LucideChevronRight, LucideCode, LucideDynamicIcon, LucideFileText, LucideIcon,
  LucideListChecks, LucidePaperclip, LucideRocket, LucideTriangleAlert, LucideUsers
} from '@lucide/angular';
import { ContactAnonymous, CreateCampaignRequest, Hyperlink, MessageContent, MessagesApiClient, Period } from 'src/app/core/services/messages-api.service';
import { AppTranslatedToaster, ToastType } from 'src/app/shared/services/app-translated-toaster';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';
import { CampaignAttachmentsComponent } from './steps/attachments/campaign-attachments.component';
import { CampaignBasicInfoComponent } from './steps/basic-info/campaign-basic-info.component';
import { CampaignContentComponent } from './steps/content/campaign-content.component';
import { CampaignPreview } from './steps/preview/campaign-preview';
import { CampaignPreviewComponent } from './steps/preview/campaign-preview.component';
import { CampaignRecipientsComponent } from './steps/recipients/campaign-recipients.component';

interface WizardStep {
  label: string;
  info: string;
  icon: LucideIcon;
}

/** Five step wizard that creates (and optionally publishes) a campaign. */
@Component({
  selector: 'app-campaign-create',
  imports: [
    RouterLink, TranslatePipe, PageHeaderComponent, LucideDynamicIcon, LucideArrowLeft, LucideCheck, LucideChevronLeft, LucideChevronRight,
    LucideRocket, LucideTriangleAlert, CampaignBasicInfoComponent, CampaignContentComponent, CampaignAttachmentsComponent,
    CampaignRecipientsComponent, CampaignPreviewComponent
  ],
  template: `
    <app-page-header [title]="'Campaigns.CreateTitle' | translate" [subtitle]="'Campaigns.CreateWizardIntro' | translate">
      <a leading class="btn btn-ghost btn-xs -ml-2 mb-2" routerLink="/campaigns">
        <svg lucideArrowLeft class="size-4"></svg>{{ 'Campaigns.Title' | translate }}
      </a>
    </app-page-header>

    <div class="panel">
      <div class="overflow-x-auto border-b border-base-300 px-4 py-5">
        <ul class="steps w-full min-w-[36rem]">
          @for (step of steps; track step.label; let i = $index) {
            <li class="step" [class.step-primary]="i <= current()" [attr.data-content]="i < current() ? '✓' : i + 1">
              <button type="button" class="flex flex-col items-center gap-0.5 rounded-field px-2 py-1 text-sm hover:bg-base-200"
                [class.font-semibold]="i === current()" [attr.aria-current]="i === current() ? 'step' : null" (click)="goTo(i)">
                {{ step.label | translate }}
              </button>
            </li>
          }
        </ul>
      </div>

      <div class="p-5 sm:p-6">
        <div class="mb-6 flex items-start gap-3">
          <span class="grid size-10 shrink-0 place-items-center rounded-full bg-primary/10 text-primary">
            <svg [lucideIcon]="steps[current()].icon" class="size-5"></svg>
          </span>
          <div>
            <h2 class="text-lg font-semibold">{{ steps[current()].label | translate }}</h2>
            <p class="text-sm text-base-content/70">{{ steps[current()].info | translate }}</p>
          </div>
        </div>

        @if (showChannelsError()) {
          <div role="alert" class="alert alert-soft alert-error mb-4 text-sm">
            <svg lucideTriangleAlert class="size-4"></svg>
            <span>{{ 'Campaigns.ChannelsRequiredError' | translate }}</span>
          </div>
        }

        <app-campaign-basic-info [hidden]="current() !== 0" (templateSelected)="onTemplateSelected($event)" />
        <app-campaign-content [hidden]="current() !== 1" [additionalData]="sampleData()" [content]="templateContent()" [data]="templateData()" />
        <app-campaign-attachments [hidden]="current() !== 2" />
        <app-campaign-recipients [hidden]="current() !== 3" />
        <app-campaign-preview [hidden]="current() !== 4" [data]="previewData()" [(published)]="published" [(ignoreUserPreferences)]="ignoreUserPreferences" />
      </div>

      <div class="sticky bottom-0 flex items-center gap-2 rounded-b-box border-t border-base-300 bg-base-100 px-5 py-4 sm:px-6">
        @if (current() > 0) {
          <button type="button" class="btn btn-ghost" [disabled]="saving()" (click)="back()">
            <svg lucideChevronLeft class="size-4"></svg>{{ 'Campaigns.Previous' | translate }}
          </button>
        }
        <span class="ml-auto text-sm text-base-content/60 tabular">{{ current() + 1 }} / {{ steps.length }}</span>
        @if (isLast()) {
          <button type="button" class="btn" [class]="published() ? 'btn-success' : 'btn-primary'" [disabled]="saving()" (click)="submit()">
            @if (saving()) { <span class="loading loading-spinner loading-sm"></span> }
            @else if (published()) { <svg lucideRocket class="size-4"></svg> }
            @else { <svg lucideCheck class="size-4"></svg> }
            {{ (published() ? 'Campaigns.SaveAndPublish' : 'Campaigns.Save') | translate }}
          </button>
        } @else {
          <button type="button" class="btn btn-primary" [disabled]="loadingTemplate()" (click)="next()">
            @if (loadingTemplate()) { <span class="loading loading-spinner loading-sm"></span> }
            {{ 'Campaigns.Next' | translate }}<svg lucideChevronRight class="size-4"></svg>
          </button>
        }
      </div>
    </div>
  `
})
export class CampaignCreateComponent {
  private readonly _api = inject(MessagesApiClient);
  private readonly _router = inject(Router);
  private readonly _toaster = inject(AppTranslatedToaster);
  private readonly _refresh = inject(RefreshBus);
  private readonly _basicInfo = viewChild.required(CampaignBasicInfoComponent);
  private readonly _content = viewChild.required(CampaignContentComponent);
  private readonly _attachments = viewChild.required(CampaignAttachmentsComponent);
  private readonly _recipients = viewChild.required(CampaignRecipientsComponent);

  protected readonly steps: WizardStep[] = [
    { label: 'Campaigns.GeneralLabel', info: 'Campaigns.GeneralInfo', icon: LucideFileText },
    { label: 'Campaigns.ContentLabel', info: 'Campaigns.ContentInfo', icon: LucideCode },
    { label: 'Campaigns.AttachmentsLabel', info: 'Campaigns.AttachmentsInfo', icon: LucidePaperclip },
    { label: 'Campaigns.RecipientsLabel', info: 'Campaigns.RecipientsInfo', icon: LucideUsers },
    { label: 'Campaigns.OverviewLabel', info: 'Campaigns.OverviewInfo', icon: LucideListChecks }
  ];
  protected readonly current = signal(0);
  protected readonly isLast = computed(() => this.current() === this.steps.length - 1);
  protected readonly saving = signal(false);
  protected readonly loadingTemplate = signal(false);
  protected readonly showChannelsError = signal(false);
  protected readonly published = signal(false);
  protected readonly ignoreUserPreferences = signal(false);
  protected readonly previewData = signal<CampaignPreview>({});
  protected readonly sampleData = signal<Record<string, any>>({});
  protected readonly templateContent = signal<{ [key: string]: MessageContent } | undefined>(undefined);
  protected readonly templateData = signal<any>(undefined);
  private _templateId?: string;
  private _templateLoaded = false;

  protected onTemplateSelected(templateId: string | undefined): void {
    this._templateId = templateId;
    this._templateLoaded = false;
  }

  protected next(): void {
    this.goTo(this.current() + 1);
  }

  protected back(): void {
    this.goTo(this.current() - 1);
  }

  /** Moves to a step; moving forward requires every step before it to be valid. */
  protected goTo(index: number): void {
    if (index < 0 || index >= this.steps.length || index === this.current()) {
      return;
    }
    if (index > this.current()) {
      for (let step = 0; step < index; step++) {
        if (!this.validate(step)) {
          this.current.set(step);
          return;
        }
      }
    }
    this.showChannelsError.set(false);
    this.syncPreview();
    if (index === 1 && this._templateId && !this._templateLoaded) {
      this.loadTemplate(this._templateId, () => this.current.set(index));
      return;
    }
    this.current.set(index);
  }

  protected submit(): void {
    for (let step = 0; step < this.steps.length; step++) {
      if (!this.validate(step)) {
        this.current.set(step);
        return;
      }
    }
    const request = this.buildRequest();
    const attachment = this._attachments().attachment();
    this.saving.set(true);
    this._api.createCampaign(request)
      .pipe(switchMap(result => attachment && result.campaignId
        ? this._api.uploadCampaignAttachment(result.campaignId, attachment).pipe(map(() => result))
        : of(result)))
      .subscribe({
        next: result => {
          this._toaster.show(ToastType.Success, 'Campaigns.CreateSuccessTitle', 'Campaigns.CreateSuccessMessage', undefined, { title: request.title });
          this._refresh.notify('campaigns');
          this._router.navigate(['campaigns', result.campaignId]);
        },
        error: () => this.saving.set(false)
      });
  }

  private validate(step: number): boolean {
    switch (step) {
      case 0: return this.check(this._basicInfo().form);
      case 1: {
        const content = this._content();
        const hasChannels = content.channelsContent.length > 0;
        this.showChannelsError.set(!hasChannels);
        return this.check(content.form) && hasChannels;
      }
      case 3: return this.check(this._recipients().form);
      default: return true;
    }
  }

  private check(form: { valid: boolean; markAllAsTouched(): void }): boolean {
    if (!form.valid) {
      form.markAllAsTouched();
    }
    return form.valid;
  }

  private loadTemplate(templateId: string, done: () => void): void {
    this.loadingTemplate.set(true);
    this._api.getTemplateById(templateId).subscribe({
      next: template => {
        this.templateContent.set(template.content);
        this.templateData.set(template.data);
        this._templateLoaded = true;
        this.loadingTemplate.set(false);
        done();
      },
      error: () => this.loadingTemplate.set(false)
    });
  }

  private syncPreview(): void {
    const basic = this._basicInfo().form.getRawValue();
    const recipients = this._recipients();
    const sendVia = recipients.form.controls.sendVia.value;
    const actionLink = new Hyperlink({ text: basic.actionLinkText, href: basic.actionLinkHref });
    const mode = recipients.options.find(o => o.value === sendVia)?.label;
    const detail = sendVia === 'distribution-list' ? recipients.distributionListName
      : sendVia === 'recipient-ids' ? `${recipients.recipientIds?.length ?? 0}`
      : sendVia === 'recipients' ? `${recipients.form.controls.recipients.value?.length ?? 0}`
      : undefined;
    this.sampleData.set({
      title: basic.title,
      type: basic.type?.label,
      actionLink: basic.actionLinkText || basic.actionLinkHref ? actionLink : undefined
    });
    this.previewData.set({
      title: basic.title,
      type: basic.type?.label,
      typeClassification: basic.type?.data?.classification,
      template: basic.template?.label,
      period: new Period({ from: basic.from ? new Date(basic.from) : undefined, to: basic.to ? new Date(basic.to) : undefined }),
      actionLink,
      channels: this._content().channelsContent.controls.map(c => c.controls.channel.value),
      recipientsMode: mode,
      recipientsDetail: detail,
      attachment: this._attachments().fileName()
    });
  }

  private buildRequest(): CreateCampaignRequest {
    const basic = this._basicInfo().form.getRawValue();
    const recipients = this._recipients();
    const recipientsValue = recipients.form.getRawValue();
    const content = this._content();
    return new CreateCampaignRequest({
      actionLink: new Hyperlink({ href: basic.actionLinkHref || undefined, text: basic.actionLinkText || undefined }),
      activePeriod: new Period({
        from: basic.from ? new Date(basic.from) : undefined,
        to: basic.to ? new Date(basic.to) : undefined
      }),
      isGlobal: recipientsValue.sendVia === 'user-base',
      published: this.published(),
      ignoreUserPreferences: this.ignoreUserPreferences(),
      title: basic.title,
      data: JSON.parse(content.form.controls.data.value || '{}'),
      mediaBaseHref: content.mediaBaseHref(),
      typeId: basic.type?.id || undefined,
      recipientIds: recipients.recipientIds ?? undefined,
      recipientListId: recipientsValue.distributionList || undefined,
      recipients: recipientsValue.recipients?.map(c => new ContactAnonymous({
        recipientId: c.recipientId, salutation: c.salutation, firstName: c.firstName, lastName: c.lastName,
        fullName: c.fullName, email: c.email, phoneNumber: c.phoneNumber
      })),
      content: content.buildContent()
    });
  }
}
