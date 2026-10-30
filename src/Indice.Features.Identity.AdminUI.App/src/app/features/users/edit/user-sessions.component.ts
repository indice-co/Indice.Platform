import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { filter, switchMap } from 'rxjs';
import { LucideIdCard, LucideLogOut } from '@lucide/angular';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AuthService } from 'src/app/core/services/auth.service';
import { IdentityApiService, ServerSideSessionInfo } from 'src/app/core/services/identity-api.service';
import { CopyButtonComponent } from 'src/app/ui/copy/copy-button.component';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { ToastService } from 'src/app/ui/toast/toast.service';
import { UserStore } from '../user-store';

/** Active server side sessions of the user. */
@Component({
  selector: 'app-user-sessions',
  imports: [DatePipe, TranslatePipe, CopyButtonComponent, EmptyStateComponent, LucideIdCard, LucideLogOut],
  template: `
    <p class="mb-3 text-sm text-base-content/70">{{ 'Users.Sessions.Intro' | translate }}</p>
    <div class="panel overflow-x-auto">
      <table class="table">
        <thead>
          <tr>
            <th>{{ 'Users.Sessions.Session' | translate }}</th>
            <th class="hidden md:table-cell">{{ 'Users.Sessions.Scheme' | translate }}</th>
            <th>{{ 'Common.CreatedAt' | translate }}</th>
            <th class="hidden lg:table-cell">{{ 'Users.Sessions.Renewed' | translate }}</th>
            <th>{{ 'Users.Sessions.Expires' | translate }}</th>
            @if (canEdit) { <th class="w-12"><span class="sr-only">{{ 'Users.Sessions.End' | translate }}</span></th> }
          </tr>
        </thead>
        <tbody>
          @if (items() === null) {
            @for (row of [1, 2]; track row) { <tr><td colspan="6"><div class="skeleton h-4 w-full"></div></td></tr> }
          } @else {
            @for (session of items(); track session.key) {
              <tr class="hover:bg-base-200/60">
                <td>
                  <p class="font-medium">{{ session.displayName || ('Users.Sessions.Session' | translate) }}</p>
                  <p class="flex items-center gap-1 text-xs text-base-content/60">
                    <span class="font-mono">{{ session.sessionId }}</span><app-copy-button [value]="session.sessionId" [label]="'Users.Sessions.SessionIdLabel' | translate" />
                  </p>
                </td>
                <td class="hidden md:table-cell"><span class="code-chip">{{ session.scheme }}</span></td>
                <td class="tabular whitespace-nowrap text-base-content/75">{{ session.created | date: 'short' }}</td>
                <td class="hidden tabular whitespace-nowrap text-base-content/75 lg:table-cell">{{ session.renewed | date: 'short' }}</td>
                <td class="tabular whitespace-nowrap text-base-content/75">{{ session.expires ? (session.expires | date: 'short') : ('Common.Never' | translate) }}</td>
                @if (canEdit) {
                  <td class="text-right">
                    <button type="button" class="btn btn-ghost btn-xs btn-square text-error" [attr.aria-label]="'Users.Sessions.EndSession' | translate"
                      [attr.title]="'Users.Sessions.EndSession' | translate" (click)="remove(session)">
                      <svg lucideLogOut class="size-4"></svg>
                    </button>
                  </td>
                }
              </tr>
            } @empty {
              <tr>
                <td colspan="6">
                  <app-empty-state [title]="'Users.Sessions.EmptyTitle' | translate" [message]="'Users.Sessions.EmptyMessage' | translate">
                    <svg icon lucideIdCard class="size-5"></svg>
                  </app-empty-state>
                </td>
              </tr>
            }
          }
        </tbody>
      </table>
    </div>
  `
})
export class UserSessionsComponent implements OnInit {
  private readonly _api = inject(IdentityApiService);
  private readonly _dialog = inject(DialogService);
  private readonly _toast = inject(ToastService);
  private readonly _store = inject(UserStore);
  private readonly _translate = inject(TranslateService);
  protected readonly canEdit = inject(AuthService).isAdminUIUsersWriter();
  protected readonly items = signal<ServerSideSessionInfo[] | null>(null);

  public ngOnInit(): void {
    this._api.getUserSessions(this._store.id()).subscribe({
      next: result => this.items.set(result.items ?? []),
      error: () => this.items.set([])
    });
  }

  protected remove(session: ServerSideSessionInfo): void {
    this._dialog
      .confirm({
        title: this._translate.instant('Users.Sessions.EndTitle'),
        message: this._translate.instant('Users.Sessions.EndMessage'),
        confirmLabel: this._translate.instant('Users.Sessions.EndSession'),
        tone: 'danger'
      })
      .pipe(filter(Boolean), switchMap(() => this._api.removeUserSession(this._store.id(), session.sessionId!)))
      .subscribe(() => {
        this.items.update(items => (items ?? []).filter(item => item.key !== session.key));
        this._toast.success(this._translate.instant('Users.Sessions.Ended'));
      });
  }
}
