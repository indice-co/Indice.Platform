import { Component, OnInit, inject, signal } from '@angular/core';
import { filter, switchMap } from 'rxjs';
import { LucideLink, LucideTrash } from '@lucide/angular';
import { AuthService } from 'src/app/core/services/auth.service';
import { IdentityApiService, UserLoginProviderInfo } from 'src/app/core/services/identity-api.service';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { ToastService } from 'src/app/ui/toast/toast.service';
import { UserStore } from '../user-store';

/** External identity providers linked to the user (Google, Microsoft, gov.gr, …). */
@Component({
  selector: 'app-user-logins',
  imports: [EmptyStateComponent, LucideLink, LucideTrash],
  template: `
    <p class="mb-3 text-sm text-base-content/70">External identity providers the user can sign in with.</p>
    <div class="panel overflow-x-auto">
      <table class="table">
        <thead>
          <tr><th>Provider</th><th>Display name</th><th>Provider key</th>@if (canEdit) { <th class="w-12"><span class="sr-only">Remove</span></th> }</tr>
        </thead>
        <tbody>
          @if (items() === null) {
            @for (row of [1, 2]; track row) { <tr><td colspan="4"><div class="skeleton h-4 w-full"></div></td></tr> }
          } @else {
            @for (login of items(); track login.name + login.key) {
              <tr class="hover:bg-base-200/60">
                <td>
                  <span class="flex items-center gap-2 font-medium">
                    <img class="size-5 object-contain" [src]="'assets/img/idps/' + login.name + '.svg'" alt="" (error)="$any($event.target).hidden = true" />
                    {{ login.name }}
                  </span>
                </td>
                <td>{{ login.displayName || '—' }}</td>
                <td><span class="code-chip">{{ login.key }}</span></td>
                @if (canEdit) {
                  <td class="text-right">
                    <button type="button" class="btn btn-ghost btn-xs btn-square text-error" [attr.aria-label]="'Remove ' + login.name" (click)="remove(login)">
                      <svg lucideTrash class="size-4"></svg>
                    </button>
                  </td>
                }
              </tr>
            } @empty {
              <tr>
                <td colspan="4">
                  <app-empty-state title="No external logins" message="This user signs in with a local account only.">
                    <svg icon lucideLink class="size-5"></svg>
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
export class UserLoginsComponent implements OnInit {
  private readonly _api = inject(IdentityApiService);
  private readonly _dialog = inject(DialogService);
  private readonly _toast = inject(ToastService);
  private readonly _store = inject(UserStore);
  protected readonly canEdit = inject(AuthService).isAdminUIUsersWriter();
  protected readonly items = signal<UserLoginProviderInfo[] | null>(null);

  public ngOnInit(): void {
    this._api.getUserExternalLogins(this._store.id()).subscribe({
      next: result => this.items.set(result.items ?? []),
      error: () => this.items.set([])
    });
  }

  protected remove(login: UserLoginProviderInfo): void {
    this._dialog
      .confirm({
        title: 'Remove external login?',
        message: `${this._store.displayName()} will no longer be able to sign in with ${login.name}.`,
        confirmLabel: 'Remove',
        tone: 'danger'
      })
      .pipe(filter(Boolean), switchMap(() => this._api.deleteUserExternalLogin(this._store.id(), login.name!, login.key!)))
      .subscribe(() => {
        this.items.update(items => (items ?? []).filter(item => !(item.name === login.name && item.key === login.key)));
        this._toast.success('External login removed', `'${login.name}' was unlinked.`);
      });
  }
}
