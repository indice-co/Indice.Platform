import { Component, OnInit, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { CreateMessageTypeRequest, MessagesApiClient, MessageTypeClassification, UpdateMessageTypeRequest } from 'src/app/core/services/messages-api.service';
import { AppTranslatedToaster, ToastType } from 'src/app/shared/services/app-translated-toaster';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { SidePaneLayoutComponent, SidePaneService } from 'src/app/ui/side-pane/side-pane';

/** Side pane form that creates a message type, or edits the one in the `messageTypeId` route param. */
@Component({
  selector: 'app-message-type-edit',
  imports: [ReactiveFormsModule, TranslatePipe, SidePaneLayoutComponent],
  template: `
    <app-side-pane-layout [title]="(messageTypeId() ? 'MessageTypes.EditDialogTitle' : 'MessageTypes.CreateDialogTitle') | translate">
      @if (loading()) {
        <div class="space-y-4">
          @for (row of [1, 2, 3]; track row) { <div class="skeleton h-10 w-full"></div> }
        </div>
      } @else {
        <form id="message-type-form" class="space-y-4" [formGroup]="form" (ngSubmit)="submit()">
          <fieldset class="fieldset">
            <legend class="fieldset-legend">{{ 'MessageTypes.NameLabelRequired' | translate }}</legend>
            <input class="input w-full" formControlName="name" maxlength="128" [placeholder]="'MessageTypes.NamePlaceholder' | translate"
              [class.input-error]="form.controls.name.invalid && form.controls.name.touched" />
            @if (form.controls.name.touched && form.controls.name.hasError('required')) {
              <p class="label text-error">{{ 'MessageTypes.NameRequiredError' | translate }}</p>
            } @else {
              <p class="label whitespace-normal">{{ 'MessageTypes.NameInfo' | translate }}</p>
            }
          </fieldset>

          <fieldset class="fieldset">
            <legend class="fieldset-legend">{{ 'MessageTypes.AliasLabel' | translate }}</legend>
            <input class="input w-full font-mono" formControlName="alias" maxlength="64" [placeholder]="'MessageTypes.AliasPlaceholder' | translate" />
          </fieldset>

          <fieldset class="fieldset">
            <legend class="fieldset-legend">{{ 'MessageTypes.ClassificationLabel' | translate }}</legend>
            <div class="grid grid-cols-2 gap-2 sm:grid-cols-3">
              @for (option of classifications; track option) {
                <label class="flex cursor-pointer items-center gap-2 rounded-field border border-base-300 px-3 py-2 text-sm has-checked:border-primary has-checked:bg-primary/5"
                  [attr.data-class]="option">
                  <input type="radio" class="radio radio-xs radio-primary" formControlName="classification" [value]="option" />
                  <span class="class-dot"></span>{{ option }}
                </label>
              }
            </div>
          </fieldset>
        </form>
      }
      <div footer class="flex justify-end gap-2 border-t border-base-300 px-6 py-4">
        <button type="button" class="btn btn-ghost" (click)="pane.close()">{{ 'MessageTypes.Close' | translate }}</button>
        <button type="submit" form="message-type-form" class="btn btn-primary" [disabled]="form.invalid || saving() || loading()">
          @if (saving()) { <span class="loading loading-spinner loading-sm"></span> }
          {{ (messageTypeId() ? 'MessageTypes.Save' : 'MessageTypes.Create') | translate }}
        </button>
      </div>
    </app-side-pane-layout>
  `
})
export class MessageTypeEditComponent implements OnInit {
  private readonly _api = inject(MessagesApiClient);
  private readonly _toaster = inject(AppTranslatedToaster);
  private readonly _refresh = inject(RefreshBus);
  protected readonly pane = inject(SidePaneService);

  /** Bound from the `edit-message-type/:messageTypeId` route; absent when creating. */
  public readonly messageTypeId = input<string>();

  protected readonly classifications = Object.values(MessageTypeClassification);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly form = inject(NonNullableFormBuilder).group({
    name: ['', [Validators.required, Validators.maxLength(128)]],
    alias: ['', Validators.maxLength(64)],
    classification: [MessageTypeClassification.System, Validators.required]
  });

  public ngOnInit(): void {
    const id = this.messageTypeId();
    if (!id) {
      return;
    }
    this.loading.set(true);
    this._api.getMessageTypeById(id).subscribe({
      next: type => {
        this.form.setValue({ name: type.name ?? '', alias: type.alias ?? '', classification: type.classification ?? MessageTypeClassification.System });
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
    const body = { name: value.name, alias: value.alias || undefined, classification: value.classification };
    const id = this.messageTypeId();
    const request$: Observable<unknown> = id
      ? this._api.updateMessageType(id, new UpdateMessageTypeRequest(body))
      : this._api.createMessageType(new CreateMessageTypeRequest(body));
    this.saving.set(true);
    request$.subscribe({
      next: () => {
        this._toaster.show(ToastType.Success,
          id ? 'MessageTypes.UpdateSuccessTitle' : 'MessageTypes.CreateSuccessTitle',
          id ? 'MessageTypes.UpdateSuccessMessage' : 'MessageTypes.CreateSuccessMessage', undefined, { name: value.name });
        this._refresh.notify('message-types');
        this.pane.close();
      },
      error: () => this.saving.set(false)
    });
  }
}
