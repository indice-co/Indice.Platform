import { Component, inject, input, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideEye, LucideEyeOff } from '@lucide/angular';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { map } from 'rxjs';
import { IdentityApiService, SetPasswordRequest } from 'src/app/core/services/identity-api.service';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { CopyButtonComponent } from 'src/app/ui/copy/copy-button.component';
import { SidePaneLayoutComponent, SidePaneService } from 'src/app/ui/side-pane/side-pane';
import { ToastService } from 'src/app/ui/toast/toast.service';
import { GeneratePasswordButtonComponent } from '../generate-password-button.component';
import { PasswordRulesComponent } from '../password-rules.component';
import { USER_REFRESH_SCOPE } from '../user-store';

/** Side pane that sets a new password for the user in `userId`. */
@Component({
  selector: 'app-user-password',
  imports: [ReactiveFormsModule, TranslatePipe, SidePaneLayoutComponent, PasswordRulesComponent, GeneratePasswordButtonComponent, CopyButtonComponent, LucideEye, LucideEyeOff],
  template: `
    <app-side-pane-layout [title]="'Users.Actions.SetPassword' | translate" [subtitle]="'Users.Password.Subtitle' | translate">
      <form id="user-password-form" class="space-y-4" [formGroup]="form" (ngSubmit)="submit()" autocomplete="off">
        <fieldset class="fieldset">
          <legend class="fieldset-legend">{{ 'Users.Password.NewPassword' | translate }} *</legend>
          <label class="input w-full" [class.input-error]="form.controls.password.invalid && form.controls.password.touched">
            <input [type]="reveal() ? 'text' : 'password'" class="grow [&::-ms-reveal]:hidden" formControlName="password" autocomplete="new-password" />
            <app-generate-password-button [userId]="userId()" (generated)="usePassword($event)" />
            @if (value().password) { <app-copy-button [value]="value().password" [label]="'Users.Password.NewPassword' | translate" /> }
            <button type="button" class="btn btn-ghost btn-xs btn-square" [attr.aria-label]="(reveal() ? 'Users.Password.Hide' : 'Users.Password.Show') | translate"
              (click)="reveal.set(!reveal())">
              @if (reveal()) { <svg lucideEyeOff class="size-4"></svg> } @else { <svg lucideEye class="size-4"></svg> }
            </button>
          </label>
          @if (form.controls.password.invalid && form.controls.password.touched) { <p class="label text-error">{{ 'Users.Validation.PasswordRequired' | translate }}</p> }
          @if (!value().bypassPasswordValidation) {
            <app-password-rules class="mt-2 block" [password]="value().password" [userId]="userId()" />
          }
        </fieldset>
        <div class="space-y-3">
          <label class="flex cursor-pointer items-start gap-3 text-sm">
            <input type="checkbox" class="checkbox checkbox-sm checkbox-primary mt-0.5" formControlName="changePasswordAfterFirstSignIn" />
            <span>{{ 'Users.Password.RequireChange' | translate }}</span>
          </label>
          <label class="flex cursor-pointer items-start gap-3 text-sm">
            <input type="checkbox" class="checkbox checkbox-sm checkbox-primary mt-0.5" formControlName="bypassPasswordValidation" />
            <span>{{ 'Users.Fields.BypassPasswordValidation' | translate }}</span>
          </label>
          <label class="flex cursor-pointer items-start gap-3 text-sm">
            <input type="checkbox" class="checkbox checkbox-sm checkbox-primary mt-0.5" formControlName="suppressNotification" />
            <span>{{ 'Users.Password.DoNotNotify' | translate }}</span>
          </label>
        </div>
      </form>
      <div footer class="flex justify-end gap-2 border-t border-base-300 px-6 py-4">
        <button type="button" class="btn btn-ghost" (click)="pane.close()">{{ 'Common.Cancel' | translate }}</button>
        <button type="submit" form="user-password-form" class="btn btn-primary" [disabled]="form.invalid || saving()">
          @if (saving()) { <span class="loading loading-spinner loading-sm"></span> }
          {{ 'Users.Actions.SetPassword' | translate }}
        </button>
      </div>
    </app-side-pane-layout>
  `
})
export class UserPasswordComponent {
  private readonly _api = inject(IdentityApiService);
  private readonly _toast = inject(ToastService);
  private readonly _refresh = inject(RefreshBus);
  private readonly _translate = inject(TranslateService);
  protected readonly pane = inject(SidePaneService);

  /** Bound from the `user-password/:userId` pane route. */
  public readonly userId = input.required<string>();

  protected readonly reveal = signal(false);
  protected readonly saving = signal(false);
  protected readonly form = inject(NonNullableFormBuilder).group({
    password: ['', Validators.required],
    changePasswordAfterFirstSignIn: [false],
    bypassPasswordValidation: [false],
    suppressNotification: [false]
  });
  protected readonly value = toSignal(this.form.valueChanges.pipe(map(() => this.form.getRawValue())), { initialValue: this.form.getRawValue() });

  protected usePassword(password: string): void {
    const control = this.form.controls.password;
    control.setValue(password);
    control.markAsDirty();
    control.markAsTouched();
    this.reveal.set(true);
  }

  protected submit(): void {
    if (this.form.invalid) {
      return;
    }
    this.saving.set(true);
    this._api.setPassword(this.userId(), new SetPasswordRequest(this.form.getRawValue())).subscribe({
      next: () => {
        this._toast.success(this._translate.instant('Users.Password.SetToast'), this._translate.instant('Users.Password.SetMessage'));
        this._refresh.notify(USER_REFRESH_SCOPE);
        this.pane.close();
      },
      error: () => this.saving.set(false)
    });
  }
}
