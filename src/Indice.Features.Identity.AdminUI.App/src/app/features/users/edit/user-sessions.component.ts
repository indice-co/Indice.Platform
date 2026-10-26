import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { filter, switchMap } from 'rxjs';
import { LucideIdCard, LucideLogOut } from '@lucide/angular';
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
  imports: [DatePipe, CopyButtonComponent, EmptyStateComponent, LucideIdCard, LucideLogOut],
  template: `
    <p class="mb-3 text-sm text-base-content/70">Sessions the identity server keeps for this user. Ending a session signs the user out of it.</p>
    <div class="panel overflow-x-auto">
      <table class="table">
        <thead>
          <tr>
            <th>Session</th>
            <th class="hidden md:table-cell">Scheme</th>
            <th>Created</th>
            <th class="hidden lg:table-cell">Renewed</th>
            <th>Expires</th>
            @if (canEdit) { <th class="w-12"><span class="sr-only">End</span></th> }
          </tr>
        </thead>
        <tbody>
          @if (items() === null) {
            @for (row of [1, 2]; track row) { <tr><td colspan="6"><div class="skeleton h-4 w-full"></div></td></tr> }
          } @else {
            @for (session of items(); track session.key) {
              <tr class="hover:bg-base-200/60">
                <td>
                  <p class="font-medium">{{ session.displayName || 'Session' }}</p>
                  <p class="flex items-center gap-1 text-xs text-base-content/60">
                    <span class="font-mono">{{ session.sessionId }}</span><app-copy-button [value]="session.sessionId" label="session id" />
                  </p>
                </td>
                <td class="hidden md:table-cell"><span class="code-chip">{{ session.scheme }}</span></td>
                <td class="tabular whitespace-nowrap text-base-content/75">{{ session.created | date: 'short' }}</td>
                <td class="hidden tabular whitespace-nowrap text-base-content/75 lg:table-cell">{{ session.renewed | date: 'short' }}</td>
                <td class="tabular whitespace-nowrap text-base-content/75">{{ session.expires ? (session.expires | date: 'short') : 'Never' }}</td>
                @if (canEdit) {
                  <td class="text-right">
                    <button type="button" class="btn btn-ghost btn-xs btn-square text-error" aria-label="End session" title="End session" (click)="remove(session)">
                      <svg lucideLogOut class="size-4"></svg>
                    </button>
                  </td>
                }
              </tr>
            } @empty {
              <tr>
                <td colspan="6">
                  <app-empty-state title="No active sessions" message="This user is not signed in anywhere right now.">
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
        title: 'End session?',
        message: 'The session will be deleted. If the user is using it, they will be signed out.',
        confirmLabel: 'End session',
        tone: 'danger'
      })
      .pipe(filter(Boolean), switchMap(() => this._api.removeUserSession(this._store.id(), session.sessionId!)))
      .subscribe(() => {
        this.items.update(items => (items ?? []).filter(item => item.key !== session.key));
        this._toast.success('Session ended');
      });
  }
}
