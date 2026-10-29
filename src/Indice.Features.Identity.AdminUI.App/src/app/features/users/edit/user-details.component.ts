import { Component, effect, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import { AbstractControl, FormControl, FormRecord, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideSave } from '@lucide/angular';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AuthService } from 'src/app/core/services/auth.service';
import {
  BasicClaimInfo, ClaimTypeInfo, IdentityApiService, MfaPolicy, PasswordExpirationPolicy, SingleUserInfo, UpdateUserRequest
} from 'src/app/core/services/identity-api.service';
import { ToastService } from 'src/app/ui/toast/toast.service';
import { ClaimValueInputComponent } from '../claim-value-input.component';
import { UserStore } from '../user-store';
import { MFA_POLICIES, PASSWORD_EXPIRATION_POLICIES, claimValueValidators } from '../user-utils';

/** Account fields of the user plus the claims every user is required to have. */
@Component({
  selector: 'app-user-details',
  imports: [DatePipe, ReactiveFormsModule, TranslatePipe, ClaimValueInputComponent, LucideSave],
  template: `
    @if (store.user(); as user) {
      <form [formGroup]="form" (ngSubmit)="save()" class="grid gap-6 xl:grid-cols-[1fr_20rem]">
        <div class="space-y-6">
          <section class="panel p-5 sm:p-6">
            <h2 class="mb-1 font-semibold">{{ 'Users.Details.Account' | translate }}</h2>
            <p class="mb-4 text-sm text-base-content/65">{{ 'Users.Details.AccountHint' | translate }}</p>
            <div class="grid gap-x-6 gap-y-2 md:grid-cols-2">
              <fieldset class="fieldset">
                <legend class="fieldset-legend">{{ 'Users.Fields.UserName' | translate }} *</legend>
                <input class="input w-full font-mono" formControlName="userName" maxlength="256" autocomplete="off"
                  [class.input-error]="invalid(form.controls.userName)" />
                @if (invalid(form.controls.userName)) { <p class="label text-error">{{ 'Users.Validation.UserNameRequired' | translate }}</p> }
              </fieldset>
              <fieldset class="fieldset">
                <legend class="fieldset-legend">{{ 'Common.Email' | translate }} *</legend>
                <input type="email" class="input w-full" formControlName="email" maxlength="256" autocomplete="off"
                  [class.input-error]="invalid(form.controls.email)" />
                @if (invalid(form.controls.email)) {
                  <p class="label text-error">{{ (form.controls.email.hasError('email') ? 'Users.Validation.EmailInvalid' : 'Users.Validation.EmailRequired') | translate }}</p>
                }
                <label class="label mt-1 cursor-pointer gap-2 text-sm text-base-content">
                  <input type="checkbox" class="checkbox checkbox-sm checkbox-primary" formControlName="emailConfirmed" />{{ 'Users.Fields.EmailVerified' | translate }}
                </label>
              </fieldset>
              <fieldset class="fieldset">
                <legend class="fieldset-legend">{{ 'Common.PhoneNumber' | translate }}</legend>
                <input type="tel" class="input w-full tabular" formControlName="phoneNumber" autocomplete="off" />
                <label class="label mt-1 cursor-pointer gap-2 text-sm text-base-content">
                  <input type="checkbox" class="checkbox checkbox-sm checkbox-primary" formControlName="phoneNumberConfirmed" />{{ 'Users.Fields.PhoneVerified' | translate }}
                </label>
              </fieldset>
            </div>
          </section>

          <section class="panel p-5 sm:p-6">
            <h2 class="mb-1 font-semibold">{{ 'Users.Details.Security' | translate }}</h2>
            <p class="mb-4 text-sm text-base-content/65">{{ 'Users.Details.SecurityHint' | translate }}</p>
            <div class="grid gap-x-6 gap-y-2 md:grid-cols-2">
              <fieldset class="fieldset">
                <legend class="fieldset-legend">{{ 'Users.Details.MfaPolicy' | translate }}</legend>
                <select class="select w-full" formControlName="twoFactorPolicy">
                  <option value="">{{ 'Users.Details.NotSet' | translate }}</option>
                  @for (policy of mfaPolicies; track policy.value) { <option [value]="policy.value">{{ policy.label | translate }}</option> }
                </select>
                <label class="label mt-1 cursor-pointer gap-2 text-sm text-base-content">
                  <input type="checkbox" class="toggle toggle-sm toggle-primary" formControlName="twoFactorEnabled" />{{ 'Users.Status.MfaEnabled' | translate }}
                </label>
              </fieldset>
              <fieldset class="fieldset">
                <legend class="fieldset-legend">{{ 'Users.Details.PasswordExpires' | translate }}</legend>
                <select class="select w-full" formControlName="passwordExpirationPolicy">
                  <option value="">{{ 'Users.Details.NotSet' | translate }}</option>
                  @for (policy of passwordPolicies; track policy.value) { <option [value]="policy.value">{{ policy.label | translate }}</option> }
                </select>
                <p class="label">
                  {{ user.passwordExpirationDate
                    ? ('Users.Details.CurrentPasswordExpires' | translate: { date: (user.passwordExpirationDate | date: 'medium') })
                    : ('Users.Details.PasswordNeverExpires' | translate) }}
                </p>
              </fieldset>
            </div>
          </section>

          @if (requiredClaims().length) {
            <section class="panel p-5 sm:p-6">
              <h2 class="mb-1 font-semibold">{{ 'Users.Details.RequiredDetails' | translate }}</h2>
              <p class="mb-4 text-sm text-base-content/65">{{ 'Users.Details.RequiredDetailsHint' | translate }}</p>
              <div class="grid gap-x-6 gap-y-2 md:grid-cols-2" formGroupName="claims">
                @for (claim of requiredClaims(); track claim.name) {
                  <fieldset class="fieldset">
                    <legend class="fieldset-legend" [attr.title]="claim.description">{{ claim.displayName || claim.name }} *</legend>
                    <app-claim-value-input [formControlName]="claim.name!" [valueType]="claim.valueType"
                      [invalid]="invalid(form.controls.claims.controls[claim.name!])" />
                    @if (invalid(form.controls.claims.controls[claim.name!])) {
                      <p class="label text-error">{{ (form.controls.claims.controls[claim.name!].hasError('pattern') ? 'Users.Validation.ValueFormat' : 'Users.Validation.ValueRequired') | translate }}</p>
                    }
                  </fieldset>
                }
              </div>
            </section>
          }

          @if (canEdit) {
            <div class="flex flex-wrap items-center gap-4">
              <button type="submit" class="btn btn-primary" [disabled]="saving()">
                @if (saving()) { <span class="loading loading-spinner loading-sm"></span> } @else { <svg lucideSave class="size-4"></svg> }
                {{ 'Common.SaveChanges' | translate }}
              </button>
              @if (showBypass()) {
                <label class="flex cursor-pointer items-center gap-2 text-sm">
                  <input type="checkbox" class="checkbox checkbox-sm checkbox-warning" formControlName="bypassEmailAsUserNamePolicy" />
                  {{ 'Users.Details.BypassEmailPolicy' | translate }}
                </label>
              }
            </div>
          }
        </div>

        <aside class="panel h-fit p-5 text-sm">
          <h2 class="mb-3 font-semibold">{{ 'Users.Details.Activity' | translate }}</h2>
          <dl class="space-y-3">
            <div><dt class="text-base-content/60">{{ 'Common.CreatedAt' | translate }}</dt><dd class="tabular">{{ user.createDate | date: 'medium' }}</dd></div>
            <div><dt class="text-base-content/60">{{ 'Users.Fields.LastSignIn' | translate }}</dt>
              <dd class="tabular">{{ user.lastSignInDate ? (user.lastSignInDate | date: 'medium') : ('Common.Never' | translate) }}</dd></div>
            <div><dt class="text-base-content/60">{{ 'Users.Details.FailedAttempts' | translate }}</dt><dd class="tabular">{{ user.accessFailedCount ?? 0 }}</dd></div>
            <div><dt class="text-base-content/60">{{ 'Users.Details.Lockout' | translate }}</dt><dd>{{ (user.lockoutEnabled ? 'Common.Enabled' : 'Common.Disabled') | translate }}</dd></div>
          </dl>
        </aside>
      </form>
    } @else {
      <div class="panel space-y-4 p-6">
        @for (row of [1, 2, 3]; track row) { <div class="skeleton h-10 w-full"></div> }
      </div>
    }
  `
})
export class UserDetailsComponent {
  private readonly _api = inject(IdentityApiService);
  private readonly _toast = inject(ToastService);
  private readonly _fb = inject(NonNullableFormBuilder);
  private readonly _translate = inject(TranslateService);
  protected readonly store = inject(UserStore);
  protected readonly canEdit = inject(AuthService).isAdminUIUsersWriter();
  protected readonly mfaPolicies = MFA_POLICIES;
  protected readonly passwordPolicies = PASSWORD_EXPIRATION_POLICIES;
  protected readonly saving = signal(false);
  protected readonly showBypass = signal(false);
  protected readonly requiredClaims = signal<ClaimTypeInfo[]>([]);
  private readonly _claimTypes = toSignal(this.store.claimTypes$, { initialValue: [] as ClaimTypeInfo[] });

  protected readonly form = this._fb.group({
    userName: ['', [Validators.required, Validators.maxLength(256)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
    emailConfirmed: [false],
    phoneNumber: [''],
    phoneNumberConfirmed: [false],
    twoFactorPolicy: [''],
    twoFactorEnabled: [false],
    passwordExpirationPolicy: [''],
    bypassEmailAsUserNamePolicy: [false],
    claims: new FormRecord<FormControl<string>>({})
  });

  constructor() {
    if (!this.canEdit) {
      this.form.disable();
    }
    // (re)fill the form whenever the user or the claim types load
    effect(() => {
      const user = this.store.user();
      const types = this._claimTypes();
      if (user) {
        this.fill(user, types.filter(type => type.required === true));
      }
    });
  }

  protected invalid(control: AbstractControl | undefined): boolean {
    return !!control && control.invalid && control.touched;
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const value = this.form.getRawValue();
    const user = this.store.user()!;
    const request = new UpdateUserRequest({
      userName: value.userName,
      email: value.email,
      emailConfirmed: value.emailConfirmed,
      phoneNumber: value.phoneNumber || undefined,
      phoneNumberConfirmed: value.phoneNumberConfirmed,
      twoFactorPolicy: (value.twoFactorPolicy || undefined) as MfaPolicy | undefined,
      twoFactorEnabled: value.twoFactorEnabled,
      passwordExpirationPolicy: (value.passwordExpirationPolicy || undefined) as PasswordExpirationPolicy | undefined,
      isAdmin: user.isAdmin,
      claims: Object.entries(value.claims).map(([type, claimValue]) => new BasicClaimInfo({ type, value: claimValue })),
      bypassEmailAsUserNamePolicy: value.bypassEmailAsUserNamePolicy
    });
    this.saving.set(true);
    this._api.updateUser(user.id!, request).subscribe({
      next: updated => {
        this.saving.set(false);
        this.showBypass.set(false);
        this.store.user.set(updated);
        this._toast.success(this._translate.instant('Users.Toasts.UserUpdated'), this._translate.instant('Users.Details.SavedMessage', { name: updated.email || updated.userName }));
      },
      error: () => {
        this.saving.set(false);
        // a validation failure can be the "email as username" policy, which administrators may override
        this.showBypass.set(true);
      }
    });
  }

  private fill(user: SingleUserInfo, required: ClaimTypeInfo[]): void {
    const claims = this.form.controls.claims;
    for (const name of Object.keys(claims.controls)) {
      claims.removeControl(name);
    }
    for (const type of required) {
      const control = new FormControl(user.claims?.find(c => c.type === type.name)?.value ?? '', { nonNullable: true, validators: claimValueValidators(type) });
      if (!this.canEdit) {
        control.disable();
      }
      claims.addControl(type.name!, control);
    }
    this.requiredClaims.set(required);
    this.form.patchValue({
      userName: user.userName ?? '',
      email: user.email ?? '',
      emailConfirmed: !!user.emailConfirmed,
      phoneNumber: user.phoneNumber ?? '',
      phoneNumberConfirmed: !!user.phoneNumberConfirmed,
      twoFactorPolicy: user.twoFactorPolicy ?? '',
      twoFactorEnabled: !!user.twoFactorEnabled,
      passwordExpirationPolicy: user.passwordExpirationPolicy ?? '',
      bypassEmailAsUserNamePolicy: false
    });
    this.form.markAsPristine();
  }
}
