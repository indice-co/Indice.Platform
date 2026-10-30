import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormControl, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { map } from 'rxjs';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { LucideArrowLeft, LucideEye, LucideEyeOff, LucidePlus, LucideTrash } from '@lucide/angular';
import { BasicClaimInfo, ClaimTypeInfo, CreateUserRequest, IdentityApiService, RoleInfo } from 'src/app/core/services/identity-api.service';
import { UiFeaturesService } from 'src/app/core/services/ui-features.service';
import { CopyButtonComponent } from 'src/app/ui/copy/copy-button.component';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';
import { StepperComponent } from 'src/app/ui/stepper/stepper.component';
import { ToastService } from 'src/app/ui/toast/toast.service';
import { TransferListComponent } from 'src/app/ui/transfer/transfer-list.component';
import { ClaimValueInputComponent } from '../claim-value-input.component';
import { GeneratePasswordButtonComponent } from '../generate-password-button.component';
import { PasswordRulesComponent } from '../password-rules.component';
import { USERS_REFRESH_SCOPE } from '../users.component';
import { claimValueValidators, isAssignableClaimType } from '../user-utils';

/** Three step wizard (details, claims, roles) that creates a user. */
@Component({
  selector: 'app-user-add',
  imports: [
    ReactiveFormsModule, RouterLink, TranslatePipe, PageHeaderComponent, StepperComponent, TransferListComponent, ClaimValueInputComponent,
    PasswordRulesComponent, GeneratePasswordButtonComponent, CopyButtonComponent, LucideArrowLeft, LucideEye, LucideEyeOff, LucidePlus, LucideTrash
  ],
  template: `
    <app-page-header [title]="'Routes.NewUser' | translate" [subtitle]="'Users.Add.Subtitle' | translate">
      <a leading routerLink="/users" class="mb-2 inline-flex items-center gap-1 text-sm text-base-content/65 hover:text-base-content">
        <svg lucideArrowLeft class="size-4"></svg>{{ 'Routes.Users' | translate }}
      </a>
    </app-page-header>

    <div class="panel">
      <div class="border-b border-base-300 px-6 py-4">
        <app-stepper [steps]="steps" [current]="step()" (select)="step.set($event)" />
      </div>

      <div class="px-6 py-6">
        @switch (step()) {
          @case (0) {
            <form id="user-details" class="grid max-w-3xl gap-x-6 gap-y-2 md:grid-cols-2" [formGroup]="details" (ngSubmit)="next()">
              <fieldset class="fieldset">
                <legend class="fieldset-legend">{{ 'Users.Fields.FirstName' | translate }} *</legend>
                <input class="input w-full" formControlName="firstName" maxlength="256" autocomplete="off"
                  [class.input-error]="invalid('firstName')" />
                @if (invalid('firstName')) { <p class="label text-error">{{ 'Users.Validation.FirstNameRequired' | translate }}</p> }
              </fieldset>
              <fieldset class="fieldset">
                <legend class="fieldset-legend">{{ 'Users.Fields.LastName' | translate }} *</legend>
                <input class="input w-full" formControlName="lastName" maxlength="256" autocomplete="off"
                  [class.input-error]="invalid('lastName')" />
                @if (invalid('lastName')) { <p class="label text-error">{{ 'Users.Validation.LastNameRequired' | translate }}</p> }
              </fieldset>
              <fieldset class="fieldset">
                <legend class="fieldset-legend">{{ 'Common.Email' | translate }} *</legend>
                <input type="email" class="input w-full" formControlName="email" maxlength="256" autocomplete="off"
                  [class.input-error]="invalid('email')" (blur)="syncUserName()" />
                @if (invalid('email')) {
                  <p class="label text-error">{{ (details.controls.email.hasError('email') ? 'Users.Validation.EmailInvalid' : 'Users.Validation.EmailRequired') | translate }}</p>
                }
                <label class="label mt-1 cursor-pointer gap-2 text-sm text-base-content">
                  <input type="checkbox" class="checkbox checkbox-sm checkbox-primary" formControlName="emailConfirmed" />{{ 'Users.Fields.EmailVerified' | translate }}
                </label>
              </fieldset>
              <fieldset class="fieldset">
                <legend class="fieldset-legend">{{ 'Users.Fields.UserName' | translate }} *</legend>
                <input class="input w-full font-mono" formControlName="userName" maxlength="256" autocomplete="off"
                  [readonly]="details.controls.emailAsUserName.value" [class.input-error]="invalid('userName')" />
                @if (invalid('userName')) { <p class="label text-error">{{ 'Users.Validation.UserNameRequired' | translate }}</p> }
                <label class="label mt-1 cursor-pointer gap-2 text-sm text-base-content">
                  <input type="checkbox" class="checkbox checkbox-sm checkbox-primary" formControlName="emailAsUserName" (change)="syncUserName()" />
                  {{ 'Users.Add.EmailAsUserName' | translate }}
                </label>
              </fieldset>
              <fieldset class="fieldset">
                <legend class="fieldset-legend">{{ 'Common.PhoneNumber' | translate }}</legend>
                <input type="tel" class="input w-full tabular" formControlName="phoneNumber" autocomplete="off" />
                <label class="label mt-1 cursor-pointer gap-2 text-sm text-base-content">
                  <input type="checkbox" class="checkbox checkbox-sm checkbox-primary" formControlName="phoneNumberConfirmed" />{{ 'Users.Fields.PhoneVerified' | translate }}
                </label>
              </fieldset>
              <fieldset class="fieldset">
                <legend class="fieldset-legend">{{ 'Users.Fields.Password' | translate }} *</legend>
                <label class="input w-full" [class.input-error]="invalid('password')">
                  <input [type]="revealPassword() ? 'text' : 'password'" class="grow [&::-ms-reveal]:hidden" formControlName="password" autocomplete="new-password" />
                  <app-generate-password-button [userName]="detailsValue().emailAsUserName ? detailsValue().email : detailsValue().userName"
                    (generated)="usePassword($event)" />
                  @if (detailsValue().password) { <app-copy-button [value]="detailsValue().password" [label]="'Users.Fields.Password' | translate" /> }
                  <button type="button" class="btn btn-ghost btn-xs btn-square" [attr.aria-label]="(revealPassword() ? 'Users.Password.Hide' : 'Users.Password.Show') | translate"
                    (click)="revealPassword.set(!revealPassword())">
                    @if (revealPassword()) { <svg lucideEyeOff class="size-4"></svg> } @else { <svg lucideEye class="size-4"></svg> }
                  </button>
                </label>
                @if (invalid('password')) { <p class="label text-error">{{ 'Users.Validation.PasswordRequired' | translate }}</p> }
                @if (!detailsValue().bypassPasswordValidation) {
                  <app-password-rules class="mt-2 block" [password]="detailsValue().password"
                    [userName]="detailsValue().emailAsUserName ? detailsValue().email : detailsValue().userName" />
                }
                <label class="label mt-1 cursor-pointer gap-2 text-sm text-base-content">
                  <input type="checkbox" class="checkbox checkbox-sm checkbox-primary" formControlName="changePasswordAfterFirstSignIn" />
                  {{ 'Users.Fields.ChangePasswordAtFirstSignIn' | translate }}
                </label>
                <label class="label cursor-pointer gap-2 text-sm text-base-content">
                  <input type="checkbox" class="checkbox checkbox-sm checkbox-primary" formControlName="bypassPasswordValidation" />
                  {{ 'Users.Fields.BypassPasswordValidation' | translate }}
                </label>
              </fieldset>
            </form>
          }
          @case (1) {
            <div class="max-w-3xl space-y-5">
              <p class="text-sm text-base-content/70">{{ 'Users.Add.ClaimsIntro' | translate }}</p>
              <form class="grid items-start gap-3 sm:grid-cols-[1fr_1fr_auto]" [formGroup]="newClaim" (ngSubmit)="addClaim()">
                <select class="select w-full" formControlName="type" (change)="claimTypeChanged()">
                  <option value="" disabled>{{ 'Users.Claims.SelectClaim' | translate }}</option>
                  @for (type of assignableClaimTypes(); track type.id) {
                    <option [value]="type.name">{{ type.displayName || type.name }}</option>
                  }
                </select>
                <app-claim-value-input formControlName="value" [placeholder]="'Common.Value' | translate" [valueType]="selectedClaimType()?.valueType"
                  [invalid]="newClaim.controls.value.invalid && newClaim.controls.value.touched" />
                <button type="submit" class="btn btn-primary" [disabled]="newClaim.invalid"><svg lucidePlus class="size-4"></svg>{{ 'Common.Add' | translate }}</button>
              </form>
              <div class="panel overflow-x-auto">
                <table class="table">
                  <thead>
                    <tr>
                      <th>{{ 'Users.Claims.Claim' | translate }}</th><th>{{ 'Common.Value' | translate }}</th>
                      <th class="w-12"><span class="sr-only">{{ 'Common.Remove' | translate }}</span></th>
                    </tr>
                  </thead>
                  <tbody>
                    @for (claim of claims(); track $index) {
                      <tr>
                        <td><span class="code-chip">{{ claim.type }}</span></td>
                        <td class="break-all">{{ claim.value }}</td>
                        <td class="text-right">
                          <button type="button" class="btn btn-ghost btn-xs btn-square text-error" [attr.aria-label]="'Users.Shared.RemoveItem' | translate: { name: claim.type }"
                            (click)="removeClaim($index)">
                            <svg lucideTrash class="size-4"></svg>
                          </button>
                        </td>
                      </tr>
                    } @empty {
                      <tr><td colspan="3" class="py-8 text-center text-sm text-base-content/60">{{ 'Users.Add.NoClaims' | translate }}</td></tr>
                    }
                  </tbody>
                </table>
              </div>
            </div>
          }
          @case (2) {
            <app-transfer-list [available]="roles()" [selected]="selectedRoles()" [key]="roleKey" [label]="roleLabel" [sublabel]="roleDescription"
              [availableTitle]="'Users.Roles.AvailableTitle' | translate" [selectedTitle]="'Users.Roles.AssignedTitle' | translate"
              [emptySelected]="'Users.Add.NoRolesAssigned' | translate"
              (added)="selectedRoleNames.update(names => [...names, $event.name!])"
              (removed)="selectedRoleNames.update(names => names.filter(name => name !== $event.name))" />
          }
        }
      </div>

      <div class="flex flex-wrap items-center gap-2 border-t border-base-300 px-6 py-4">
        @if (step() > 0) {
          <button type="button" class="btn btn-ghost" (click)="step.set(step() - 1)" [disabled]="saving()">{{ 'Common.Back' | translate }}</button>
        }
        <span class="flex-1"></span>
        @if (step() < steps.length - 1) {
          <button type="button" class="btn btn-primary" (click)="next()">{{ 'Common.Next' | translate }}</button>
        } @else {
          <button type="button" class="btn btn-ghost" (click)="save(false)" [disabled]="saving()">{{ 'Common.Create' | translate }}</button>
          <button type="button" class="btn btn-primary" (click)="save(true)" [disabled]="saving()">
            @if (saving()) { <span class="loading loading-spinner loading-sm"></span> }
            {{ 'Users.Add.CreateAndConfigure' | translate }}
          </button>
        }
      </div>
    </div>
  `
})
export class UserAddComponent {
  private readonly _api = inject(IdentityApiService);
  private readonly _router = inject(Router);
  private readonly _toast = inject(ToastService);
  private readonly _refresh = inject(RefreshBus);
  private readonly _fb = inject(NonNullableFormBuilder);
  private readonly _translate = inject(TranslateService);

  protected readonly steps = [this._translate.instant('Common.Details'), this._translate.instant('Routes.Claims'), this._translate.instant('Routes.Roles')];
  protected readonly step = signal(0);
  protected readonly saving = signal(false);

  protected readonly details = this._fb.group({
    firstName: ['', [Validators.required, Validators.maxLength(256)]],
    lastName: ['', [Validators.required, Validators.maxLength(256)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
    emailConfirmed: [false],
    emailAsUserName: [!!inject(UiFeaturesService).features()?.emailAsUserName],
    userName: ['', [Validators.required, Validators.maxLength(256)]],
    phoneNumber: [''],
    phoneNumberConfirmed: [false],
    password: ['', Validators.required],
    changePasswordAfterFirstSignIn: [false],
    bypassPasswordValidation: [false]
  });
  protected readonly detailsValue = toSignal(this.details.valueChanges.pipe(map(() => this.details.getRawValue())), { initialValue: this.details.getRawValue() });
  protected readonly revealPassword = signal(false);

  protected usePassword(password: string): void {
    const control = this.details.controls.password;
    control.setValue(password);
    control.markAsDirty();
    control.markAsTouched();
    this.revealPassword.set(true);
  }

  private readonly _claimTypes = toSignal(
    this._api.getClaimTypes(1, 2147483647, 'name+', undefined).pipe(map(result => result.items ?? [])), { initialValue: [] as ClaimTypeInfo[] });
  protected readonly claims = signal<BasicClaimInfo[]>([]);
  protected readonly newClaim = this._fb.group({ type: ['', Validators.required], value: ['', Validators.required] });
  protected readonly selectedClaimType = signal<ClaimTypeInfo | undefined>(undefined);
  protected readonly assignableClaimTypes = computed(() => this._claimTypes().filter(isAssignableClaimType));

  protected readonly roles = toSignal(
    this._api.getRoles(1, 2147483647, 'name+', undefined).pipe(map(result => result.items ?? [])), { initialValue: [] as RoleInfo[] });
  protected readonly selectedRoleNames = signal<string[]>([]);
  protected readonly selectedRoles = computed(() => this.roles().filter(role => this.selectedRoleNames().includes(role.name!)));
  protected readonly roleKey = (role: RoleInfo) => role.id!;
  protected readonly roleLabel = (role: RoleInfo) => role.name ?? '';
  protected readonly roleDescription = (role: RoleInfo) => role.description;

  protected invalid(name: 'firstName' | 'lastName' | 'email' | 'userName' | 'password'): boolean {
    const control = this.details.controls[name];
    return control.invalid && control.touched;
  }

  protected syncUserName(): void {
    const { emailAsUserName, email, userName } = this.details.getRawValue();
    if (emailAsUserName && email !== userName) {
      this.details.controls.userName.setValue(email);
    }
  }

  protected next(): void {
    if (this.step() === 0 && this.details.invalid) {
      this.details.markAllAsTouched();
      return;
    }
    this.step.update(step => Math.min(step + 1, this.steps.length - 1));
  }

  protected claimTypeChanged(): void {
    const type = this._claimTypes().find(t => t.name === this.newClaim.controls.type.value);
    this.selectedClaimType.set(type);
    const value = this.newClaim.controls.value as FormControl<string>;
    value.setValidators(claimValueValidators(type));
    value.setValue('');
  }

  protected addClaim(): void {
    if (this.newClaim.invalid) {
      this.newClaim.markAllAsTouched();
      return;
    }
    const { type, value } = this.newClaim.getRawValue();
    this.claims.update(claims => [...claims, new BasicClaimInfo({ type, value })]);
    this.newClaim.reset({ type: '', value: '' });
    this.selectedClaimType.set(undefined);
  }

  protected removeClaim(index: number): void {
    this.claims.update(claims => claims.filter((_, i) => i !== index));
  }

  protected save(configure: boolean): void {
    if (this.details.invalid) {
      this.details.markAllAsTouched();
      this.step.set(0);
      return;
    }
    const value = this.details.getRawValue();
    const request = new CreateUserRequest({
      firstName: value.firstName,
      lastName: value.lastName,
      userName: value.emailAsUserName ? value.email : value.userName,
      email: value.email,
      emailConfirmed: value.emailConfirmed,
      phoneNumber: value.phoneNumber || undefined,
      phoneNumberConfirmed: value.phoneNumberConfirmed,
      password: value.password,
      changePasswordAfterFirstSignIn: value.changePasswordAfterFirstSignIn,
      bypassPasswordValidation: value.bypassPasswordValidation,
      claims: this.claims(),
      roles: this.selectedRoleNames()
    });
    this.saving.set(true);
    this._api.createUser(request).subscribe({
      next: user => {
        this._toast.success(this._translate.instant('Users.Add.CreatedTitle'), this._translate.instant('Users.Add.CreatedMessage', { name: user.email || user.userName }));
        this._refresh.notify(USERS_REFRESH_SCOPE);
        this._router.navigate(configure ? ['/users', user.id] : ['/users']);
      },
      error: () => this.saving.set(false)
    });
  }
}
