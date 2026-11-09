import { Component, OnInit, inject, signal, viewChild } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideArrowLeft, LucideSave } from '@lucide/angular';
import { CreateTemplateRequest, MessageContent, MessagesApiClient, MessageType, TemplateType } from 'src/app/core/services/messages-api.service';
import { AppTranslatedToaster, ToastType } from 'src/app/shared/services/app-translated-toaster';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';
import { TemplateContentEditorComponent } from '../content-editor/template-content-editor.component';
import { TEMPLATE_TYPES, templateTypeLabel } from '../template-type';

@Component({
  selector: 'app-template-create',
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe, PageHeaderComponent, TemplateContentEditorComponent, LucideArrowLeft, LucideSave],
  host: { '(document:keydown.control.s)': 'onSaveShortcut($event)' },
  template: `
    <app-page-header [title]="'Templates.CreateTemplateDialogTitle' | translate" [subtitle]="'Templates.CreateWizardIntro' | translate">
      <a leading class="btn btn-ghost btn-xs -ml-2 mb-2" routerLink="..">
        <svg lucideArrowLeft class="size-4"></svg>{{ 'Templates.Title' | translate }}
      </a>
      <div actions>
        <a class="btn btn-ghost btn-sm" routerLink="..">{{ 'Common.Cancel' | translate }}</a>
        <button type="button" class="btn btn-primary btn-sm" [disabled]="saving() || form.invalid || editor().form.invalid" (click)="save()">
          @if (saving()) { <span class="loading loading-spinner loading-xs"></span> } @else { <svg lucideSave class="size-4"></svg> }
          {{ 'Templates.Save' | translate }}
        </button>
      </div>
    </app-page-header>

    <div class="space-y-6">
      <section class="panel p-5 sm:p-6">
        <h2 class="font-semibold">{{ 'Templates.BasicInformationHeader' | translate }}</h2>
        <p class="mb-4 text-sm text-base-content/70">{{ 'Templates.BasicInformationDescription' | translate }}</p>
        <form class="grid gap-x-6 gap-y-2 md:grid-cols-2" [formGroup]="form" (ngSubmit)="save()">
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

          <fieldset class="fieldset">
            <legend class="fieldset-legend">{{ 'Templates.KindLabel' | translate }}</legend>
            <select class="select w-full" formControlName="type">
              @for (type of types; track type) {
                <option [value]="type">{{ typeLabel(type) | translate }}</option>
              }
            </select>
            <p class="label whitespace-normal">{{ 'Templates.KindInfo' | translate }}</p>
          </fieldset>

          <fieldset class="fieldset">
            <legend class="fieldset-legend">{{ 'Templates.MessageTypeLabel' | translate }}</legend>
            <select class="select w-full" formControlName="messageTypeId">
              <option value="">{{ 'Templates.SelectPlaceholder' | translate }}</option>
              @for (type of messageTypes(); track type.id) {
                <option [value]="type.id">{{ type.name }}</option>
              }
            </select>
          </fieldset>

          <label class="flex cursor-pointer items-center justify-between gap-4 rounded-box border border-base-300 px-4 py-3 md:col-span-2">
            <span class="text-sm font-medium">{{ 'Templates.IgnoreUserPreferencesLabel' | translate }}</span>
            <input type="checkbox" class="toggle toggle-primary" formControlName="ignoreUserPreferences" />
          </label>
        </form>
      </section>

      <section>
        <h2 class="font-semibold">{{ 'Templates.ContentLabel' | translate }}</h2>
        <p class="mb-3 text-sm text-base-content/70">{{ 'Templates.ContentDescription' | translate }}</p>
        <app-template-content-editor [content]="initialContent" [data]="{}" />
      </section>
    </div>
  `
})
export class TemplateCreateComponent implements OnInit {
  private readonly _api = inject(MessagesApiClient);
  private readonly _toaster = inject(AppTranslatedToaster);
  private readonly _router = inject(Router);

  protected readonly editor = viewChild.required(TemplateContentEditorComponent);
  protected readonly types = TEMPLATE_TYPES;
  protected readonly typeLabel = templateTypeLabel;
  protected readonly messageTypes = signal<MessageType[]>([]);
  protected readonly saving = signal(false);
  protected readonly initialContent: { [key: string]: MessageContent } = { inbox: new MessageContent() };
  protected readonly form = inject(NonNullableFormBuilder).group({
    name: ['', [Validators.required, Validators.maxLength(256)]],
    alias: ['', Validators.maxLength(64)],
    type: [TemplateType.Full],
    messageTypeId: [''],
    ignoreUserPreferences: [false]
  });

  public ngOnInit(): void {
    this._api.getMessageTypes().subscribe(result => this.messageTypes.set(result.items ?? []));
  }

  protected onSaveShortcut(event: Event): void {
    event.preventDefault();
    this.save();
  }

  protected save(): void {
    const editor = this.editor();
    if (this.form.invalid || editor.form.invalid) {
      this.form.markAllAsTouched();
      editor.markAllAsTouched();
      return;
    }
    if (this.saving()) {
      return;
    }
    const value = this.form.getRawValue();
    const { content, data } = editor.getValue();
    const request = new CreateTemplateRequest({
      name: value.name,
      alias: value.alias || undefined,
      messageTypeId: value.messageTypeId || undefined,
      ignoreUserPreferences: value.ignoreUserPreferences,
      type: value.type,
      content,
      data
    });
    this.saving.set(true);
    this._api.createTemplate(request).subscribe({
      next: template => {
        this._toaster.show(ToastType.Success, 'Templates.CreateSuccessTitle', 'Templates.CreateSuccessMessage', undefined, { title: template.name });
        this._router.navigate(['templates']);
      },
      error: () => this.saving.set(false)
    });
  }
}
