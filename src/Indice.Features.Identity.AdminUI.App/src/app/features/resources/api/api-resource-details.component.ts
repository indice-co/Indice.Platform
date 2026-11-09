import { Component, OnInit, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { IdentityApiService, UpdateApiResourceRequest } from 'src/app/core/services/identity-api.service';
import { ToastService } from 'src/app/ui/toast/toast.service';
import { ApiResourceStore } from './api-resource-edit.component';

/** Details tab of an API resource. */
@Component({
  selector: 'app-api-resource-details',
  imports: [ReactiveFormsModule, TranslatePipe],
  template: `
    <form class="panel max-w-3xl space-y-5 p-6" [formGroup]="form" (ngSubmit)="submit()">
      <div class="grid gap-4 sm:grid-cols-2">
        <fieldset class="fieldset">
          <legend class="fieldset-legend">{{ 'Common.Name' | translate }}</legend>
          <input class="input w-full font-mono" [value]="store.resource()?.name" disabled />
          <p class="label">{{ 'Resources.Form.NameImmutable' | translate }}</p>
        </fieldset>
        <fieldset class="fieldset">
          <legend class="fieldset-legend">{{ 'Common.DisplayName' | translate }}</legend>
          <input class="input w-full" formControlName="displayName" maxlength="200" />
        </fieldset>
      </div>
      <fieldset class="fieldset">
        <legend class="fieldset-legend">{{ 'Common.Description' | translate }}</legend>
        <textarea class="textarea w-full" rows="3" formControlName="description" maxlength="1000"></textarea>
      </fieldset>
      <label class="flex cursor-pointer items-start gap-3 rounded-field border border-base-300 p-3 has-checked:border-primary/50 has-checked:bg-primary/5 sm:max-w-sm">
        <input type="checkbox" class="toggle toggle-sm toggle-primary mt-0.5" formControlName="enabled" />
        <span>
          <span class="block text-sm font-semibold">{{ 'Common.Enabled' | translate }}</span>
          <span class="block text-xs text-base-content/65">{{ 'Resources.Api.EnabledHint' | translate }}</span>
        </span>
      </label>

      @if (store.editable()) {
        <div class="flex justify-end gap-2 border-t border-base-300 pt-4">
          <button type="button" class="btn btn-ghost" [disabled]="form.pristine || saving()" (click)="reset()">{{ 'Common.Discard' | translate }}</button>
          <button type="submit" class="btn btn-primary" [disabled]="form.invalid || form.pristine || saving()">
            @if (saving()) { <span class="loading loading-spinner loading-sm"></span> }
            {{ 'Common.SaveChanges' | translate }}
          </button>
        </div>
      }
    </form>
  `
})
export class ApiResourceDetailsComponent implements OnInit {
  private readonly _api = inject(IdentityApiService);
  private readonly _toast = inject(ToastService);
  private readonly _translate = inject(TranslateService);
  protected readonly store = inject(ApiResourceStore);
  protected readonly saving = signal(false);

  protected readonly form = inject(NonNullableFormBuilder).group({
    displayName: ['', Validators.maxLength(200)],
    description: ['', Validators.maxLength(1000)],
    enabled: [false]
  });

  public ngOnInit(): void {
    this.reset();
    if (!this.store.editable()) {
      this.form.disable();
    }
  }

  protected reset(): void {
    const resource = this.store.resource()!;
    this.form.reset({ displayName: resource.displayName ?? '', description: resource.description ?? '', enabled: !!resource.enabled });
  }

  protected submit(): void {
    const resource = this.store.resource()!;
    const value = this.form.getRawValue();
    this.saving.set(true);
    this._api.updateApiResource(resource.id!, new UpdateApiResourceRequest({
      enabled: value.enabled,
      displayName: value.displayName || undefined,
      description: value.description || undefined
    })).subscribe({
      next: () => {
        this.store.patch(value);
        this.form.markAsPristine();
        this.saving.set(false);
        this._toast.success(this._translate.instant('Resources.Api.Updated.Title'), this._translate.instant('Resources.Api.Updated.Message', { name: resource.name }));
      },
      error: () => this.saving.set(false)
    });
  }
}
