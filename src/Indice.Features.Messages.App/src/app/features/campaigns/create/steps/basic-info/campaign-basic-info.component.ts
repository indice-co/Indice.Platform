import { Component, inject, output } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { MessagesApiClient, MessageType, TemplateListItem, TemplateType } from 'src/app/core/services/messages-api.service';
import { ChannelIconComponent } from 'src/app/ui/badges/badges';
import { URL_PATTERN, toLocalInput } from '../../../campaign-utils';
import { PagedLookup } from '../../../shared/paged-lookup';
import { SearchSelectComponent, SearchSelectItem } from '../../../shared/search-select.component';

/** Wizard step 1: title, message type, active period, call to action and optional template. */
@Component({
  selector: 'app-campaign-basic-info',
  imports: [ReactiveFormsModule, TranslatePipe, SearchSelectComponent, ChannelIconComponent],
  template: `
    <form class="grid gap-x-6 gap-y-2 md:grid-cols-2" [formGroup]="form">
      <fieldset class="fieldset md:col-span-2">
        <legend class="fieldset-legend">{{ 'Campaigns.TitleLabelRequired' | translate }}</legend>
        <input class="input w-full" formControlName="title" maxlength="128" [placeholder]="'Campaigns.TitlePlaceholder' | translate"
          [class.input-error]="form.controls.title.invalid && form.controls.title.touched" />
        @if (form.controls.title.touched && form.controls.title.hasError('required')) {
          <p class="label text-error">{{ 'Campaigns.TitleRequiredError' | translate }}</p>
        } @else if (form.controls.title.hasError('maxlength')) {
          <p class="label text-error">{{ 'Campaigns.TitleMaxLengthError' | translate }}</p>
        } @else {
          <p class="label whitespace-normal">{{ 'Campaigns.TitleInfo' | translate }}</p>
        }
      </fieldset>

      <fieldset class="fieldset md:col-span-2">
        <legend class="fieldset-legend">{{ 'Campaigns.TypeLabel' | translate }}</legend>
        <app-search-select [items]="types.items()" [loading]="types.loading()" [hasMore]="types.hasMore()"
          [placeholder]="'Campaigns.TypePlaceholder' | translate" [selected]="form.controls.type.value"
          (selectedChange)="onTypeChanged($event)" (search)="types.search($event)" (showMore)="types.more()">
          <ng-template let-item>
            <span class="flex items-center gap-2" [attr.data-class]="item.data?.classification">
              <span class="class-dot"></span>{{ item.label }}
            </span>
          </ng-template>
        </app-search-select>
      </fieldset>

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

      <h3 class="mt-4 text-sm font-semibold md:col-span-2">{{ 'Campaigns.CallToActionSectionLabel' | translate }}</h3>
      <fieldset class="fieldset">
        <legend class="fieldset-legend">{{ 'Campaigns.CallToActionTextLabel' | translate }}</legend>
        <input class="input w-full" formControlName="actionLinkText" maxlength="128" [placeholder]="'Campaigns.CallToActionTextPlaceholder' | translate" />
        <p class="label whitespace-normal">{{ 'Campaigns.CallToActionTextInfo' | translate }}</p>
      </fieldset>
      <fieldset class="fieldset">
        <legend class="fieldset-legend">{{ 'Campaigns.CallToActionLinkLabel' | translate }}</legend>
        <input type="url" class="input w-full" formControlName="actionLinkHref" maxlength="2048" [placeholder]="'Campaigns.CallToActionLinkPlaceholder' | translate"
          [class.input-error]="form.controls.actionLinkHref.invalid && form.controls.actionLinkHref.touched" />
        @if (form.controls.actionLinkHref.touched && form.controls.actionLinkHref.hasError('pattern')) {
          <p class="label text-error">{{ 'Campaigns.CallToActionLinkInvalidError' | translate }}</p>
        } @else {
          <p class="label whitespace-normal">{{ 'Campaigns.CallToActionLinkInfo' | translate }}</p>
        }
      </fieldset>

      <fieldset class="fieldset mt-4 md:col-span-2">
        <legend class="fieldset-legend">{{ 'Campaigns.TemplateUsageLabel' | translate }}</legend>
        <div class="grid gap-4 md:grid-cols-2">
          <div class="join">
            <input type="radio" class="join-item btn btn-sm" formControlName="needsTemplate" value="no"
              [attr.aria-label]="'Campaigns.TemplateUsageNoOption' | translate" (change)="onNeedsTemplateChanged()" />
            <input type="radio" class="join-item btn btn-sm" formControlName="needsTemplate" value="yes"
              [attr.aria-label]="'Campaigns.TemplateUsageYesOption' | translate" (change)="onNeedsTemplateChanged()" />
          </div>
          @if (form.controls.needsTemplate.value === 'yes') {
            <div>
              <app-search-select [items]="templates.items()" [loading]="templates.loading()" [hasMore]="templates.hasMore()"
                [placeholder]="'Campaigns.TemplatePlaceholder' | translate" [selected]="form.controls.template.value"
                [invalid]="form.controls.template.invalid && form.controls.template.touched"
                (selectedChange)="onTemplateChanged($event)" (search)="templates.search($event)" (showMore)="templates.more()">
                <ng-template let-item>
                  <span class="flex w-full items-center gap-2">
                    <span class="grow truncate">{{ item.label }}</span>
                    @for (channel of item.data?.channels ?? []; track channel) {
                      <app-channel-icon [kind]="channel" />
                    }
                  </span>
                </ng-template>
              </app-search-select>
              @if (form.controls.template.touched && form.controls.template.hasError('required')) {
                <p class="label mt-1 text-error">{{ 'Campaigns.TemplateRequiredError' | translate }}</p>
              }
            </div>
          }
        </div>
      </fieldset>
    </form>
  `
})
export class CampaignBasicInfoComponent {
  private readonly _api = inject(MessagesApiClient);
  private readonly _translate = inject(TranslateService);

  /** Emits the id of the selected template (or `undefined` when cleared). */
  public readonly templateSelected = output<string | undefined>();

  protected readonly now = toLocalInput(new Date());
  public readonly form = inject(NonNullableFormBuilder).group({
    title: ['', [Validators.required, Validators.maxLength(128)]],
    from: [toLocalInput(new Date())],
    to: [''],
    actionLinkText: ['', Validators.maxLength(128)],
    actionLinkHref: ['', [Validators.pattern(URL_PATTERN), Validators.maxLength(2048)]],
    type: [null as SearchSelectItem<MessageType> | null],
    needsTemplate: ['no' as 'no' | 'yes'],
    template: [null as SearchSelectItem<TemplateListItem> | null]
  });

  protected readonly types = new PagedLookup<MessageType>(
    (page, size, term) => this._api.getMessageTypes(page, size, 'name+', term),
    type => ({ id: type.id!, label: type.name ?? '', data: type })
  );

  protected readonly templates = new PagedLookup<TemplateListItem>(
    (page, size, term) => this._api.getTemplates(page, size, 'name+', term, this.form.controls.type.value?.id, true, TemplateType.Full),
    template => ({ id: template.id!, label: template.name ?? '', data: template })
  );

  protected onTypeChanged(type: SearchSelectItem<MessageType> | null): void {
    this.form.controls.type.setValue(type);
    if (this.form.controls.needsTemplate.value === 'yes') {
      this.templates.search('');
    }
  }

  protected onNeedsTemplateChanged(): void {
    const template = this.form.controls.template;
    if (this.form.controls.needsTemplate.value === 'yes') {
      template.setValidators(Validators.required);
      template.markAsTouched();
    } else {
      template.clearValidators();
    }
    if (template.value) {
      this.templateSelected.emit(undefined);
    }
    template.setValue(null);
    template.updateValueAndValidity();
  }

  protected onTemplateChanged(template: SearchSelectItem<TemplateListItem> | null): void {
    this.form.controls.template.setValue(template);
    this.form.controls.template.markAsTouched();
    if (!template) {
      this.templateSelected.emit(undefined);
      return;
    }
    const { actionLinkText, actionLinkHref } = this.form.controls;
    if (!actionLinkText.value) {
      actionLinkText.setValue(this._translate.instant('Campaigns.ActionLinkDefaultText'));
    }
    if (!actionLinkHref.value) {
      actionLinkHref.setValue('https://www.indice.gr');
    }
    this.templateSelected.emit(template.id);
  }
}
