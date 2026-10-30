import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { filter, switchMap } from 'rxjs';
import { LucideAppWindow, LucideExternalLink, LucideUnlink } from '@lucide/angular';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AuthService } from 'src/app/core/services/auth.service';
import { IdentityApiService, UserClientInfo } from 'src/app/core/services/identity-api.service';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { ToastService } from 'src/app/ui/toast/toast.service';
import { UserStore } from '../user-store';

/** Applications the user has granted access to (consents, refresh / reference tokens), with revocation. */
@Component({
  selector: 'app-user-applications',
  imports: [DatePipe, TranslatePipe, EmptyStateComponent, LucideAppWindow, LucideExternalLink, LucideUnlink],
  template: `
    <div class="mb-3 flex flex-wrap items-center justify-between gap-3">
      <p class="text-sm text-base-content/70">{{ 'Users.Applications.Intro' | translate }}</p>
      @if (canEdit && (items()?.length ?? 0) > 1) {
        <button type="button" class="btn btn-sm btn-error btn-soft" (click)="revokeAll()"><svg lucideUnlink class="size-4"></svg>{{ 'Users.Applications.RevokeAll' | translate }}</button>
      }
    </div>

    @if (items() === null) {
      <div class="grid gap-4 lg:grid-cols-2">@for (row of [1, 2]; track row) { <div class="skeleton h-40"></div> }</div>
    } @else {
      <div class="grid gap-4 lg:grid-cols-2">
        @for (app of items(); track app.clientId) {
          <article class="panel flex flex-col p-5">
            <div class="flex items-start gap-3">
              <span class="grid size-10 shrink-0 place-items-center overflow-hidden rounded-field bg-base-200 text-base-content/60">
                @if (app.logoUri) { <img [src]="app.logoUri" alt="" class="size-full object-contain" /> } @else { <svg lucideAppWindow class="size-5"></svg> }
              </span>
              <div class="min-w-0 flex-1">
                <h3 class="truncate font-semibold">{{ app.clientName || app.clientId }}</h3>
                <p class="truncate font-mono text-xs text-base-content/60">{{ app.clientId }}</p>
              </div>
              @if (app.clientUri) {
                <a class="btn btn-ghost btn-xs btn-square" [href]="app.clientUri" target="_blank" rel="noopener" [attr.aria-label]="'Users.Applications.OpenApplication' | translate">
                  <svg lucideExternalLink class="size-4"></svg>
                </a>
              }
            </div>
            @if (app.description) { <p class="mt-3 text-sm text-base-content/75">{{ app.description }}</p> }
            <dl class="mt-3 grid grid-cols-2 gap-2 text-xs">
              <div><dt class="text-base-content/60">{{ 'Users.Applications.Granted' | translate }}</dt><dd class="tabular">{{ app.createdAt | date: 'medium' }}</dd></div>
              <div><dt class="text-base-content/60">{{ 'Users.Applications.Expires' | translate }}</dt>
                <dd class="tabular">{{ app.expiresAt ? (app.expiresAt | date: 'medium') : ('Common.Never' | translate) }}</dd></div>
            </dl>
            @if (app.types?.length) {
              <div class="mt-3 flex flex-wrap gap-1">
                @for (type of app.types; track type) { <span class="badge badge-sm badge-soft badge-info">{{ type }}</span> }
              </div>
            }
            @if (app.scopes?.length) {
              <div class="mt-2 flex flex-wrap gap-1">
                @for (scope of app.scopes; track scope) { <span class="code-chip text-xs">{{ scope }}</span> }
              </div>
            }
            @if (canEdit) {
              <div class="mt-4 flex justify-end border-t border-base-200 pt-3">
                <button type="button" class="btn btn-ghost btn-sm text-error" (click)="revoke(app)"><svg lucideUnlink class="size-4"></svg>{{ 'Users.Applications.RevokeAccess' | translate }}</button>
              </div>
            }
          </article>
        } @empty {
          <div class="panel lg:col-span-2">
            <app-empty-state [title]="'Users.Applications.EmptyTitle' | translate" [message]="'Users.Applications.EmptyMessage' | translate">
              <svg icon lucideAppWindow class="size-5"></svg>
            </app-empty-state>
          </div>
        }
      </div>
    }
  `
})
export class UserApplicationsComponent implements OnInit {
  private readonly _api = inject(IdentityApiService);
  private readonly _dialog = inject(DialogService);
  private readonly _toast = inject(ToastService);
  private readonly _store = inject(UserStore);
  private readonly _translate = inject(TranslateService);
  protected readonly canEdit = inject(AuthService).isAdminUIUsersWriter();
  protected readonly items = signal<UserClientInfo[] | null>(null);

  public ngOnInit(): void {
    this._api.getUserApplications(this._store.id()).subscribe({
      next: result => this.items.set(result.items ?? []),
      error: () => this.items.set([])
    });
  }

  protected revoke(app: UserClientInfo): void {
    this._dialog
      .confirm({
        title: this._translate.instant('Users.Applications.RevokeTitle'),
        message: this._translate.instant('Users.Applications.RevokeMessage', { name: this._store.displayName(), app: app.clientName || app.clientId }),
        confirmLabel: this._translate.instant('Users.Applications.Revoke'),
        tone: 'danger'
      })
      .pipe(filter(Boolean), switchMap(() => this._api.revokeUserApplicationAccess(this._store.id(), app.clientId!)))
      .subscribe(() => {
        this.items.update(items => (items ?? []).filter(item => item.clientId !== app.clientId));
        this._toast.success(
          this._translate.instant('Users.Applications.AccessRevoked'),
          this._translate.instant('Users.Applications.AccessRevokedMessage', { app: app.clientName || app.clientId }));
      });
  }

  protected revokeAll(): void {
    this._dialog
      .confirm({
        title: this._translate.instant('Users.Applications.RevokeAllTitle'),
        message: this._translate.instant('Users.Applications.RevokeAllMessage', { name: this._store.displayName() }),
        confirmLabel: this._translate.instant('Users.Applications.RevokeAll'),
        tone: 'danger'
      })
      .pipe(filter(Boolean), switchMap(() => this._api.revokeAllUserApplicationAccess(this._store.id())))
      .subscribe(() => {
        this.items.set([]);
        this._toast.success(this._translate.instant('Users.Applications.AccessRevoked'), this._translate.instant('Users.Applications.AllRevokedMessage'));
      });
  }
}
