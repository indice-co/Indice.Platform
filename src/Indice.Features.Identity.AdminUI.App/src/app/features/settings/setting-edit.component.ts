import { Component, OnInit, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { filter, switchMap } from 'rxjs';
import { LucideTrash } from '@lucide/angular';
import { CreateAppSettingRequest, IdentityApiService, UpdateAppSettingRequest } from 'src/app/core/services/identity-api.service';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { SidePaneLayoutComponent, SidePaneService } from 'src/app/ui/side-pane/side-pane';
import { ToastService } from 'src/app/ui/toast/toast.service';
import { SETTINGS_REFRESH } from './settings.component';

/** Side pane form that creates an app setting, or edits the one in the `key` route param. */
@Component({
  selector: 'app-setting-edit',
  imports: [ReactiveFormsModule, SidePaneLayoutComponent, LucideTrash],
  template: `
    <app-side-pane-layout [title]="key() ? 'Edit setting' : 'New setting'"
      subtitle="Keys use the configuration path syntax, e.g. Section:Subsection:Name.">
      @if (loading()) {
        <div class="space-y-4">
          @for (row of [1, 2]; track row) { <div class="skeleton h-10 w-full"></div> }
        </div>
      } @else {
        <form id="setting-form" class="space-y-4" [formGroup]="form" (ngSubmit)="submit()">
          <fieldset class="fieldset">
            <legend class="fieldset-legend">Key{{ key() ? '' : ' *' }}</legend>
            <input class="input w-full font-mono" formControlName="key" maxlength="512" placeholder="e.g. General:Host"
              [class.input-error]="form.controls.key.invalid && form.controls.key.touched" />
            @if (form.controls.key.touched && form.controls.key.hasError('required')) {
              <p class="label text-error">A key is required.</p>
            } @else if (key()) {
              <p class="label whitespace-normal">The key cannot change; delete the setting and create a new one instead.</p>
            }
          </fieldset>
          <fieldset class="fieldset">
            <legend class="fieldset-legend">Value *</legend>
            <textarea class="textarea w-full font-mono text-sm" rows="8" formControlName="value"
              [class.textarea-error]="form.controls.value.invalid && form.controls.value.touched"></textarea>
            @if (form.controls.value.touched && form.controls.value.hasError('required')) {
              <p class="label text-error">A value is required.</p>
            }
          </fieldset>
        </form>
      }
      <div footer class="flex items-center gap-2 border-t border-base-300 px-6 py-4">
        @if (key()) {
          <button type="button" class="btn btn-ghost text-error" (click)="remove()" [disabled]="saving()">
            <svg lucideTrash class="size-4"></svg>Delete
          </button>
        }
        <span class="flex-1"></span>
        <button type="button" class="btn btn-ghost" (click)="pane.close()">Cancel</button>
        <button type="submit" form="setting-form" class="btn btn-primary" [disabled]="form.invalid || saving() || loading()">
          @if (saving()) { <span class="loading loading-spinner loading-sm"></span> }
          {{ key() ? 'Save' : 'Create setting' }}
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
        this._toast.success(key ? 'Setting updated' : 'Setting created', `Setting '${setting.key}' was ${key ? 'updated' : 'created'}.`);
        this._refresh.notify(SETTINGS_REFRESH);
        this.pane.close();
      },
      error: () => this.saving.set(false)
    });
  }

  protected remove(): void {
    const key = this.key()!;
    this._dialog
      .confirm({ title: 'Delete setting?', message: `Setting '${key}' will be deleted permanently.`, confirmLabel: 'Delete', tone: 'danger' })
      .pipe(filter(Boolean), switchMap(() => this._api.deleteSetting(key)))
      .subscribe(() => {
        this._toast.success('Setting deleted', `Setting '${key}' was deleted.`);
        this._refresh.notify(SETTINGS_REFRESH);
        this.pane.close();
      });
  }
}
