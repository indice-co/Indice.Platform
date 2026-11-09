import { Component, OnInit, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { CreateMessageSenderRequest, MessagesApiClient, UpdateMessageSenderRequest } from 'src/app/core/services/messages-api.service';
import { AppTranslatedToaster, ToastType } from 'src/app/shared/services/app-translated-toaster';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { SidePaneLayoutComponent, SidePaneService } from 'src/app/ui/side-pane/side-pane';
import { SettingsStore } from '../../settings-store.service';

/** Side pane form that creates an email sender, or edits the one in the `messageSenderId` route param. */
@Component({
  selector: 'app-email-senders-edit',
  imports: [ReactiveFormsModule, TranslatePipe, SidePaneLayoutComponent],
  template: `
    <app-side-pane-layout [title]="(messageSenderId() ? 'Settings.EditSenderDialogTitle' : 'Settings.CreateSenderDialogTitle') | translate">
      @if (loading()) {
        <div class="space-y-4">
          @for (row of [1, 2, 3]; track row) { <div class="skeleton h-10 w-full"></div> }
        </div>
      } @else {
        <form id="message-sender-form" class="space-y-4" [formGroup]="form" (ngSubmit)="submit()">
          <fieldset class="fieldset">
            <legend class="fieldset-legend">{{ 'Settings.SenderAddressLabelRequired' | translate }}</legend>
            <input class="input w-full" type="email" formControlName="sender" maxlength="128" autocomplete="off"
              [placeholder]="'Settings.SenderAddressPlaceholder' | translate"
              [class.input-error]="form.controls.sender.invalid && form.controls.sender.touched" />
            @if (form.controls.sender.touched && form.controls.sender.hasError('required')) {
              <p class="label text-error">{{ 'Settings.SenderAddressRequiredError' | translate }}</p>
            } @else if (form.controls.sender.hasError('maxlength')) {
              <p class="label text-error">{{ 'Settings.SenderAddressMaxLengthError' | translate }}</p>
            } @else {
              <p class="label whitespace-normal">{{ 'Settings.SenderAddressInfo' | translate }}</p>
            }
          </fieldset>

          <fieldset class="fieldset">
            <legend class="fieldset-legend">{{ 'Settings.SenderDisplayNameLabelRequired' | translate }}</legend>
            <input class="input w-full" formControlName="displayName" maxlength="128" [placeholder]="'Settings.SenderDisplayNamePlaceholder' | translate"
              [class.input-error]="form.controls.displayName.invalid && form.controls.displayName.touched" />
            @if (form.controls.displayName.touched && form.controls.displayName.hasError('required')) {
              <p class="label text-error">{{ 'Settings.SenderDisplayNameRequiredError' | translate }}</p>
            } @else if (form.controls.displayName.hasError('maxlength')) {
              <p class="label text-error">{{ 'Settings.SenderDisplayNameMaxLengthError' | translate }}</p>
            } @else {
              <p class="label whitespace-normal">{{ 'Settings.SenderDisplayNameInfo' | translate }}</p>
            }
          </fieldset>

          @if (messageSenderId()) {
            <label class="flex items-start justify-between gap-4 rounded-box border border-base-300 px-4 py-3"
              [class.cursor-pointer]="!isDefaultSender()" [class.opacity-70]="isDefaultSender()">
              <span>
                <span class="block text-sm font-medium">{{ 'Settings.DefaultSenderCheckboxLabel' | translate }}</span>
                <span class="block text-xs text-base-content/60">{{ 'Settings.DefaultSenderCheckboxInfo' | translate }}</span>
              </span>
              <input type="checkbox" class="toggle toggle-primary mt-0.5" formControlName="isDefault" />
            </label>
          }
        </form>
      }
      <div footer class="flex justify-end gap-2 border-t border-base-300 px-6 py-4">
        <button type="button" class="btn btn-ghost" (click)="pane.close()">{{ 'Settings.Close' | translate }}</button>
        <button type="submit" form="message-sender-form" class="btn btn-primary" [disabled]="form.invalid || saving() || loading()">
          @if (saving()) { <span class="loading loading-spinner loading-sm"></span> }
          {{ (messageSenderId() ? 'Settings.Save' : 'Settings.Create') | translate }}
        </button>
      </div>
    </app-side-pane-layout>
  `
})
export class EmailSendersEditComponent implements OnInit {
  private readonly _api = inject(MessagesApiClient);
  private readonly _store = inject(SettingsStore);
  private readonly _toaster = inject(AppTranslatedToaster);
  private readonly _refresh = inject(RefreshBus);
  protected readonly pane = inject(SidePaneService);

  /** Bound from the `edit-message-sender/:messageSenderId` route; absent when creating. */
  public readonly messageSenderId = input<string>();

  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  /** The default sender cannot be unset; another sender has to be made the default instead. */
  protected readonly isDefaultSender = signal(false);
  protected readonly form = inject(NonNullableFormBuilder).group({
    sender: ['', [Validators.required, Validators.maxLength(128)]],
    displayName: ['', [Validators.required, Validators.maxLength(128)]],
    isDefault: [false]
  });

  public ngOnInit(): void {
    const id = this.messageSenderId();
    if (!id) {
      return;
    }
    this.loading.set(true);
    this._api.getMessageSenderById(id).subscribe({
      next: sender => {
        this.form.setValue({ sender: sender.sender ?? '', displayName: sender.displayName ?? '', isDefault: sender.isDefault ?? false });
        this.isDefaultSender.set(sender.isDefault ?? false);
        if (sender.isDefault) {
          this.form.controls.isDefault.disable();
        }
        this.loading.set(false);
      },
      error: () => this.pane.close()
    });
  }

  protected submit(): void {
    if (this.form.invalid) {
      return;
    }
    const value = this.form.getRawValue();
    const id = this.messageSenderId();
    const request$: Observable<unknown> = id
      ? this._store.updateMessageSender(id, new UpdateMessageSenderRequest(value))
      : this._store.createMessageSender(new CreateMessageSenderRequest({ sender: value.sender, displayName: value.displayName }));
    this.saving.set(true);
    request$.subscribe({
      next: () => {
        if (id) {
          this._toaster.show(ToastType.Success, 'Settings.UpdateSenderSuccessTitle', 'Settings.UpdateSenderSuccessMessage', undefined, { sender: value.sender });
        } else {
          this._toaster.show(ToastType.Success, 'Settings.CreateSenderSuccessTitle', 'Settings.CreateSenderSuccessMessage', undefined, { sender: value.displayName });
        }
        this._refresh.notify('message-senders');
        this.pane.close();
      },
      error: () => this.saving.set(false)
    });
  }
}
