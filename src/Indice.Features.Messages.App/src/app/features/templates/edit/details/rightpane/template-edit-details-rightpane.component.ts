import { Component, OnInit, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { MessagesApiClient, MessageType, Template } from 'src/app/core/services/messages-api.service';
import { AppTranslatedToaster, ToastType } from 'src/app/shared/services/app-translated-toaster';
import { SidePaneLayoutComponent, SidePaneService } from 'src/app/ui/side-pane/side-pane';
import { TemplateEditStore } from '../../template-edit-store.service';

/** Which part of the template the pane edits. */
export type TemplateEditSection = 'name' | 'preferences' | 'messageType';

/** Side pane that edits one section of a template's basic information (`edit-template/:templateId;section=…`). */
@Component({
  selector: 'app-template-details-edit-rightpane',
  imports: [ReactiveFormsModule, TranslatePipe, SidePaneLayoutComponent],
  template: `
    <app-side-pane-layout [title]="'Templates.EditTemplateDialogTitle' | translate" [subtitle]="template()?.name">
      @if (!template()) {
        <div class="space-y-4">
          @for (row of [1, 2]; track row) { <div class="skeleton h-10 w-full"></div> }
        </div>
      } @else {
        <form id="template-details-form" class="space-y-4" [formGroup]="form" (ngSubmit)="submit()">
          @switch (section()) {
            @case ('preferences') {
              <label class="flex cursor-pointer items-center justify-between gap-4 rounded-box border border-base-300 px-4 py-3">
                <span class="text-sm font-medium">{{ 'Templates.IgnoreUserPreferencesLabel' | translate }}</span>
                <input type="checkbox" class="toggle toggle-primary" formControlName="ignoreUserPreferences" />
              </label>
            }
            @case ('messageType') {
              <fieldset class="fieldset">
                <legend class="fieldset-legend">{{ 'Templates.MessageTypeLabel' | translate }}</legend>
                <select class="select w-full" formControlName="messageTypeId">
                  <option value="">{{ 'Templates.SelectPlaceholder' | translate }}</option>
                  @for (type of messageTypes(); track type.id) {
                    <option [value]="type.id">{{ type.name }}</option>
                  }
                </select>
              </fieldset>
            }
            @default {
              <fieldset class="fieldset">
                <legend class="fieldset-legend">{{ 'Templates.TitleLabelRequired' | translate }}</legend>
                <input class="input w-full" formControlName="name" maxlength="256" [placeholder]="'Templates.TitlePlaceholder' | translate"
                  [class.input-error]="form.controls.name.invalid && form.controls.name.touched" />
                @if (form.controls.name.touched && form.controls.name.hasError('required')) {
                  <p class="label text-error">{{ 'Templates.TitleRequiredError' | translate }}</p>
                } @else if (form.controls.name.hasError('maxlength')) {
                  <p class="label text-error">{{ 'Templates.TitleMaxLengthError' | translate }}</p>
                } @else {
                  <p class="label whitespace-normal">{{ 'Templates.TitleInfo' | translate }}</p>
                }
              </fieldset>
              <fieldset class="fieldset">
                <legend class="fieldset-legend">{{ 'Templates.AliasLabel' | translate }}</legend>
                <input class="input w-full font-mono" formControlName="alias" maxlength="64" [placeholder]="'Templates.AliasPlaceholder' | translate" />
                @if (form.controls.alias.hasError('maxlength')) {
                  <p class="label text-error">{{ 'Templates.AliasMaxLengthError' | translate }}</p>
                }
              </fieldset>
            }
          }
        </form>
      }
      <div footer class="flex justify-end gap-2 border-t border-base-300 px-6 py-4">
        <button type="button" class="btn btn-ghost" (click)="pane.close()">{{ 'Templates.Close' | translate }}</button>
        <button type="submit" form="template-details-form" class="btn btn-primary" [disabled]="form.invalid || saving() || !template()">
          @if (saving()) { <span class="loading loading-spinner loading-sm"></span> }
          {{ 'Templates.Save' | translate }}
        </button>
      </div>
    </app-side-pane-layout>
  `
})
export class TemplateDetailsEditRightpaneComponent implements OnInit {
  private readonly _api = inject(MessagesApiClient);
  private readonly _store = inject(TemplateEditStore);
  private readonly _toaster = inject(AppTranslatedToaster);
  protected readonly pane = inject(SidePaneService);

  /** Bound from the `edit-template/:templateId` route. */
  public readonly templateId = input.required<string>();
  /** Bound from the `section` matrix param. */
  public readonly section = input<TemplateEditSection>('name');

  protected readonly template = signal<Template | undefined>(undefined);
  protected readonly messageTypes = signal<MessageType[]>([]);
  protected readonly saving = signal(false);
  protected readonly form = inject(NonNullableFormBuilder).group({
    name: ['', [Validators.required, Validators.maxLength(256)]],
    alias: ['', Validators.maxLength(64)],
    ignoreUserPreferences: [false],
    messageTypeId: ['']
  });

  public ngOnInit(): void {
    if (this.section() === 'messageType') {
      this._api.getMessageTypes().subscribe(result => this.messageTypes.set(result.items ?? []));
    }
    this._store.getTemplate(this.templateId()).subscribe({
      next: template => {
        this.form.setValue({
          name: template.name ?? '',
          alias: template.alias ?? '',
          ignoreUserPreferences: template.ignoreUserPreferences ?? false,
          messageTypeId: template.messageType?.id ?? ''
        });
        this.template.set(template);
      },
      error: () => this.pane.close()
    });
  }

  protected submit(): void {
    const template = this.template();
    if (this.form.invalid || !template) {
      return;
    }
    const value = this.form.getRawValue();
    const id = this.templateId();
    let request$: Observable<void>;
    switch (this.section()) {
      case 'preferences':
        request$ = this._store.updateUserPreference(id, value.ignoreUserPreferences);
        break;
      case 'messageType':
        request$ = this._store.updateTemplateMessageType(id, value.messageTypeId || undefined);
        break;
      default:
        request$ = this._store.updateTemplate(id, new Template({ ...template, name: value.name, alias: value.alias || undefined }));
    }
    this.saving.set(true);
    request$.subscribe({
      next: () => {
        this._toaster.show(ToastType.Success, 'Templates.UpdateSuccessTitle', 'Templates.UpdateSuccessMessage', undefined, { name: value.name });
        this.pane.close();
      },
      error: () => this.saving.set(false)
    });
  }
}
