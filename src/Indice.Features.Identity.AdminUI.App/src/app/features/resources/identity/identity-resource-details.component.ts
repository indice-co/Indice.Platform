import { Component, OnInit, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { IdentityApiService, UpdateIdentityResourceRequest } from 'src/app/core/services/identity-api.service';
import { ToastService } from 'src/app/ui/toast/toast.service';
import { IdentityResourceStore } from './identity-resource-edit.component';

/** Details tab of an identity resource. */
@Component({
  selector: 'app-identity-resource-details',
  imports: [ReactiveFormsModule],
  template: `
    <form class="panel max-w-3xl space-y-5 p-6" [formGroup]="form" (ngSubmit)="submit()">
      <div class="grid gap-4 sm:grid-cols-2">
        <fieldset class="fieldset">
          <legend class="fieldset-legend">Name</legend>
          <input class="input w-full font-mono" [value]="store.resource()?.name" disabled />
          <p class="label">The name cannot change once created.</p>
        </fieldset>
        <fieldset class="fieldset">
          <legend class="fieldset-legend">Display name</legend>
          <input class="input w-full" formControlName="displayName" maxlength="200" />
        </fieldset>
      </div>
      <fieldset class="fieldset">
        <legend class="fieldset-legend">Description</legend>
        <textarea class="textarea w-full" rows="3" formControlName="description" maxlength="1000"></textarea>
      </fieldset>

      <div class="grid gap-3 sm:grid-cols-2">
        @for (option of options; track option.key) {
          <label class="flex cursor-pointer items-start gap-3 rounded-field border border-base-300 p-3 has-checked:border-primary/50 has-checked:bg-primary/5">
            <input type="checkbox" class="toggle toggle-sm toggle-primary mt-0.5" [formControlName]="option.key" />
            <span>
              <span class="block text-sm font-semibold">{{ option.label }}</span>
              <span class="block text-xs text-base-content/65">{{ option.hint }}</span>
            </span>
          </label>
        }
      </div>

      @if (store.editable()) {
        <div class="flex justify-end gap-2 border-t border-base-300 pt-4">
          <button type="button" class="btn btn-ghost" [disabled]="form.pristine || saving()" (click)="reset()">Discard</button>
          <button type="submit" class="btn btn-primary" [disabled]="form.invalid || form.pristine || saving()">
            @if (saving()) { <span class="loading loading-spinner loading-sm"></span> }
            Save changes
          </button>
        </div>
      }
    </form>
  `
})
export class IdentityResourceDetailsComponent implements OnInit {
  private readonly _api = inject(IdentityApiService);
  private readonly _toast = inject(ToastService);
  protected readonly store = inject(IdentityResourceStore);
  protected readonly saving = signal(false);

  protected readonly options = [
    { key: 'enabled', label: 'Enabled', hint: 'Clients can request this resource.' },
    { key: 'required', label: 'Required', hint: 'Users cannot deselect it on the consent screen.' },
    { key: 'emphasize', label: 'Emphasize', hint: 'Highlighted on the consent screen.' },
    { key: 'showInDiscoveryDocument', label: 'Show in discovery document', hint: 'Listed in the OpenID Connect discovery document.' }
  ] as const;

  protected readonly form = inject(NonNullableFormBuilder).group({
    displayName: ['', Validators.maxLength(200)],
    description: ['', Validators.maxLength(1000)],
    enabled: [false],
    required: [false],
    emphasize: [false],
    showInDiscoveryDocument: [false]
  });

  public ngOnInit(): void {
    this.reset();
    if (!this.store.editable()) {
      this.form.disable();
    }
  }

  protected reset(): void {
    const resource = this.store.resource()!;
    this.form.reset({
      displayName: resource.displayName ?? '',
      description: resource.description ?? '',
      enabled: !!resource.enabled,
      required: !!resource.required,
      emphasize: !!resource.emphasize,
      showInDiscoveryDocument: !!resource.showInDiscoveryDocument
    });
  }

  protected submit(): void {
    const resource = this.store.resource()!;
    const value = this.form.getRawValue();
    this.saving.set(true);
    this._api.updateIdentityResource(resource.id!, new UpdateIdentityResourceRequest({
      ...value,
      displayName: value.displayName || undefined,
      description: value.description || undefined
    })).subscribe({
      next: () => {
        this.store.patch(value);
        this.form.markAsPristine();
        this.saving.set(false);
        this._toast.success('Identity resource updated', `Identity resource '${resource.name}' was updated.`);
      },
      error: () => this.saving.set(false)
    });
  }
}
