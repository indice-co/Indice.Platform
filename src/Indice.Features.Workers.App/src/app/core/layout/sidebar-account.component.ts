import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { AuthService, ImgUserPictureDirective } from '@indice/ng-auth';
import { LucideChevronsUpDown, LucideLogOut } from '@lucide/angular';

/**
 * The rail's account anchor: avatar + identity at the bottom-left, opening a popover *upward*
 * with sign-out. The whole shell is behind the auth guard, so there is always a signed-in user.
 */
@Component({
  selector: 'app-sidebar-account',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ImgUserPictureDirective, LucideChevronsUpDown, LucideLogOut],
  template: `
    <div class="dropdown dropdown-top w-full">
      <button
        type="button"
        tabindex="0"
        class="flex w-full items-center gap-3 rounded-field px-3 py-2 text-left transition hover:bg-base-200"
        [class.justify-center]="collapsed()"
        [attr.aria-label]="collapsed() ? userName() + ' — account menu' : null"
        [title]="userName()"
      >
        <span class="avatar">
          <span class="size-8 overflow-hidden rounded-full bg-base-300">
            @if (userId()) {
              <img [userPicture]="userId()!" [displayName]="userName()" [size]="64" alt="" />
            }
          </span>
        </span>
        @if (!collapsed()) {
          <span class="min-w-0 flex-1">
            <span class="block truncate font-medium text-base-content">{{ userName() }}</span>
            <span class="block truncate text-xs opacity-70">{{ userEmail() }}</span>
          </span>
          <svg lucideChevronsUpDown class="size-4 shrink-0 opacity-60"></svg>
        }
      </button>
      <ul
        tabindex="0"
        class="dropdown-content menu z-50 mb-2 w-full rounded-box border border-base-300 bg-base-100 p-1 text-base-content shadow-lg"
      >
        <li>
          <button type="button" (click)="logout()"><svg lucideLogOut class="size-4"></svg>Sign out</button>
        </li>
      </ul>
    </div>
  `,
})
export class SidebarAccountComponent {
  private readonly auth = inject(AuthService);

  /** Render avatar-only, for the collapsed icon rail. */
  readonly collapsed = input(false);
  /** Raised when a menu item was picked — the shell closes the mobile drawer on it. */
  readonly navigated = output<void>();

  private readonly _user = toSignal(this.auth.user$, { initialValue: this.auth.getCurrentUser() });
  protected readonly userId = () => this._user()?.profile?.sub;
  protected readonly userName = () => this._user()?.profile?.given_name ? `${this._user()?.profile?.given_name} ${this._user()?.profile?.family_name ?? ''}`.trim() :
                                      this._user()?.profile?.name ?? this._user()?.profile?.email ?? '';
  protected readonly userEmail = () => this._user()?.profile?.email ?? '';

  protected logout(): void {
    this.navigated.emit();
    this.auth.signoutRedirect();
  }
}
