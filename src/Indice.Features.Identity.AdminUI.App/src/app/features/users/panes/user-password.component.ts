import { Component, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideEye, LucideEyeOff } from '@lucide/angular';
import { IdentityApiService, SetPasswordRequest } from 'src/app/core/services/identity-api.service';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { SidePaneLayoutComponent, SidePaneService } from 'src/app/ui/side-pane/side-pane';
import { ToastService } from 'src/app/ui/toast/toast.service';
import { USER_REFRESH_SCOPE } from '../user-store';

/** Side pane that sets a new password for the user in `userId`. */
@Component({
  selector: 'app-user-password',
  imports: [ReactiveFormsModule, SidePaneLayoutComponent, LucideEye, LucideEyeOff],
  template: `
    <app-side-pane-layout title="Set password" subtitle="Replaces the user's current password.">
      <form id="user-password-form" class="space-y-4" [formGroup]="form" (ngSubmit)="submit()" autocomplete="off">
        <fieldset class="fieldset">
          <legend class="fieldset-legend">New password *</legend>
          <label class="input w-full" [class.input-error]="form.controls.password.invalid && form.controls.password.touched">
            <input [type]="reveal() ? 'text' : 'password'" class="grow" formControlName="password" autocomplete="new-password" />
            <button type="button" class="btn btn-ghost btn-xs btn-square" [attr.aria-label]="reveal() ? 'Hide password' : 'Show password'"
              (click)="reveal.set(!reveal())">
              @if (reveal()) { <svg lucideEyeOff class="size-4"></svg> } @else { <svg lucideEye class="size-4"></svg> }
            </button>
          </label>
          @if (form.controls.password.invalid && form.controls.password.touched) { <p class="label text-error">A password is required.</p> }
        </fieldset>
        <div class="space-y-3">
          <label class="flex cursor-pointer items-start gap-3 text-sm">
            <input type="checkbox" class="checkbox checkbox-sm checkbox-primary mt-0.5" formControlName="changePasswordAfterFirstSignIn" />
            <span>Require the user to choose a new password at their next sign in</span>
          </label>
          <label class="flex cursor-pointer items-start gap-3 text-sm">
            <input type="checkbox" class="checkbox checkbox-sm checkbox-primary mt-0.5" formControlName="bypassPasswordValidation" />
            <span>Skip password policy validation</span>
          </label>
          <label class="flex cursor-pointer items-start gap-3 text-sm">
            <input type="checkbox" class="checkbox checkbox-sm checkbox-primary mt-0.5" formControlName="suppressNotification" />
            <span>Do not notify the user</span>
          </label>
        </div>
      </form>
      <div footer class="flex justify-end gap-2 border-t border-base-300 px-6 py-4">
        <button type="button" class="btn btn-ghost" (click)="pane.close()">Cancel</button>
        <button type="submit" form="user-password-form" class="btn btn-primary" [disabled]="form.invalid || saving()">
          @if (saving()) { <span class="loading loading-spinner loading-sm"></span> }
          Set password
        </button>
      </div>
    </app-side-pane-layout>
  `
})
export class UserPasswordComponent {
  private readonly _api = inject(IdentityApiService);
  private readonly _toast = inject(ToastService);
  private readonly _refresh = inject(RefreshBus);
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

  protected submit(): void {
    if (this.form.invalid) {
      return;
    }
    this.saving.set(true);
    this._api.setPassword(this.userId(), new SetPasswordRequest(this.form.getRawValue())).subscribe({
      next: () => {
        this._toast.success('Password set', 'The new password is active.');
        this._refresh.notify(USER_REFRESH_SCOPE);
        this.pane.close();
      },
      error: () => this.saving.set(false)
    });
  }
}
