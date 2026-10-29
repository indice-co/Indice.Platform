import { Component, OnInit, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { filter, switchMap } from 'rxjs';
import { LucideLock, LucideTrash } from '@lucide/angular';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AuthService } from 'src/app/core/services/auth.service';
import { ClaimValueType, CreateClaimTypeRequest, IdentityApiService, UpdateClaimTypeRequest } from 'src/app/core/services/identity-api.service';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { SidePaneLayoutComponent, SidePaneService } from 'src/app/ui/side-pane/side-pane';
import { ToastService } from 'src/app/ui/toast/toast.service';
import { CLAIM_TYPES_REFRESH } from './claim-types.component';

/** Side pane form that creates a claim type, or shows / edits the one in the `id` route param. */
@Component({
  selector: 'app-claim-type-edit',
  imports: [ReactiveFormsModule, SidePaneLayoutComponent, LucideLock, LucideTrash, TranslatePipe],
  template: `
    <app-side-pane-layout [title]="id() ? (name() || ('ClaimTypes.Edit.ClaimType' | translate)) : ('ClaimTypes.New' | translate)"
      [subtitle]="(id() ? 'ClaimTypes.Edit.Subtitle' : 'ClaimTypes.Edit.SubtitleNew') | translate">
      @if (loading()) {
        <div class="space-y-4">
          @for (row of [1, 2, 3, 4]; track row) { <div class="skeleton h-10 w-full"></div> }
        </div>
      } @else {
        @if (reserved()) {
          <div role="alert" class="alert alert-soft alert-warning mb-4 text-sm">
            <svg lucideLock class="size-4"></svg>{{ 'ClaimTypes.Edit.ReservedNotice' | translate }}
          </div>
        }
        <form id="claim-type-form" class="space-y-4" [formGroup]="form" (ngSubmit)="submit()">
          <fieldset class="fieldset">
            <legend class="fieldset-legend">{{ 'Common.Name' | translate }}{{ id() ? '' : ' *' }}</legend>
            <input class="input w-full font-mono" formControlName="name" maxlength="64" [placeholder]="'ClaimTypes.Edit.NamePlaceholder' | translate"
              [class.input-error]="form.controls.name.invalid && form.controls.name.touched" />
            @if (form.controls.name.touched && form.controls.name.hasError('required')) {
              <p class="label text-error">{{ 'ClaimTypes.Edit.NameRequired' | translate }}</p>
            } @else if (id()) {
              <p class="label whitespace-normal">{{ 'ClaimTypes.Edit.NameImmutable' | translate }}</p>
            }
          </fieldset>
          <fieldset class="fieldset">
            <legend class="fieldset-legend">{{ 'Common.DisplayName' | translate }}</legend>
            <input class="input w-full" formControlName="displayName" maxlength="128" [placeholder]="'ClaimTypes.Edit.DisplayNamePlaceholder' | translate" />
          </fieldset>
          <fieldset class="fieldset">
            <legend class="fieldset-legend">{{ 'Common.Description' | translate }}</legend>
            <textarea class="textarea w-full" rows="3" formControlName="description" maxlength="1000"></textarea>
          </fieldset>
          <div class="grid gap-4 sm:grid-cols-2">
            <fieldset class="fieldset">
              <legend class="fieldset-legend">{{ 'ClaimTypes.Fields.ValueType' | translate }}</legend>
              <select class="select w-full" formControlName="valueType">
                @for (type of valueTypes; track type) { <option [value]="type">{{ type }}</option> }
              </select>
            </fieldset>
            <fieldset class="fieldset">
              <legend class="fieldset-legend">{{ 'ClaimTypes.Edit.Rule' | translate }}</legend>
              <input class="input w-full font-mono" formControlName="rule" maxlength="2000" [placeholder]="'ClaimTypes.Edit.RulePlaceholder' | translate" />
            </fieldset>
          </div>
          <label class="flex cursor-pointer items-start gap-3 rounded-field border border-base-300 p-3 has-checked:border-primary/50 has-checked:bg-primary/5">
            <input type="checkbox" class="toggle toggle-sm toggle-primary mt-0.5" formControlName="userEditable" />
            <span>
              <span class="block text-sm font-semibold">{{ 'ClaimTypes.Fields.UserEditable' | translate }}</span>
              <span class="block text-xs text-base-content/65">{{ 'ClaimTypes.Edit.UserEditableHint' | translate }}</span>
            </span>
          </label>
          <label class="flex cursor-pointer items-start gap-3 rounded-field border border-base-300 p-3 has-checked:border-primary/50 has-checked:bg-primary/5">
            <input type="checkbox" class="toggle toggle-sm toggle-primary mt-0.5" formControlName="required" />
            <span>
              <span class="block text-sm font-semibold">{{ 'Common.Required' | translate }}</span>
              <span class="block text-xs text-base-content/65">{{ 'ClaimTypes.Edit.RequiredHint' | translate }}</span>
            </span>
          </label>
        </form>
      }
      <div footer class="flex items-center gap-2 border-t border-base-300 px-6 py-4">
        @if (id() && canEdit && !reserved()) {
          <button type="button" class="btn btn-ghost text-error" (click)="remove()" [disabled]="saving()">
            <svg lucideTrash class="size-4"></svg>{{ 'Common.Delete' | translate }}
          </button>
        }
        <span class="flex-1"></span>
        <button type="button" class="btn btn-ghost" (click)="pane.close()">{{ (canEdit ? 'Common.Cancel' : 'Common.Close') | translate }}</button>
        @if (canEdit) {
          <button type="submit" form="claim-type-form" class="btn btn-primary" [disabled]="form.invalid || saving() || loading()">
            @if (saving()) { <span class="loading loading-spinner loading-sm"></span> }
            {{ (id() ? 'Common.Save' : 'ClaimTypes.Edit.Create') | translate }}
          </button>
        }
      </div>
    </app-side-pane-layout>
  `
})
export class ClaimTypeEditComponent implements OnInit {
  private readonly _api = inject(IdentityApiService);
  private readonly _auth = inject(AuthService);
  private readonly _dialog = inject(DialogService);
  private readonly _toast = inject(ToastService);
  private readonly _refresh = inject(RefreshBus);
  private readonly _translate = inject(TranslateService);
  protected readonly pane = inject(SidePaneService);
  protected readonly canEdit = this._auth.isAdminUIUsersWriter() || this._auth.isAdminUIClientsWriter();

  /** Bound from the `claim-type/:id` pane route; absent when creating. */
  public readonly id = input<string>();

  protected readonly valueTypes = Object.values(ClaimValueType);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly name = signal('');
  protected readonly reserved = signal(false);
  protected readonly form = inject(NonNullableFormBuilder).group({
    name: ['', [Validators.required, Validators.maxLength(64)]],
    displayName: ['', Validators.maxLength(128)],
    description: ['', Validators.maxLength(1000)],
    valueType: [ClaimValueType.String as ClaimValueType],
    rule: [''],
    userEditable: [false],
    required: [false]
  });

  public ngOnInit(): void {
    const id = this.id();
    if (!this.canEdit) {
      this.form.disable();
    }
    if (!id) {
      return;
    }
    this.form.controls.name.disable();
    this.loading.set(true);
    this._api.getClaimType(id).subscribe({
      next: claim => {
        this.name.set(claim.name ?? '');
        this.reserved.set(!!claim.reserved);
        this.form.setValue({
          name: claim.name ?? '',
          displayName: claim.displayName ?? '',
          description: claim.description ?? '',
          valueType: claim.valueType ?? ClaimValueType.String,
          rule: claim.rule ?? '',
          userEditable: !!claim.userEditable,
          required: !!claim.required
        });
        this.loading.set(false);
      },
      error: () => this.pane.close()
    });
  }

  protected submit(): void {
    if (this.form.invalid) {
      return;
    }
    const { name, ...value } = this.form.getRawValue();
    const body = {
      ...value,
      displayName: value.displayName || undefined,
      description: value.description || undefined,
      rule: value.rule || undefined
    };
    const id = this.id();
    const request$ = id
      ? this._api.updateClaimType(id, new UpdateClaimTypeRequest(body))
      : this._api.createClaimType(new CreateClaimTypeRequest({ ...body, name: name.trim() }));
    this.saving.set(true);
    request$.subscribe({
      next: claim => {
        this._toast.success(
          this._translate.instant(id ? 'ClaimTypes.Toast.UpdatedTitle' : 'ClaimTypes.Toast.CreatedTitle'),
          this._translate.instant(id ? 'ClaimTypes.Toast.UpdatedMessage' : 'ClaimTypes.Toast.CreatedMessage', { name: claim.name })
        );
        this._refresh.notify(CLAIM_TYPES_REFRESH);
        this.pane.close();
      },
      error: () => this.saving.set(false)
    });
  }

  protected remove(): void {
    const id = this.id()!;
    this._dialog
      .confirm({
        title: this._translate.instant('ClaimTypes.Delete.Title'),
        message: this._translate.instant('ClaimTypes.Delete.Message', { name: this.name() }),
        confirmLabel: this._translate.instant('Common.Delete'),
        tone: 'danger'
      })
      .pipe(filter(Boolean), switchMap(() => this._api.deleteClaimType(id)))
      .subscribe(() => {
        this._toast.success(this._translate.instant('ClaimTypes.Toast.DeletedTitle'), this._translate.instant('ClaimTypes.Toast.DeletedMessage', { name: this.name() }));
        this._refresh.notify(CLAIM_TYPES_REFRESH);
        this.pane.close();
      });
  }
}
