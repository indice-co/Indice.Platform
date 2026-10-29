import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { Observable, filter, switchMap } from 'rxjs';
import {
  LucideActivity, LucideAppWindow, LucideArrowLeft, LucideBan, LucideCamera, LucideEllipsisVertical, LucideEraser, LucideIdCard,
  LucideKeyRound, LucideLink, LucideLockOpen, LucideLogIn, LucideMonitorSmartphone, LucideRotateCcw, LucideSend, LucideShieldCheck,
  LucideTags, LucideTimer, LucideTrash, LucideUserCog, LucideUserRoundX
} from '@lucide/angular';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
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
    DatePipe, RouterLink, RouterOutlet, TranslatePipe, RouteTabsComponent, StatusBadgeComponent, CopyButtonComponent, EmptyStateComponent,
    ImgUserPictureDirective, LucideArrowLeft, LucideBan, LucideCamera, LucideEllipsisVertical, LucideEraser, LucideKeyRound, LucideLockOpen,
    LucideRotateCcw, LucideSend, LucideTimer, LucideTrash, LucideUserRoundX
  ],
  template: `
    <a routerLink="/users" class="mb-3 inline-flex items-center gap-1 text-sm text-base-content/65 hover:text-base-content">
      <svg lucideArrowLeft class="size-4"></svg>{{ 'Routes.Users' | translate }}
    </a>

    @if (store.failed()) {
      <div class="panel">
        <app-empty-state [title]="'Users.Edit.NotFoundTitle' | translate" [message]="'Users.Edit.NotFoundMessage' | translate">
          <svg icon lucideUserRoundX class="size-5"></svg>
          <a actions routerLink="/users" class="btn btn-sm">{{ 'Users.Edit.BackToUsers' | translate }}</a>
        </app-empty-state>
      </div>
    } @else {
      <section class="panel mb-6 p-5 sm:p-6">
        @if (user(); as user) {
          <div class="flex flex-col gap-5 sm:flex-row sm:items-center">
            <div class="relative size-20 shrink-0">
              <button type="button" class="group relative size-full overflow-hidden rounded-full bg-base-300 ring-4 ring-base-200"
                [attr.aria-label]="(canEdit ? 'Users.Picture.Change' : 'Users.Picture.Title') | translate" (click)="openPicture()">
                <img class="size-full object-cover" [userPicture]="user.id!" [displayName]="store.displayName()" [size]="256" [version]="pictureVersion()"
                  alt="" />
                @if (canEdit) {
                  <span class="absolute inset-0 grid place-items-center bg-neutral/55 text-neutral-content opacity-0 transition-opacity group-hover:opacity-100 group-focus-visible:opacity-100">
                    <svg lucideCamera class="size-5"></svg>
                  </span>
                }
              </button>
              @if (canEdit) {
                <!-- always visible adorner so the picture editor is discoverable; the avatar button above is the accessible entry point -->
                <button type="button" tabindex="-1" aria-hidden="true" [attr.title]="'Users.Picture.Change' | translate"
                  class="btn btn-circle btn-primary btn-xs absolute -right-1 -bottom-1 shadow-sm ring-2 ring-base-100" (click)="openPicture()">
                  <svg lucideCamera class="size-3.5"></svg>
                </button>
              }
            </div>

            <div class="min-w-0 flex-1 space-y-1.5">
              <div class="flex flex-wrap items-center gap-2">
                <h1 class="truncate text-2xl font-semibold tracking-tight">{{ store.displayName() }}</h1>
                @if (user.isAdmin) { <app-status-badge tone="primary">{{ 'Users.Status.GlobalAdmin' | translate }}</app-status-badge> }
                @if (user.blocked) { <app-status-badge tone="error">{{ 'Users.Status.Blocked' | translate }}</app-status-badge> }
                @if (user.isLocked) { <app-status-badge tone="warning">{{ 'Users.Status.LockedOut' | translate }}</app-status-badge> }
                @if (user.twoFactorEnabled) { <app-status-badge tone="success">{{ 'Users.Status.Mfa' | translate }}</app-status-badge> }
              </div>
              <p class="flex flex-wrap items-center gap-x-3 gap-y-1 text-sm text-base-content/70">
                <span>{{ user.email }}</span>
                @if (user.userName && user.userName !== user.email) { <span class="font-mono text-xs">{{ user.userName }}</span> }
              </p>
              <p class="flex items-center gap-1 text-xs text-base-content/55">
                <span class="font-mono">{{ user.id }}</span><app-copy-button [value]="user.id" [label]="'Users.Edit.UserIdLabel' | translate" />
                <span class="ml-2">{{ 'Users.Edit.LastSignIn' | translate: { date: user.lastSignInDate ? (user.lastSignInDate | date: 'medium') : ('Users.Edit.Never' | translate) } }}</span>
              </p>
            </div>

            @if (canEdit) {
              <div class="flex flex-wrap items-center gap-2">
                @if (user.isLocked) {
                  <button type="button" class="btn btn-sm btn-warning" (click)="unlock()"><svg lucideLockOpen class="size-4"></svg>{{ 'Users.Actions.Unlock' | translate }}</button>
                }
                <button type="button" class="btn btn-sm" [class.btn-error]="!user.blocked" [class.btn-soft]="!user.blocked"
                  [disabled]="isSelf()" (click)="toggleBlock()">
                  <svg lucideBan class="size-4"></svg>{{ (user.blocked ? 'Users.Actions.Unblock' : 'Users.Actions.Block') | translate }}
                </button>
                <div class="dropdown dropdown-end">
                  <button type="button" tabindex="0" class="btn btn-sm btn-ghost btn-square border-base-300" [attr.aria-label]="'Users.Edit.MoreActions' | translate">
                    <svg lucideEllipsisVertical class="size-4"></svg>
                  </button>
                  <ul tabindex="0" class="dropdown-content menu z-30 mt-2 w-64 rounded-box border border-base-300 bg-base-100 p-1 shadow-lg">
                    <li><button type="button" [disabled]="isSelf()" (click)="setPassword()"><svg lucideKeyRound class="size-4"></svg>{{ 'Users.Actions.SetPassword' | translate }}</button></li>
                    <li><button type="button" [disabled]="isSelf()" (click)="removePassword()"><svg lucideEraser class="size-4"></svg>{{ 'Users.Actions.RemovePassword' | translate }}</button></li>
                    @if (!user.emailConfirmed) {
                      <li><button type="button" (click)="resendConfirmation()"><svg lucideSend class="size-4"></svg>{{ 'Users.Actions.ResendConfirmation' | translate }}</button></li>
                    }
                    <li><button type="button" (click)="resetMfa()"><svg lucideRotateCcw class="size-4"></svg>{{ 'Users.Actions.ResetMfa' | translate }}</button></li>
                    <li><button type="button" (click)="resetActionCounter()"><svg lucideTimer class="size-4"></svg>{{ 'Users.Actions.ClearActionLimits' | translate }}</button></li>
                    <li class="my-1 border-t border-base-300"></li>
                    <li><button type="button" class="text-error" [disabled]="isSelf()" (click)="delete()"><svg lucideTrash class="size-4"></svg>{{ 'Users.Actions.DeleteUser' | translate }}</button></li>
                  </ul>
                </div>
              </div>
            }
          </div>
          @if (user.isLocked && user.lockoutEnd) {
            <div role="alert" class="alert alert-soft alert-warning mt-5">
              <span>{{ 'Users.Edit.LockedOutAlert' | translate: { date: (user.lockoutEnd | date: 'medium'), count: user.accessFailedCount } }}</span>
            </div>
          }
          @if (user.blocked) {
            <div role="alert" class="alert alert-soft alert-error mt-5">
              <span>{{ 'Users.Edit.BlockedAlert' | translate }}</span>
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
      <!-- Keep the outlet inside an HTML element: as a direct child of a control flow block it can inherit the SVG namespace of a preceding icon, which makes the routed tab render as an (invisible) SVG element. -->
      <div><router-outlet /></div>
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
  private readonly _translate = inject(TranslateService);
  protected readonly store = inject(UserStore);
  protected readonly canEdit = this._auth.isAdminUIUsersWriter();
  protected readonly user = this.store.user;
  protected readonly pictureVersion = signal(0);
  protected readonly isSelf = computed(() => this.store.id() === this._auth.getSubjectId());

  protected readonly tabs = computed<RouteTab[]>(() => {
    const features = this._features.features();
    const t = (key: string): string => this._translate.instant(key);
    return [
      { label: t('Common.Details'), link: 'details', icon: LucideUserCog },
      { label: t('Routes.Roles'), link: 'roles', icon: LucideShieldCheck },
      { label: t('Routes.Claims'), link: 'claims', icon: LucideTags },
      { label: t('Routes.Applications'), link: 'applications', icon: LucideAppWindow },
      { label: t('Routes.ExternalLogins'), link: 'external-logins', icon: LucideLink },
      { label: t('Routes.Devices'), link: 'devices', icon: LucideMonitorSmartphone },
      { label: t('Routes.Sessions'), link: 'sessions', icon: LucideIdCard },
      { label: t('Routes.SignInLogs'), link: 'sign-in-logs', icon: LucideLogIn, hidden: !features?.signInLogsEnabled },
      { label: t('Routes.ActivityLogs'), link: 'activity-logs', icon: LucideActivity, hidden: !features?.activityLogsEnabled }
    ];
  });

  private get label(): string {
    return this.store.displayName() || this._translate.instant('Users.Edit.TheUser');
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
    const t = this._translate;
    this.confirmAndRun(
      blocked ? t.instant('Users.Edit.BlockTitle') : t.instant('Users.Edit.UnblockTitle'),
      blocked ? t.instant('Users.Edit.BlockMessage', { name: this.label }) : t.instant('Users.Edit.UnblockMessage', { name: this.label }),
      blocked ? t.instant('Users.Actions.Block') : t.instant('Users.Actions.Unblock'),
      () => this._api.setUserBlock(this.store.id(), new SetUserBlockRequest({ blocked })),
      blocked ? t.instant('Users.Edit.BlockedToast') : t.instant('Users.Edit.UnblockedToast'),
      () => this.store.patch({ blocked }),
      blocked
    );
  }

  protected unlock(): void {
    const t = this._translate;
    this.confirmAndRun(t.instant('Users.Edit.UnlockTitle'), t.instant('Users.Edit.UnlockMessage', { name: this.label }), t.instant('Users.Actions.Unlock'),
      () => this._api.unlockUser(this.store.id()), t.instant('Users.Edit.UnlockedToast'),
      () => this.store.patch({ isLocked: false, lockoutEnd: undefined, accessFailedCount: 0 }));
  }

  protected setPassword(): void {
    (document.activeElement as HTMLElement | null)?.blur();
    this._pane.open(['user-password', this.store.id()]);
  }

  protected removePassword(): void {
    const t = this._translate;
    this.confirmAndRun(t.instant('Users.Edit.RemovePasswordTitle'), t.instant('Users.Edit.RemovePasswordMessage', { name: this.label }), t.instant('Common.Remove'),
      () => this._api.removePassword(this.store.id()), t.instant('Users.Edit.PasswordRemovedToast'), undefined, true);
  }

  protected resendConfirmation(): void {
    (document.activeElement as HTMLElement | null)?.blur();
    this._api.resendConfirmationEmail(this.store.id())
      .subscribe(() => this._toast.success(
        this._translate.instant('Users.Edit.EmailSentTitle'), this._translate.instant('Users.Edit.EmailSentMessage', { email: this.user()?.email })));
  }

  protected resetMfa(): void {
    const t = this._translate;
    this.confirmAndRun(t.instant('Users.Edit.ResetMfaTitle'), t.instant('Users.Edit.ResetMfaMessage', { name: this.label }),
      t.instant('Common.Reset'), () => this._api.resetMfa(this.store.id()), t.instant('Users.Edit.MfaResetToast'), () => this.store.reload(), true);
  }

  protected resetActionCounter(): void {
    const t = this._translate;
    this.confirmAndRun(t.instant('Users.Edit.ClearLimitsTitle'), t.instant('Users.Edit.ClearLimitsMessage', { name: this.label }), t.instant('Common.Clear'),
      () => this._api.resetActionCounter(this.store.id()), t.instant('Users.Edit.LimitsClearedToast'), () => this.store.reload());
  }

  protected delete(): void {
    const t = this._translate;
    this.confirmAndRun(t.instant('Users.Edit.DeleteTitle'), t.instant('Users.Edit.DeleteMessage', { name: this.label }),
      t.instant('Common.Delete'), () => this._api.deleteUser(this.store.id()), t.instant('Users.Edit.DeletedToast'), () => {
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
