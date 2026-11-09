import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { AuthService, ImgUserPictureDirective } from '@indice/ng-auth';
import { LucideChevronsUpDown, LucideLogIn, LucideLogOut, LucideUser } from '@lucide/angular';

import { injectSignedIn } from '../auth/auth-state';

/**
 * The rail's account anchor: avatar + identity at the bottom-left, opening a popover *upward*
 * with the profile route and sign-out — the shape ChatGPT / Gemini / Grok use.
 *
 * For a guest (anonymous, or holding only a guest token) the same anchor shows a "G" avatar and
 * the popover offers Log in instead of Profile / Sign out.
 */
@Component({
  selector: 'app-sidebar-account',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, ImgUserPictureDirective, LucideChevronsUpDown, LucideLogIn, LucideLogOut, LucideUser],
  template: `
    <div class="dropdown dropdown-top w-full">
      <button
        type="button"
        tabindex="0"
        class="flex w-full items-center gap-3 rounded-field px-3 py-2 text-left transition hover:bg-base-200"
        [class.justify-center]="collapsed()"
        [attr.aria-label]="collapsed() ? displayName() + ' — account menu' : null"
        [title]="displayName()"
      >
        <span class="avatar">
          <span class="size-8 overflow-hidden rounded-full bg-base-300">
            @if (signedIn() && userId()) {
              <img [userPicture]="userId()!" [displayName]="displayName()" [size]="64" alt="" />
            } @else {
              <!-- Guests have no picture on the identity server — the avatar is just the letter. -->
              <span class="grid size-full place-items-center text-sm font-semibold text-base-content/70" aria-hidden="true">G</span>
            }
          </span>
        </span>
        @if (!collapsed()) {
          <span class="min-w-0 flex-1">
            <span class="block truncate font-medium text-base-content">{{ displayName() }}</span>
            <span class="block truncate text-xs opacity-70">{{ signedIn() ? userEmail() : 'Not signed in' }}</span>
          </span>
          <svg lucideChevronsUpDown class="size-4 shrink-0 opacity-60"></svg>
        }
      </button>
      <ul
        tabindex="0"
        class="dropdown-content menu z-50 mb-2 w-full rounded-box border border-base-300 bg-base-100 p-1 text-base-content shadow-lg"
      >
        @if (signedIn()) {
          <li>
            <a routerLink="/profile" (click)="navigated.emit()"><svg lucideUser class="size-4"></svg>Profile</a>
          </li>
          <li>
            <button type="button" (click)="logout()"><svg lucideLogOut class="size-4"></svg>Sign out</button>
          </li>
        } @else {
          <li>
            <button type="button" (click)="signIn()"><svg lucideLogIn class="size-4"></svg>Sign in</button>
          </li>
        }
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

  /** `false` for guests and anonymous visitors — they share the same "G" treatment. */
  protected readonly signedIn = injectSignedIn();
  private readonly _user = toSignal(this.auth.user$, { initialValue: this.auth.getCurrentUser() });
  protected readonly userId = () => this._user()?.profile?.sub;
  private readonly userName = () => this._user()?.profile?.given_name ? `${this._user()?.profile?.given_name} ${this._user()?.profile?.family_name ?? ''}`.trim() :
                                    this._user()?.profile?.name ?? this._user()?.profile?.email ?? '';
  protected readonly userEmail = () => this._user()?.profile?.email ?? '';
  protected readonly displayName = computed(() => (this.signedIn() ? this.userName() || 'You' : 'Guest'));

  protected signIn(): void {
    this.navigated.emit();
    this.auth.signinRedirect({ location: '/' });
  }

  protected logout(): void {
    this.navigated.emit();
    this.auth.signoutRedirect();
  }
}
