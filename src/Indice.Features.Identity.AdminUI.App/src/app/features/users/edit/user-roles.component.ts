import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter, switchMap } from 'rxjs';
import { LucideCrown } from '@lucide/angular';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AuthService } from 'src/app/core/services/auth.service';
import { IdentityApiService, RoleInfo, UpdateUserRequest } from 'src/app/core/services/identity-api.service';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { ToastService } from 'src/app/ui/toast/toast.service';
import { TransferListComponent } from 'src/app/ui/transfer/transfer-list.component';
import { UserStore } from '../user-store';

/** Role assignments of the user and the global administrator flag. */
@Component({
  selector: 'app-user-roles',
  imports: [TranslatePipe, TransferListComponent, LucideCrown],
  template: `
    @if (store.user(); as user) {
      <section class="panel mb-6 flex flex-col gap-4 p-5 sm:flex-row sm:items-center">
        <span class="grid size-10 shrink-0 place-items-center rounded-field bg-primary/10 text-primary"><svg lucideCrown class="size-5"></svg></span>
        <div class="min-w-0 flex-1">
          <h2 class="font-semibold">{{ 'Users.Status.GlobalAdmin' | translate }}</h2>
          <p class="text-sm text-base-content/65">{{ 'Users.Roles.GlobalAdminHint' | translate }}</p>
        </div>
        <input type="checkbox" class="toggle toggle-primary" [attr.aria-label]="'Users.Status.GlobalAdmin' | translate" [checked]="user.isAdmin"
          [disabled]="!canEdit || isSelf() || busy()" (change)="toggleAdmin($event)" />
      </section>

      @if (roles() === undefined) {
        <div class="grid gap-4 md:grid-cols-2"><div class="skeleton h-64"></div><div class="skeleton h-64"></div></div>
      } @else {
        <app-transfer-list [available]="roles()!" [selected]="assigned()" [key]="key" [label]="label" [sublabel]="description" [readonly]="!canEdit"
          [availableTitle]="'Users.Roles.AvailableTitle' | translate" [selectedTitle]="'Users.Roles.AssignedTitle' | translate"
          [emptySelected]="'Users.Roles.EmptySelected' | translate" [emptyAvailable]="'Users.Roles.EmptyAvailable' | translate"
          (added)="add($event)" (removed)="remove($event)" />
      }
    }
  `
})
export class UserRolesComponent {
  private readonly _api = inject(IdentityApiService);
  private readonly _auth = inject(AuthService);
  private readonly _toast = inject(ToastService);
  private readonly _dialog = inject(DialogService);
  private readonly _translate = inject(TranslateService);
  protected readonly store = inject(UserStore);
  protected readonly canEdit = this._auth.isAdminUIUsersWriter();
  protected readonly isSelf = computed(() => this.store.id() === this._auth.getSubjectId());
  protected readonly busy = signal(false);
  protected readonly roles = toSignal(this.store.roles$);
  protected readonly assigned = computed(() => {
    const names = this.store.user()?.roles ?? [];
    return (this.roles() ?? []).filter(role => names.includes(role.name!));
  });
  protected readonly key = (role: RoleInfo) => role.id!;
  protected readonly label = (role: RoleInfo) => role.name ?? '';
  protected readonly description = (role: RoleInfo) => role.description;

  protected add(role: RoleInfo): void {
    this._api.addUserRole(this.store.id(), role.id!).subscribe(() => {
      this.store.patch({ roles: [...(this.store.user()?.roles ?? []), role.name!] });
      this._toast.success(
        this._translate.instant('Users.Roles.RoleAssigned'),
        this._translate.instant('Users.Roles.RoleAssignedMessage', { role: role.name, name: this.store.displayName() }));
    });
  }

  protected remove(role: RoleInfo): void {
    this._api.deleteUserRole(this.store.id(), role.id!).subscribe(() => {
      this.store.patch({ roles: (this.store.user()?.roles ?? []).filter(name => name !== role.name) });
      this._toast.success(
        this._translate.instant('Users.Roles.RoleRemoved'),
        this._translate.instant('Users.Roles.RoleRemovedMessage', { role: role.name, name: this.store.displayName() }));
    });
  }

  protected toggleAdmin(event: Event): void {
    const input = event.target as HTMLInputElement;
    const isAdmin = input.checked;
    // keep the toggle in sync with the saved state until the change is confirmed
    input.checked = !isAdmin;
    const user = this.store.user()!;
    this._dialog
      .confirm({
        title: this._translate.instant(isAdmin ? 'Users.Roles.MakeAdminTitle' : 'Users.Roles.RemoveAdminTitle'),
        message: this._translate.instant(isAdmin ? 'Users.Roles.MakeAdminMessage' : 'Users.Roles.RemoveAdminMessage', { name: this.store.displayName() }),
        confirmLabel: this._translate.instant(isAdmin ? 'Users.Roles.MakeAdmin' : 'Common.Remove'),
        tone: isAdmin ? 'default' : 'danger'
      })
      .pipe(
        filter(Boolean),
        switchMap(() => {
          this.busy.set(true);
          return this._api.updateUser(user.id!, new UpdateUserRequest({
            userName: user.userName,
            email: user.email,
            emailConfirmed: user.emailConfirmed,
            phoneNumber: user.phoneNumber,
            phoneNumberConfirmed: user.phoneNumberConfirmed,
            twoFactorEnabled: user.twoFactorEnabled,
            twoFactorPolicy: user.twoFactorPolicy,
            passwordExpirationPolicy: user.passwordExpirationPolicy,
            isAdmin
          }));
        })
      )
      .subscribe({
        next: updated => {
          this.busy.set(false);
          this.store.user.set(updated);
          this._toast.success(
            this._translate.instant('Users.Toasts.UserUpdated'),
            this._translate.instant(isAdmin ? 'Users.Roles.NowAdmin' : 'Users.Roles.NoLongerAdmin'));
        },
        error: () => this.busy.set(false)
      });
  }
}
