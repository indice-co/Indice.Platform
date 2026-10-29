import { Component, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { CreateApiScopeRequest, IdentityApiService } from 'src/app/core/services/identity-api.service';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { SidePaneLayoutComponent, SidePaneService } from 'src/app/ui/side-pane/side-pane';
import { ToastService } from 'src/app/ui/toast/toast.service';
import { ClaimsAssignmentComponent } from '../shared/claims-assignment.component';
import { API_RESOURCE_REFRESH } from './api-resource-edit.component';

/** Side pane that adds a scope to the API resource in the `resourceId` route param. */
@Component({
  selector: 'app-api-resource-scope-add',
  imports: [ReactiveFormsModule, SidePaneLayoutComponent, ClaimsAssignmentComponent, TranslatePipe],
  template: `
    <app-side-pane-layout [title]="'Resources.Api.AddScope' | translate" [subtitle]="'Resources.Api.ScopeAddSubtitle' | translate">
      <form id="scope-add-form" class="space-y-4" [formGroup]="form" (ngSubmit)="submit()">
        <fieldset class="fieldset">
          <legend class="fieldset-legend">{{ 'Common.Name' | translate }} *</legend>
          <input class="input w-full font-mono" formControlName="name" maxlength="200" [placeholder]="'Resources.Form.ExamplePlaceholder' | translate: { example: 'orders:read' }"
            [class.input-error]="form.controls.name.invalid && form.controls.name.touched" />
          @if (form.controls.name.touched && form.controls.name.hasError('required')) {
            <p class="label text-error">{{ 'Resources.Form.NameRequired' | translate }}</p>
          } @else {
            <p class="label whitespace-normal" [innerHTML]="'Resources.Api.ScopeNameHint' | translate"></p>
          }
        </fieldset>
        <fieldset class="fieldset">
          <legend class="fieldset-legend">{{ 'Common.DisplayName' | translate }}</legend>
          <input class="input w-full" formControlName="displayName" maxlength="200" />
        </fieldset>
        <fieldset class="fieldset">
          <legend class="fieldset-legend">{{ 'Common.Description' | translate }}</legend>
          <textarea class="textarea w-full" rows="3" formControlName="description" maxlength="1000"></textarea>
        </fieldset>
        <div>
          <h3 class="mb-2 text-sm font-semibold">{{ 'Resources.UserClaims' | translate }}</h3>
          <app-claims-assignment [assigned]="claims()" (added)="addClaim($event)"
            (removed)="removeClaim($event)" />
        </div>
      </form>
      <div footer class="flex justify-end gap-2 border-t border-base-300 px-6 py-4">
        <button type="button" class="btn btn-ghost" (click)="pane.close()">{{ 'Common.Cancel' | translate }}</button>
        <button type="submit" form="scope-add-form" class="btn btn-primary" [disabled]="form.invalid || saving()">
          @if (saving()) { <span class="loading loading-spinner loading-sm"></span> }
          {{ 'Resources.Api.AddScope' | translate }}
        </button>
      </div>
    </app-side-pane-layout>
  `
})
export class ApiResourceScopeAddComponent {
  private readonly _api = inject(IdentityApiService);
  private readonly _toast = inject(ToastService);
  private readonly _translate = inject(TranslateService);
  private readonly _refresh = inject(RefreshBus);
  protected readonly pane = inject(SidePaneService);

  /** Bound from the `resource-api-scope-add/:resourceId` pane route. */
  public readonly resourceId = input.required<string>();

  protected readonly saving = signal(false);
  protected readonly claims = signal<string[]>([]);
  protected readonly form = inject(NonNullableFormBuilder).group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    displayName: ['', Validators.maxLength(200)],
    description: ['', Validators.maxLength(1000)]
  });

  protected addClaim(claim: string): void {
    this.claims.update(claims => [...claims, claim]);
  }

  protected removeClaim(claim: string): void {
    this.claims.update(claims => claims.filter(name => name !== claim));
  }

  protected submit(): void {
    if (this.form.invalid) {
      return;
    }
    const value = this.form.getRawValue();
    this.saving.set(true);
    this._api.addApiResourceScope(+this.resourceId(), new CreateApiScopeRequest({
      name: value.name.trim(),
      displayName: value.displayName || undefined,
      description: value.description || undefined,
      userClaims: this.claims(),
      showInDiscoveryDocument: true,
      translations: {}
    })).subscribe({
      next: scope => {
        this._toast.success(this._translate.instant('Resources.Scopes.Added.Title'), this._translate.instant('Resources.Scopes.Added.Message', { name: scope.name }));
        this._refresh.notify(API_RESOURCE_REFRESH);
        this.pane.close();
      },
      error: () => this.saving.set(false)
    });
  }
}
