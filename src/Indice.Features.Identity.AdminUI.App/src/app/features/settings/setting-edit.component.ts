import { Component, OnInit, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { filter, switchMap } from 'rxjs';
import { LucideTrash } from '@lucide/angular';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { CreateAppSettingRequest, IdentityApiService, UpdateAppSettingRequest } from 'src/app/core/services/identity-api.service';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { SidePaneLayoutComponent, SidePaneService } from 'src/app/ui/side-pane/side-pane';
import { ToastService } from 'src/app/ui/toast/toast.service';
import { SETTINGS_REFRESH } from './settings.component';

/** Side pane form that creates an app setting, or edits the one in the `key` route param. */
@Component({
  selector: 'app-setting-edit',
  imports: [ReactiveFormsModule, SidePaneLayoutComponent, LucideTrash, TranslatePipe],
  template: `
    <app-side-pane-layout [title]="(key() ? 'Settings.Edit.Title' : 'Settings.New') | translate"
      [subtitle]="'Settings.Edit.Subtitle' | translate">
      @if (loading()) {
        <div class="space-y-4">
          @for (row of [1, 2]; track row) { <div class="skeleton h-10 w-full"></div> }
        </div>
      } @else {
        <form id="setting-form" class="space-y-4" [formGroup]="form" (ngSubmit)="submit()">
          <fieldset class="fieldset">
            <legend class="fieldset-legend">{{ 'Settings.Fields.Key' | translate }}{{ key() ? '' : ' *' }}</legend>
            <input class="input w-full font-mono" formControlName="key" maxlength="512" [placeholder]="'Settings.Edit.KeyPlaceholder' | translate"
              [class.input-error]="form.controls.key.invalid && form.controls.key.touched" />
            @if (form.controls.key.touched && form.controls.key.hasError('required')) {
              <p class="label text-error">{{ 'Settings.Edit.KeyRequired' | translate }}</p>
            } @else if (key()) {
              <p class="label whitespace-normal">{{ 'Settings.Edit.KeyImmutable' | translate }}</p>
            }
          </fieldset>
          <fieldset class="fieldset">
            <legend class="fieldset-legend">{{ 'Common.Value' | translate }} *</legend>
            <textarea class="textarea w-full font-mono text-sm" rows="8" formControlName="value"
              [class.textarea-error]="form.controls.value.invalid && form.controls.value.touched"></textarea>
            @if (form.controls.value.touched && form.controls.value.hasError('required')) {
              <p class="label text-error">{{ 'Settings.Edit.ValueRequired' | translate }}</p>
            }
          </fieldset>
        </form>
      }
      <div footer class="flex items-center gap-2 border-t border-base-300 px-6 py-4">
        @if (key()) {
          <button type="button" class="btn btn-ghost text-error" (click)="remove()" [disabled]="saving()">
            <svg lucideTrash class="size-4"></svg>{{ 'Common.Delete' | translate }}
          </button>
        }
        <span class="flex-1"></span>
        <button type="button" class="btn btn-ghost" (click)="pane.close()">{{ 'Common.Cancel' | translate }}</button>
        <button type="submit" form="setting-form" class="btn btn-primary" [disabled]="form.invalid || saving() || loading()">
          @if (saving()) { <span class="loading loading-spinner loading-sm"></span> }
          {{ (key() ? 'Common.Save' : 'Settings.Edit.Create') | translate }}
        </button>
      </div>
    </app-side-pane-layout>
  `
})
export class SettingEditComponent implements OnInit {
  private readonly _api = inject(IdentityApiService);
  private readonly _dialog = inject(DialogService);
  private readonly _toast = inject(ToastService);
  private readonly _refresh = inject(RefreshBus);
  private readonly _translate = inject(TranslateService);
  protected readonly pane = inject(SidePaneService);

  /** Bound from the `setting/:key` pane route; absent when creating. */
  public readonly key = input<string>();

  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly form = inject(NonNullableFormBuilder).group({
    key: ['', [Validators.required, Validators.maxLength(512)]],
    value: ['', Validators.required]
  });

  public ngOnInit(): void {
    const key = this.key();
    if (!key) {
      return;
    }
    this.form.controls.key.disable();
    this.loading.set(true);
    this._api.getSettingByKey(key).subscribe({
      next: setting => {
        this.form.setValue({ key: setting.key ?? key, value: setting.value ?? '' });
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
    const key = this.key();
    const request$ = key
      ? this._api.updateSetting(key, new UpdateAppSettingRequest({ value: value.value }))
      : this._api.createSetting(new CreateAppSettingRequest({ key: value.key.trim(), value: value.value }));
    this.saving.set(true);
    request$.subscribe({
      next: setting => {
        this._toast.success(
          this._translate.instant(key ? 'Settings.Toast.UpdatedTitle' : 'Settings.Toast.CreatedTitle'),
          this._translate.instant(key ? 'Settings.Toast.UpdatedMessage' : 'Settings.Toast.CreatedMessage', { name: setting.key })
        );
        this._refresh.notify(SETTINGS_REFRESH);
        this.pane.close();
      },
      error: () => this.saving.set(false)
    });
  }

  protected remove(): void {
    const key = this.key()!;
    this._dialog
      .confirm({
        title: this._translate.instant('Settings.Delete.Title'),
        message: this._translate.instant('Settings.Delete.Message', { name: key }),
        confirmLabel: this._translate.instant('Common.Delete'),
        tone: 'danger'
      })
      .pipe(filter(Boolean), switchMap(() => this._api.deleteSetting(key)))
      .subscribe(() => {
        this._toast.success(this._translate.instant('Settings.Toast.DeletedTitle'), this._translate.instant('Settings.Toast.DeletedMessage', { name: key }));
        this._refresh.notify(SETTINGS_REFRESH);
        this.pane.close();
      });
  }
}
