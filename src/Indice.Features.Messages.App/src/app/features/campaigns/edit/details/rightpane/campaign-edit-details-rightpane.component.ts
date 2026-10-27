import { Component, OnInit, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import {
  CampaignDetails, DistributionList, Hyperlink, MessageContent, MessageSender, MessagesApiClient, MessageType, Period
} from 'src/app/core/services/messages-api.service';
import { SettingsStore } from 'src/app/features/settings/settings-store.service';
import { AppTranslatedToaster, ToastType } from 'src/app/shared/services/app-translated-toaster';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { SidePaneLayoutComponent, SidePaneService } from 'src/app/ui/side-pane/side-pane';
import { URL_PATTERN, toLocalInput } from '../../../campaign-utils';
import { PagedLookup } from '../../../shared/paged-lookup';
import { SearchSelectComponent, SearchSelectItem } from '../../../shared/search-select.component';
import { CampaignEditStore } from '../../campaign-edit-store.service';

export type CampaignEditAction = 'editTitle' | 'editType' | 'editSender' | 'editActivePeriod' | 'editCta' | 'editList';

/** Side pane that edits one aspect (`action`) of a draft campaign. */
@Component({
  selector: 'app-campaign-details-edit-rightpane',
  imports: [ReactiveFormsModule, TranslatePipe, SidePaneLayoutComponent, SearchSelectComponent],
  template: `
    <app-side-pane-layout [title]="'Campaigns.EditCampaignTitle' | translate" [subtitle]="campaign()?.title">
      @if (!campaign()) {
        <div class="space-y-4">
          @for (row of [1, 2]; track row) { <div class="skeleton h-10 w-full"></div> }
        </div>
      } @else {
        <form id="campaign-edit-form" class="space-y-4" [formGroup]="form" (ngSubmit)="submit()">
          @switch (action()) {
            @case ('editType') {
              <fieldset class="fieldset">
                <legend class="fieldset-legend">{{ 'Campaigns.TypeLabel' | translate }}</legend>
                <select class="select w-full" formControlName="typeId">
                  <option value="">{{ 'Campaigns.SelectPlaceholder' | translate }}</option>
                  @for (type of messageTypes(); track type.id) {
                    <option [value]="type.id">{{ type.name }}</option>
                  }
                </select>
                <p class="label whitespace-normal">{{ 'Campaigns.TypeInfo' | translate }}</p>
              </fieldset>
            }
            @case ('editSender') {
              <fieldset class="fieldset">
                <legend class="fieldset-legend">{{ 'Campaigns.SenderLabel' | translate }}</legend>
                <select class="select w-full" formControlName="senderId">
                  <option value="" disabled>{{ 'Campaigns.SelectPlaceholder' | translate }}</option>
                  @for (sender of senders(); track sender.id) {
                    <option [value]="sender.id">{{ sender.displayName }} &lt;{{ sender.sender }}&gt;</option>
                  }
                </select>
                <p class="label whitespace-normal">{{ 'Campaigns.SenderInfo' | translate }}</p>
              </fieldset>
            }
            @case ('editActivePeriod') {
              <fieldset class="fieldset">
                <legend class="fieldset-legend">{{ 'Campaigns.StartDateLabel' | translate }}</legend>
                <input type="datetime-local" class="input w-full" formControlName="from" [min]="now" [max]="form.controls.to.value || null" />
                <p class="label whitespace-normal">{{ 'Campaigns.StartDateInfo' | translate }}</p>
              </fieldset>
              <fieldset class="fieldset">
                <legend class="fieldset-legend">{{ 'Campaigns.EndDateLabel' | translate }}</legend>
                <input type="datetime-local" class="input w-full" formControlName="to" [min]="form.controls.from.value || null"
                  [readonly]="!form.controls.from.value" />
                <p class="label whitespace-normal">{{ 'Campaigns.EndDateInfo' | translate }}</p>
              </fieldset>
            }
            @case ('editCta') {
              <fieldset class="fieldset">
                <legend class="fieldset-legend">{{ 'Campaigns.CallToActionTextLabel' | translate }}</legend>
                <input class="input w-full" formControlName="actionLinkText" maxlength="128" [placeholder]="'Campaigns.CallToActionTextPlaceholder' | translate" />
                <p class="label whitespace-normal">{{ 'Campaigns.CallToActionTextInfo' | translate }}</p>
              </fieldset>
              <fieldset class="fieldset">
                <legend class="fieldset-legend">{{ 'Campaigns.CallToActionLinkLabel' | translate }}</legend>
                <input type="url" class="input w-full" formControlName="actionLinkHref" maxlength="2048"
                  [placeholder]="'Campaigns.CallToActionLinkPlaceholder' | translate"
                  [class.input-error]="form.controls.actionLinkHref.invalid && form.controls.actionLinkHref.touched" />
                @if (form.controls.actionLinkHref.touched && form.controls.actionLinkHref.hasError('pattern')) {
                  <p class="label text-error">{{ 'Campaigns.CallToActionLinkInvalidError' | translate }}</p>
                } @else {
                  <p class="label whitespace-normal">{{ 'Campaigns.CallToActionLinkInfo' | translate }}</p>
                }
              </fieldset>
            }
            @case ('editList') {
              <fieldset class="fieldset">
                <legend class="fieldset-legend">{{ 'Campaigns.DistributionListLabel' | translate }}</legend>
                <app-search-select [items]="lists.items()" [loading]="lists.loading()" [hasMore]="lists.hasMore()"
                  [placeholder]="'Campaigns.SelectPlaceholder' | translate" [selected]="form.controls.distributionList.value"
                  (selectedChange)="form.controls.distributionList.setValue($event); form.markAsDirty()"
                  (search)="lists.search($event)" (showMore)="lists.more()" />
                <p class="label whitespace-normal">{{ 'Campaigns.DistributionListInfo' | translate }}</p>
              </fieldset>
            }
            @default {
              <fieldset class="fieldset">
                <legend class="fieldset-legend">{{ 'Campaigns.TitleLabelRequired' | translate }}</legend>
                <input class="input w-full" formControlName="title" maxlength="128" [placeholder]="'Campaigns.TitlePlaceholder' | translate"
                  [class.input-error]="form.controls.title.invalid && form.controls.title.touched" />
                @if (form.controls.title.touched && form.controls.title.hasError('required')) {
                  <p class="label text-error">{{ 'Campaigns.TitleRequiredError' | translate }}</p>
                } @else {
                  <p class="label whitespace-normal">{{ 'Campaigns.TitleInfo' | translate }}</p>
                }
              </fieldset>
            }
          }
        </form>
      }
      <div footer class="flex justify-end gap-2 border-t border-base-300 px-6 py-4">
        <button type="button" class="btn btn-ghost" (click)="pane.close()">{{ 'Campaigns.Close' | translate }}</button>
        <button type="submit" form="campaign-edit-form" class="btn btn-primary" [disabled]="form.invalid || saving() || !campaign()">
          @if (saving()) { <span class="loading loading-spinner loading-sm"></span> }
          {{ 'Campaigns.Save' | translate }}
        </button>
      </div>
    </app-side-pane-layout>
  `
})
export class CampaignDetailsEditRightpaneComponent implements OnInit {
  private readonly _api = inject(MessagesApiClient);
  private readonly _store = inject(CampaignEditStore);
  private readonly _settings = inject(SettingsStore);
  private readonly _toaster = inject(AppTranslatedToaster);
  private readonly _refresh = inject(RefreshBus);
  protected readonly pane = inject(SidePaneService);

  /** Matrix params of the `edit-campaign` pane route. */
  public readonly campaignId = input.required<string>();
  public readonly action = input<CampaignEditAction>('editTitle');

  protected readonly now = toLocalInput(new Date());
  protected readonly campaign = signal<CampaignDetails | undefined>(undefined);
  protected readonly saving = signal(false);
  protected readonly messageTypes = signal<MessageType[]>([]);
  protected readonly senders = signal<MessageSender[]>([]);

  protected readonly form = inject(NonNullableFormBuilder).group({
    title: ['', [Validators.required, Validators.maxLength(128)]],
    typeId: [''],
    senderId: [''],
    from: [''],
    to: [''],
    actionLinkText: ['', Validators.maxLength(128)],
    actionLinkHref: ['', [Validators.pattern(URL_PATTERN), Validators.maxLength(2048)]],
    distributionList: [null as SearchSelectItem<DistributionList> | null]
  });

  protected readonly lists = new PagedLookup<DistributionList>(
    (page, size, term) => this._api.getDistributionLists(page, size, 'name', term || undefined),
    list => ({ id: list.id!, label: list.name ?? '', data: list })
  );

  public ngOnInit(): void {
    this._store.getCampaign(this.campaignId()).subscribe({
      next: campaign => {
        this.campaign.set(campaign);
        this.form.patchValue({
          title: campaign.title ?? '',
          typeId: campaign.type?.id ?? '',
          senderId: campaign.content?.['email']?.sender?.id ?? '',
          from: toLocalInput(campaign.activePeriod?.from ?? new Date()),
          to: toLocalInput(campaign.activePeriod?.to),
          actionLinkText: campaign.actionLink?.text ?? '',
          actionLinkHref: campaign.actionLink?.href ?? '',
          distributionList: campaign.distributionList?.id
            ? { id: campaign.distributionList.id, label: campaign.distributionList.name ?? '', data: campaign.distributionList }
            : null
        });
        this.loadLookups();
      },
      error: () => this.pane.close()
    });
  }

  protected submit(): void {
    const current = this.campaign();
    if (!current || this.form.invalid) {
      return;
    }
    const value = this.form.getRawValue();
    const model = new CampaignDetails(current);
    switch (this.action()) {
      case 'editType': {
        const type = this.messageTypes().find(t => t.id === value.typeId);
        model.type = type ? new MessageType(type) : undefined;
        break;
      }
      case 'editSender': {
        const email = current.content?.['email'];
        if (email) {
          const sender = this.senders().find(s => s.id === value.senderId);
          // The default sender is not stored on the campaign: the server falls back to it.
          const chosen = sender && !sender.isDefault ? new MessageSender({ id: sender.id, sender: sender.sender, displayName: sender.displayName }) : undefined;
          model.content = { ...current.content, email: new MessageContent({ ...email, sender: chosen }) };
        }
        break;
      }
      case 'editActivePeriod':
        model.activePeriod = new Period({ from: value.from ? new Date(value.from) : undefined, to: value.to ? new Date(value.to) : undefined });
        break;
      case 'editCta':
        model.actionLink = new Hyperlink({ text: value.actionLinkText, href: value.actionLinkHref });
        break;
      case 'editList':
        if (value.distributionList) {
          model.distributionList = new DistributionList({ id: value.distributionList.id, name: value.distributionList.label });
          model.isGlobal = false;
        }
        break;
      default:
        model.title = value.title;
    }
    this.saving.set(true);
    this._store.updateCampaign(this.campaignId(), model).subscribe({
      next: () => {
        this._toaster.show(ToastType.Success, 'Campaigns.UpdateSuccessTitle', 'Campaigns.UpdateSuccessMessage', undefined, { title: model.title });
        this._refresh.notify('campaign');
        this.pane.close();
      },
      error: () => this.saving.set(false)
    });
  }

  private loadLookups(): void {
    switch (this.action()) {
      case 'editType':
        this._api.getMessageTypes(1, 100, 'name+').subscribe(result => this.messageTypes.set(result.items ?? []));
        break;
      case 'editSender':
        this._settings.getMessageSenders().subscribe(result => {
          const senders = result.items ?? [];
          this.senders.set(senders);
          if (!this.form.controls.senderId.value) {
            this.form.controls.senderId.setValue(senders.find(s => s.isDefault)?.id ?? '');
          }
        });
        break;
    }
  }
}
