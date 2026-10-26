import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { Observable, filter, switchMap } from 'rxjs';
import {
  LucideActivity, LucideAppWindow, LucideArrowLeft, LucideBan, LucideCamera, LucideEllipsisVertical, LucideEraser, LucideIdCard,
  LucideKeyRound, LucideLink, LucideLockOpen, LucideLogIn, LucideMonitorSmartphone, LucideRotateCcw, LucideSend, LucideShieldCheck,
  LucideTags, LucideTimer, LucideTrash, LucideUserCog, LucideUserRoundX
} from '@lucide/angular';
import { AuthService } from 'src/app/core/services/auth.service';
import { IdentityApiService, SetUserBlockRequest } from 'src/app/core/services/identity-api.service';
import { UiFeaturesService } from 'src/app/core/services/ui-features.service';
import { ImgUserPictureDirective } from 'src/app/ui/avatar/user-picture.directive';
import { StatusBadgeComponent } from 'src/app/ui/badges/badges';
import { CopyButtonComponent } from 'src/app/ui/copy/copy-button.component';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { SidePaneService } from 'src/app/ui/side-pane/side-pane';
import { RouteTab, RouteTabsComponent } from 'src/app/ui/tabs/route-tabs.component';
import { ToastService } from 'src/app/ui/toast/toast.service';
import { USERS_REFRESH_SCOPE } from '../users.component';
import { UserStore } from '../user-store';
import { ProfilePictureDialogComponent, ProfilePictureDialogData } from './profile-picture-dialog.component';

/** User page: identity card with quick actions on top, routed tabs below. */
@Component({
  selector: 'app-user-edit',
  providers: [UserStore],
  imports: [
    DatePipe, RouterLink, RouterOutlet, RouteTabsComponent, StatusBadgeComponent, CopyButtonComponent, EmptyStateComponent,
    ImgUserPictureDirective, LucideArrowLeft, LucideBan, LucideCamera, LucideEllipsisVertical, LucideEraser, LucideKeyRound, LucideLockOpen,
    LucideRotateCcw, LucideSend, LucideTimer, LucideTrash, LucideUserRoundX
  ],
  template: `
    <a routerLink="/users" class="mb-3 inline-flex items-center gap-1 text-sm text-base-content/65 hover:text-base-content">
      <svg lucideArrowLeft class="size-4"></svg>Users
    </a>

    @if (store.failed()) {
      <div class="panel">
        <app-empty-state title="User not found" message="The user may have been deleted, or you followed a broken link.">
          <svg icon lucideUserRoundX class="size-5"></svg>
          <a actions routerLink="/users" class="btn btn-sm">Back to users</a>
        </app-empty-state>
      </div>
    } @else {
      <section class="panel mb-6 p-5 sm:p-6">
        @if (user(); as user) {
          <div class="flex flex-col gap-5 sm:flex-row sm:items-center">
            <button type="button" class="group relative size-20 shrink-0 overflow-hidden rounded-full bg-base-300 ring-4 ring-base-200"
              [attr.aria-label]="canEdit ? 'Change profile picture' : 'Profile picture'" (click)="openPicture()">
              <img class="size-full object-cover" [userPicture]="user.id!" [displayName]="store.displayName()" [size]="256" [version]="pictureVersion()"
                alt="" />
              @if (canEdit) {
                <span class="absolute inset-0 grid place-items-center bg-neutral/55 text-neutral-content opacity-0 transition-opacity group-hover:opacity-100 group-focus-visible:opacity-100">
                  <svg lucideCamera class="size-5"></svg>
                </span>
              }
            </button>

            <div class="min-w-0 flex-1 space-y-1.5">
              <div class="flex flex-wrap items-center gap-2">
                <h1 class="truncate text-2xl font-semibold tracking-tight">{{ store.displayName() }}</h1>
                @if (user.isAdmin) { <app-status-badge tone="primary">Global administrator</app-status-badge> }
                @if (user.blocked) { <app-status-badge tone="error">Blocked</app-status-badge> }
                @if (user.isLocked) { <app-status-badge tone="warning">Locked out</app-status-badge> }
                @if (user.twoFactorEnabled) { <app-status-badge tone="success">MFA</app-status-badge> }
              </div>
              <p class="flex flex-wrap items-center gap-x-3 gap-y-1 text-sm text-base-content/70">
                <span>{{ user.email }}</span>
                @if (user.userName && user.userName !== user.email) { <span class="font-mono text-xs">{{ user.userName }}</span> }
              </p>
              <p class="flex items-center gap-1 text-xs text-base-content/55">
                <span class="font-mono">{{ user.id }}</span><app-copy-button [value]="user.id" label="user id" />
                <span class="ml-2">Last sign-in {{ user.lastSignInDate ? (user.lastSignInDate | date: 'medium') : 'never' }}</span>
              </p>
            </div>

            @if (canEdit) {
              <div class="flex flex-wrap items-center gap-2">
                @if (user.isLocked) {
                  <button type="button" class="btn btn-sm btn-warning" (click)="unlock()"><svg lucideLockOpen class="size-4"></svg>Unlock</button>
                }
                <button type="button" class="btn btn-sm" [class.btn-error]="!user.blocked" [class.btn-soft]="!user.blocked"
                  [disabled]="isSelf()" (click)="toggleBlock()">
                  <svg lucideBan class="size-4"></svg>{{ user.blocked ? 'Unblock' : 'Block' }}
                </button>
                <div class="dropdown dropdown-end">
                  <button type="button" tabindex="0" class="btn btn-sm btn-ghost btn-square border-base-300" aria-label="More actions">
                    <svg lucideEllipsisVertical class="size-4"></svg>
                  </button>
                  <ul tabindex="0" class="dropdown-content menu z-30 mt-2 w-64 rounded-box border border-base-300 bg-base-100 p-1 shadow-lg">
                    <li><button type="button" [disabled]="isSelf()" (click)="setPassword()"><svg lucideKeyRound class="size-4"></svg>Set password</button></li>
                    <li><button type="button" [disabled]="isSelf()" (click)="removePassword()"><svg lucideEraser class="size-4"></svg>Remove password</button></li>
                    @if (!user.emailConfirmed) {
                      <li><button type="button" (click)="resendConfirmation()"><svg lucideSend class="size-4"></svg>Resend confirmation email</button></li>
                    }
                    <li><button type="button" (click)="resetMfa()"><svg lucideRotateCcw class="size-4"></svg>Reset MFA</button></li>
                    <li><button type="button" (click)="resetActionCounter()"><svg lucideTimer class="size-4"></svg>Clear action limits</button></li>
                    <li class="my-1 border-t border-base-300"></li>
                    <li><button type="button" class="text-error" [disabled]="isSelf()" (click)="delete()"><svg lucideTrash class="size-4"></svg>Delete user</button></li>
                  </ul>
                </div>
              </div>
            }
          </div>
          @if (user.isLocked && user.lockoutEnd) {
            <div role="alert" class="alert alert-soft alert-warning mt-5">
              <span>This user is locked out until {{ user.lockoutEnd | date: 'medium' }} after {{ user.accessFailedCount }} failed sign-in attempts.</span>
            </div>
          }
          @if (user.blocked) {
            <div role="alert" class="alert alert-soft alert-error mt-5">
              <span>This user is blocked and cannot sign in until an administrator unblocks them.</span>
            </div>
          }
        } @else {
          <div class="flex items-center gap-5">
            <div class="skeleton size-20 shrink-0 rounded-full"></div>
            <div class="flex-1 space-y-2"><div class="skeleton h-7 w-64"></div><div class="skeleton h-4 w-48"></div></div>
          </div>
        }
      </section>

      <app-route-tabs class="mb-6" [tabs]="tabs()" />
      <router-outlet />
    }
  `
})
export class UserEditComponent {
  private readonly _api = inject(IdentityApiService);
  private readonly _auth = inject(AuthService);
  private readonly _dialog = inject(DialogService);
  private readonly _toast = inject(ToastService);
  private readonly _router = inject(Router);
  private readonly _refresh = inject(RefreshBus);
  private readonly _pane = inject(SidePaneService);
  private readonly _features = inject(UiFeaturesService);
  protected readonly store = inject(UserStore);
  protected readonly canEdit = this._auth.isAdminUIUsersWriter();
  protected readonly user = this.store.user;
  protected readonly pictureVersion = signal(0);
  protected readonly isSelf = computed(() => this.store.id() === this._auth.getSubjectId());

  protected readonly tabs = computed<RouteTab[]>(() => {
    const features = this._features.features();
    return [
      { label: 'Details', link: 'details', icon: LucideUserCog },
      { label: 'Roles', link: 'roles', icon: LucideShieldCheck },
      { label: 'Claims', link: 'claims', icon: LucideTags },
      { label: 'Applications', link: 'applications', icon: LucideAppWindow },
      { label: 'External logins', link: 'external-logins', icon: LucideLink },
      { label: 'Devices', link: 'devices', icon: LucideMonitorSmartphone },
      { label: 'Sessions', link: 'sessions', icon: LucideIdCard },
      { label: 'Sign-in logs', link: 'sign-in-logs', icon: LucideLogIn, hidden: !features?.signInLogsEnabled },
      { label: 'Activity logs', link: 'activity-logs', icon: LucideActivity, hidden: !features?.activityLogsEnabled }
    ];
  });

  private get label(): string {
    return this.store.displayName() || 'the user';
  }

  protected openPicture(): void {
    const user = this.user();
    if (!user) {
      return;
    }
    this._dialog
      .open<boolean, ProfilePictureDialogData>(ProfilePictureDialogComponent, {
        data: { userId: user.id!, displayName: this.store.displayName(), editable: this.canEdit, version: this.pictureVersion() },
        size: 'md'
      })
      .closed.pipe(filter(Boolean))
      .subscribe(() => this.pictureVersion.update(version => version + 1));
  }

  protected toggleBlock(): void {
    const blocked = !this.user()?.blocked;
    this.confirmAndRun(
      blocked ? 'Block user?' : 'Unblock user?',
      blocked ? `${this.label} will not be able to sign in until unblocked.` : `${this.label} will be able to sign in again.`,
      blocked ? 'Block' : 'Unblock',
      () => this._api.setUserBlock(this.store.id(), new SetUserBlockRequest({ blocked })),
      blocked ? 'User blocked' : 'User unblocked',
      () => this.store.patch({ blocked }),
      blocked
    );
  }

  protected unlock(): void {
    this.confirmAndRun('Unlock user?', `${this.label} will be able to sign in again right away.`, 'Unlock',
      () => this._api.unlockUser(this.store.id()), 'User unlocked',
      () => this.store.patch({ isLocked: false, lockoutEnd: undefined, accessFailedCount: 0 }));
  }

  protected setPassword(): void {
    (document.activeElement as HTMLElement | null)?.blur();
    this._pane.open(['user-password', this.store.id()]);
  }

  protected removePassword(): void {
    this.confirmAndRun('Remove password?', `${this.label} will only be able to sign in with an external login or a passwordless method.`, 'Remove',
      () => this._api.removePassword(this.store.id()), 'Password removed', undefined, true);
  }

  protected resendConfirmation(): void {
    (document.activeElement as HTMLElement | null)?.blur();
    this._api.resendConfirmationEmail(this.store.id())
      .subscribe(() => this._toast.success('Email sent', `A new confirmation email was sent to ${this.user()?.email}.`));
  }

  protected resetMfa(): void {
    this.confirmAndRun('Reset MFA?',
      `${this.label}'s multi-factor configuration, including authenticator app keys, will be cleared. Depending on the MFA policy they will be asked to register again.`,
      'Reset', () => this._api.resetMfa(this.store.id()), 'MFA reset', () => this.store.reload(), true);
  }

  protected resetActionCounter(): void {
    this.confirmAndRun('Clear action limits?', `Rate limit counters of ${this.label} will be reset.`, 'Clear',
      () => this._api.resetActionCounter(this.store.id()), 'Action limits cleared', () => this.store.reload());
  }

  protected delete(): void {
    this.confirmAndRun('Delete user?', `${this.label} will be deleted permanently, along with their claims, roles and logins. This cannot be undone.`,
      'Delete', () => this._api.deleteUser(this.store.id()), 'User deleted', () => {
        this._refresh.notify(USERS_REFRESH_SCOPE);
        this._router.navigate(['/users']);
      }, true);
  }

  private confirmAndRun(title: string, message: string, confirmLabel: string, action: () => Observable<unknown>, success: string,
    after?: () => void, danger = false): void {
    (document.activeElement as HTMLElement | null)?.blur();
    this._dialog
      .confirm({ title, message, confirmLabel, tone: danger ? 'danger' : 'default' })
      .pipe(filter(Boolean), switchMap(action))
      .subscribe(() => {
        this._toast.success(success);
        after?.();
      });
  }
}
