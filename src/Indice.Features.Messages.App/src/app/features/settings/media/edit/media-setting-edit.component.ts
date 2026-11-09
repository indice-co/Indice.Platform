import { Component, OnInit, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { UpdateMediaSettingRequest } from 'src/app/core/services/media-api.service';
import { AppTranslatedToaster, ToastType } from 'src/app/shared/services/app-translated-toaster';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { SidePaneLayoutComponent, SidePaneService } from 'src/app/ui/side-pane/side-pane';
import { SettingsStore } from '../../settings-store.service';

const URL_PATTERN = /^((http|https):\/\/.)[-a-zA-Z0-9@:%._\+~#=]{2,256}\.[a-z]{2,6}\b([-a-zA-Z0-9@:%_\+.~#?&/=]*)$/;

/** Side pane that edits the value of the media setting in the `mediaSettingKey` route param. */
@Component({
  selector: 'app-media-setting-edit',
  imports: [ReactiveFormsModule, TranslatePipe, SidePaneLayoutComponent],
  template: `
    <app-side-pane-layout [title]="'Settings.EditMediaSettingDialogTitle' | translate" [subtitle]="mediaSettingKey()">
      @if (loading()) {
        <div class="skeleton h-10 w-full"></div>
      } @else {
        <form id="media-setting-form" class="space-y-4" [formGroup]="form" (ngSubmit)="submit()">
          @if (description()) {
            <p class="text-sm text-base-content/70">{{ description() }}</p>
          }
          <fieldset class="fieldset">
            <legend class="fieldset-legend">{{ 'Settings.SettingValueLabelRequired' | translate }}</legend>
            <input class="input w-full font-mono" type="url" formControlName="value" [placeholder]="'Settings.SettingValuePlaceholder' | translate"
              [class.input-error]="form.controls.value.invalid && form.controls.value.touched" />
            @if (form.controls.value.touched && form.controls.value.hasError('pattern')) {
              <p class="label text-error">{{ 'Settings.SettingValuePatternError' | translate }}</p>
            } @else {
              <p class="label whitespace-normal">{{ 'Settings.SettingValueInfo' | translate }}</p>
            }
          </fieldset>
        </form>
      }
      <div footer class="flex justify-end gap-2 border-t border-base-300 px-6 py-4">
        <button type="button" class="btn btn-ghost" (click)="pane.close()">{{ 'Settings.Close' | translate }}</button>
        <button type="submit" form="media-setting-form" class="btn btn-primary" [disabled]="form.invalid || saving() || loading()">
          @if (saving()) { <span class="loading loading-spinner loading-sm"></span> }
          {{ 'Settings.Save' | translate }}
        </button>
      </div>
    </app-side-pane-layout>
  `
})
export class MediaSettingEditComponent implements OnInit {
  private readonly _store = inject(SettingsStore);
  private readonly _toaster = inject(AppTranslatedToaster);
  private readonly _refresh = inject(RefreshBus);
  protected readonly pane = inject(SidePaneService);

  /** Bound from the `edit-media-setting/:mediaSettingKey` route. */
  public readonly mediaSettingKey = input.required<string>();

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly description = signal<string | undefined>(undefined);
  protected readonly form = inject(NonNullableFormBuilder).group({
    value: ['', Validators.pattern(URL_PATTERN)]
  });

  public ngOnInit(): void {
    this._store.getMediaSetting(this.mediaSettingKey()).subscribe({
      next: setting => {
        this.form.setValue({ value: setting?.value || '' });
        this.description.set(setting?.description);
        this.loading.set(false);
      },
      error: () => this.pane.close()
    });
  }

  protected submit(): void {
    if (this.form.invalid) {
      return;
    }
    const key = this.mediaSettingKey();
    this.saving.set(true);
    this._store.updateMediaSettings(key, new UpdateMediaSettingRequest({ value: this.form.getRawValue().value })).subscribe({
      next: () => {
        this._toaster.show(ToastType.Success, 'Settings.MediaSettingUpdateSuccessTitle', 'Settings.MediaSettingUpdateSuccessMessage', undefined, { key });
        this._refresh.notify('media-settings');
        this.pane.close();
      },
      error: () => this.saving.set(false)
    });
  }
}
