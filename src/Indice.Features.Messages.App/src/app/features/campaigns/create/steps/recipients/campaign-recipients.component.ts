import { Component, ElementRef, OnInit, computed, inject, signal, viewChild } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormControl, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideGlobe, LucideIdCard, LucideListChecks, LucidePencil, LucideUserPlus, LucideUsers } from '@lucide/angular';
import { settings } from 'src/app/core/models/settings';
import { Contact, DistributionList, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { ListContactCreateComponent } from 'src/app/shared/components/list-contact-create/list-contact-create.component';

export type SendVia = 'distribution-list' | 'recipient-ids' | 'recipients' | 'user-base';

/** Wizard step 4: who receives the campaign (a distribution list, recipient ids, picked contacts or everyone). */
@Component({
  selector: 'app-campaign-recipients',
  imports: [ReactiveFormsModule, TranslatePipe, ListContactCreateComponent, LucideGlobe, LucideIdCard, LucideListChecks, LucidePencil, LucideUserPlus, LucideUsers],
  template: `
    <form class="grid gap-6 lg:grid-cols-[18rem_1fr]" [formGroup]="form">
      <fieldset class="fieldset">
        <legend class="fieldset-legend">{{ 'Campaigns.RecipientsSendToLabelRequired' | translate }}</legend>
        <div class="space-y-2">
          @for (option of options; track option.value) {
            <label class="flex cursor-pointer items-center gap-3 rounded-field border border-base-300 px-3 py-2.5 text-sm has-checked:border-primary has-checked:bg-primary/5">
              <input type="radio" class="radio radio-sm radio-primary" formControlName="sendVia" [value]="option.value" (change)="onSendViaChanged()" />
              @switch (option.value) {
                @case ('distribution-list') { <svg lucideListChecks class="size-4 text-base-content/60"></svg> }
                @case ('recipient-ids') { <svg lucideIdCard class="size-4 text-base-content/60"></svg> }
                @case ('recipients') { <svg lucideUsers class="size-4 text-base-content/60"></svg> }
                @default { <svg lucideGlobe class="size-4 text-base-content/60"></svg> }
              }
              <span class="font-medium">{{ option.label | translate }}</span>
            </label>
          }
        </div>
      </fieldset>

      <div class="min-w-0 pt-2 lg:pt-9">
        @switch (sendVia()) {
          @case ('distribution-list') {
            <fieldset class="fieldset max-w-md">
              <legend class="fieldset-legend">{{ 'Campaigns.DistributionListIdLabel' | translate }}</legend>
              <select class="select w-full" formControlName="distributionList"
                [class.select-error]="form.controls.distributionList.invalid && form.controls.distributionList.touched">
                <option value="" disabled>{{ 'Campaigns.SelectPlaceholder' | translate }}</option>
                @for (list of distributionLists(); track list.id) {
                  <option [value]="list.id">{{ list.name }}</option>
                }
              </select>
              @if (form.controls.distributionList.touched && form.controls.distributionList.hasError('required')) {
                <p class="label text-error">{{ 'Campaigns.DistributionListRequiredError' | translate }}</p>
              }
            </fieldset>
          }
          @case ('recipient-ids') {
            <fieldset class="fieldset max-w-xl">
              <legend class="fieldset-legend">{{ 'Campaigns.RecipientIdsLabel' | translate }}</legend>
              <textarea class="textarea h-40 w-full font-mono text-xs" formControlName="recipientIds"
                [placeholder]="'Campaigns.RecipientIdsPlaceholder' | translate"
                [class.textarea-error]="form.controls.recipientIds.invalid && form.controls.recipientIds.touched"></textarea>
              @if (form.controls.recipientIds.touched && form.controls.recipientIds.hasError('required')) {
                <p class="label text-error">{{ 'Campaigns.RecipientIdsRequiredError' | translate }}</p>
              } @else {
                <p class="label tabular">{{ recipientIdsCount() }} {{ 'Campaigns.RecipientsCountSuffix' | translate }}</p>
              }
            </fieldset>
          }
          @case ('recipients') {
            @let contacts = form.controls.recipients.value ?? [];
            <div class="flex flex-wrap items-center gap-4 rounded-box border border-dashed border-base-300 p-4">
              @if (contacts.length) {
                <div class="avatar-group -space-x-3">
                  @for (contact of contacts.slice(0, 4); track $index) {
                    <div class="avatar"><div class="w-10"><img [src]="avatarOrigin + '/avatar/' + contact.fullName + '/?circular=true'" alt="" /></div></div>
                  }
                  @if (contacts.length > 4) {
                    <div class="avatar avatar-placeholder"><div class="w-10 bg-neutral text-neutral-content text-xs">+{{ contacts.length - 4 }}</div></div>
                  }
                </div>
                <p class="text-sm">{{ 'Campaigns.AddedRecipientsInfoPrefix' | translate }} <strong class="tabular">{{ contacts.length }}</strong> {{ 'Campaigns.RecipientsCountSuffix' | translate }}</p>
                <button type="button" class="btn btn-sm ml-auto" (click)="openPicker()">
                  <svg lucidePencil class="size-4"></svg>{{ 'Shared.EditActionLabel' | translate }}
                </button>
              } @else {
                <p class="text-sm text-base-content/70">{{ 'Campaigns.NoRecipientsInfo' | translate }}</p>
                <button type="button" class="btn btn-primary btn-sm ml-auto" (click)="openPicker()">
                  <svg lucideUserPlus class="size-4"></svg>{{ 'Campaigns.Add' | translate }}
                </button>
              }
            </div>
            @if (form.controls.recipients.touched && form.controls.recipients.hasError('required')) {
              <p class="mt-2 text-sm text-error">{{ 'Campaigns.RecipientsRequiredError' | translate }}</p>
            }
          }
          @default {
            <div role="alert" class="alert alert-soft alert-warning max-w-xl text-sm">
              <svg lucideGlobe class="size-4"></svg>
              <span>{{ 'Campaigns.RecipientsAllInfo' | translate }}</span>
            </div>
          }
        }
      </div>
    </form>

    <dialog #pickerDialog class="modal modal-bottom sm:modal-middle" (cancel)="$event.preventDefault(); picker.cancel()">
      <div class="modal-box flex max-h-[85vh] max-w-2xl flex-col">
        <h3 class="mb-4 text-lg font-semibold">{{ 'Campaigns.RecipientsSendViaRecipientsOption' | translate }}</h3>
        <app-list-contact-create #picker class="min-h-80 flex-1 overflow-auto"
          (onSubmit)="setRecipients($event); closePicker()" (onCancel)="setRecipients($event); closePicker()" />
        <div class="modal-action">
          <button type="button" class="btn btn-ghost" (click)="picker.cancel()">{{ 'Campaigns.Close' | translate }}</button>
          <button type="button" class="btn btn-primary" (click)="picker.submit()">{{ 'Campaigns.Add' | translate }}</button>
        </div>
      </div>
    </dialog>
  `
})
export class CampaignRecipientsComponent implements OnInit {
  private readonly _api = inject(MessagesApiClient);
  private readonly _dialog = viewChild.required<ElementRef<HTMLDialogElement>>('pickerDialog');
  private readonly _picker = viewChild.required(ListContactCreateComponent);

  public readonly options: { value: SendVia; label: string }[] = [
    { value: 'distribution-list', label: 'Campaigns.RecipientsSendViaDistributionListOption' },
    { value: 'recipient-ids', label: 'Campaigns.RecipientsSendViaRecipientIdsOption' },
    { value: 'recipients', label: 'Campaigns.RecipientsSendViaRecipientsOption' },
    { value: 'user-base', label: 'Campaigns.RecipientsSendViaAllOption' }
  ];
  protected readonly avatarOrigin = (settings.api_url || '').substring(0, 4) === 'http' ? new URL(settings.api_url).origin : '';
  public readonly distributionLists = signal<DistributionList[]>([]);

  public readonly form = inject(NonNullableFormBuilder).group({
    sendVia: ['distribution-list' as SendVia],
    distributionList: ['', Validators.required],
    recipientIds: [''],
    recipients: new FormControl<Contact[] | null>(null)
  });

  public readonly sendVia = toSignal(this.form.controls.sendVia.valueChanges, { initialValue: this.form.controls.sendVia.value });
  private readonly _recipientIds = toSignal(this.form.controls.recipientIds.valueChanges, { initialValue: '' });
  protected readonly recipientIdsCount = computed(() => this.parseRecipientIds(this._recipientIds()).length);

  public ngOnInit(): void {
    this._api.getDistributionLists(undefined, undefined, undefined, undefined, false)
      .subscribe(result => this.distributionLists.set(result.items ?? []));
  }

  /** Name of the selected distribution list, for the review step. */
  public get distributionListName(): string | undefined {
    const id = this.form.controls.distributionList.value;
    return id ? this.distributionLists().find(l => l.id === id)?.name : undefined;
  }

  public get recipientIds(): string[] | null {
    const ids = this.parseRecipientIds(this.form.controls.recipientIds.value);
    return ids.length ? ids : null;
  }

  protected onSendViaChanged(): void {
    const { distributionList, recipientIds, recipients } = this.form.controls;
    for (const control of [distributionList, recipientIds, recipients]) {
      control.reset();
      control.clearValidators();
    }
    this._picker().reset();
    const required = { 'distribution-list': distributionList, 'recipient-ids': recipientIds, 'recipients': recipients, 'user-base': null }[this.form.controls.sendVia.value];
    required?.setValidators(Validators.required);
    for (const control of [distributionList, recipientIds, recipients]) {
      control.updateValueAndValidity();
    }
  }

  protected openPicker(): void {
    this._dialog().nativeElement.showModal();
  }

  protected closePicker(): void {
    this._dialog().nativeElement.close();
  }

  protected setRecipients(recipients: Contact[]): void {
    this.form.controls.recipients.setValue(recipients?.length ? recipients : null);
    this.form.controls.recipients.markAsTouched();
  }

  private parseRecipientIds(text: string | null | undefined): string[] {
    return (text ?? '').split('\n').map(x => x.trim()).filter(x => x !== '');
  }
}
