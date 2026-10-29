import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { filter, switchMap } from 'rxjs';
import { LucideShuffle, LucideTrash, LucideVault } from '@lucide/angular';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ApiSecretInfo, CreateSecretRequest, IdentityApiService } from 'src/app/core/services/identity-api.service';
import { StatusBadgeComponent } from 'src/app/ui/badges/badges';
import { CopyButtonComponent } from 'src/app/ui/copy/copy-button.component';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { ToastService } from 'src/app/ui/toast/toast.service';
import { ApiResourceStore } from './api-resource-edit.component';

/** Secrets tab of an API resource (used by the API to call the introspection endpoint). */
@Component({
  selector: 'app-api-resource-secrets',
  imports: [DatePipe, ReactiveFormsModule, StatusBadgeComponent, CopyButtonComponent, EmptyStateComponent, TranslatePipe, LucideShuffle, LucideTrash, LucideVault],
  template: `
    <div class="grid gap-6 xl:grid-cols-[1fr_22rem]">
      <div class="panel overflow-x-auto">
        <table class="table">
          <thead>
            <tr>
              <th>{{ 'Common.Type' | translate }}</th>
              <th>{{ 'Common.Value' | translate }}</th>
              <th>{{ 'Resources.Secrets.Expiration' | translate }}</th>
              <th>{{ 'Common.Description' | translate }}</th>
              @if (store.editable()) { <th class="w-12"><span class="sr-only">{{ 'Common.Actions' | translate }}</span></th> }
            </tr>
          </thead>
          <tbody>
            @for (secret of secrets(); track secret.id) {
              <tr>
                <td><span class="badge badge-sm badge-ghost">{{ secret.type || 'SharedSecret' }}</span></td>
                <td class="text-base-content/50 italic">{{ 'Resources.Secrets.Hidden' | translate }}</td>
                <td class="whitespace-nowrap">
                  @if (secret.expiration) {
                    <span class="tabular">{{ secret.expiration | date: 'mediumDate' }}</span>
                    @if (secret.isExpired) { <app-status-badge tone="error" class="ml-2">{{ 'Resources.Secrets.Expired' | translate }}</app-status-badge> }
                  } @else {
                    <span class="text-base-content/50">{{ 'Common.Never' | translate }}</span>
                  }
                </td>
                <td class="text-base-content/75">{{ secret.description || '—' }}</td>
                @if (store.editable()) {
                  <td class="text-right">
                    <button type="button" class="btn btn-ghost btn-xs btn-square text-error" (click)="remove(secret)" [attr.aria-label]="'Resources.Secrets.DeleteSecret' | translate">
                      <svg lucideTrash class="size-4"></svg>
                    </button>
                  </td>
                }
              </tr>
            } @empty {
              <tr>
                <td [attr.colspan]="store.editable() ? 5 : 4">
                  <app-empty-state [title]="'Resources.Secrets.Empty.Title' | translate" [message]="'Resources.Secrets.Empty.Message' | translate">
                    <svg icon lucideVault class="size-5"></svg>
                  </app-empty-state>
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>

      @if (store.editable()) {
        <form class="panel h-fit space-y-3 p-5" [formGroup]="form" (ngSubmit)="add()">
          <h3 class="font-semibold">{{ 'Resources.Secrets.AddTitle' | translate }}</h3>
          <fieldset class="fieldset py-0">
            <legend class="fieldset-legend">{{ 'Common.Value' | translate }} *</legend>
            <div class="join w-full">
              <input class="input join-item w-full font-mono text-xs" formControlName="value" autocomplete="off" />
              <button type="button" class="btn join-item" (click)="generate()" [attr.aria-label]="'Resources.Secrets.GenerateRandom' | translate" [title]="'Resources.Secrets.Generate' | translate">
                <svg lucideShuffle class="size-4"></svg>
              </button>
            </div>
            <p class="label flex items-center gap-1 whitespace-normal">
              {{ 'Resources.Secrets.CopyNow' | translate }}
              <app-copy-button [value]="form.controls.value.value" [label]="'Resources.Secrets.CopyLabel' | translate" />
            </p>
          </fieldset>
          <fieldset class="fieldset py-0">
            <legend class="fieldset-legend">{{ 'Resources.Secrets.Expiration' | translate }}</legend>
            <input type="date" class="input w-full" formControlName="expiration" [min]="today" />
          </fieldset>
          <fieldset class="fieldset py-0">
            <legend class="fieldset-legend">{{ 'Common.Description' | translate }}</legend>
            <input class="input w-full" formControlName="description" maxlength="2000" />
          </fieldset>
          <button type="submit" class="btn btn-primary btn-block" [disabled]="form.invalid || saving()">
            @if (saving()) { <span class="loading loading-spinner loading-sm"></span> }
            {{ 'Resources.Secrets.Add' | translate }}
          </button>
        </form>
      }
    </div>
  `
})
export class ApiResourceSecretsComponent {
  private readonly _api = inject(IdentityApiService);
  private readonly _dialog = inject(DialogService);
  private readonly _toast = inject(ToastService);
  private readonly _translate = inject(TranslateService);
  protected readonly store = inject(ApiResourceStore);

  protected readonly secrets = computed(() => this.store.resource()?.secrets ?? []);
  protected readonly saving = signal(false);
  protected readonly today = new Date().toISOString().slice(0, 10);
  protected readonly form = inject(NonNullableFormBuilder).group({
    value: ['', Validators.required],
    expiration: [''],
    description: ['']
  });

  protected generate(): void {
    const bytes = crypto.getRandomValues(new Uint8Array(32));
    this.form.controls.value.setValue(btoa(String.fromCharCode(...bytes)).replace(/[+/=]/g, ''));
  }

  protected add(): void {
    const value = this.form.getRawValue();
    this.saving.set(true);
    this._api.addApiResourceSecret(this.store.resource()!.id!, new CreateSecretRequest({
      value: value.value,
      description: value.description || undefined,
      expiration: value.expiration ? new Date(value.expiration) : undefined
    })).subscribe({
      next: secret => {
        this.store.patch({ secrets: [...this.secrets(), new ApiSecretInfo({ ...secret, value: undefined })] });
        this.form.reset();
        this.saving.set(false);
        this._toast.success(this._translate.instant('Resources.Secrets.Added.Title'), this._translate.instant('Resources.Secrets.Added.Message'));
      },
      error: () => this.saving.set(false)
    });
  }

  protected remove(secret: ApiSecretInfo): void {
    this._dialog
      .confirm({
        title: this._translate.instant('Resources.Secrets.Delete.Title'),
        message: this._translate.instant('Resources.Secrets.Delete.Message'),
        confirmLabel: this._translate.instant('Common.Delete'),
        tone: 'danger'
      })
      .pipe(filter(Boolean), switchMap(() => this._api.deleteApiResourceSecret(this.store.resource()!.id!, secret.id!)))
      .subscribe(() => {
        this.store.patch({ secrets: this.secrets().filter(s => s.id !== secret.id) });
        this._toast.success(this._translate.instant('Resources.Secrets.Deleted.Title'), this._translate.instant('Resources.Secrets.Deleted.Message'));
      });
  }
}
