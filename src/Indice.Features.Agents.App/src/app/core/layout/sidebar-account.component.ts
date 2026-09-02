import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService, ImgUserPictureDirective } from '@indice/ng-auth';

import { injectSignedIn } from '../auth/auth-state';
import { initialsOf } from '../models/initials';

/**
 * The rail's account anchor: avatar + identity at the bottom-left, opening a popover *upward*
 * with the profile route and sign-out — the shape ChatGPT / Gemini / Grok use.
 *
 * For a guest (anonymous, or holding only a guest token) the same anchor shows a "G" avatar and
 * the popover offers Log in / Sign up instead of Profile / Sign out.
 */
@Component({
  selector: 'app-sidebar-account',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, ImgUserPictureDirective],
  template: `
    <div class="dropdown dropdown-top w-full">
      <div
        tabindex="0"
        role="button"
        class="flex w-full items-center gap-2.5 rounded-field px-2 py-2 transition
               hover:bg-base-200"
        [class.justify-center]="collapsed()"
        [attr.aria-label]="collapsed() ? displayName() + ' — account menu' : null"
        [title]="collapsed() ? displayName() : ''"
      >
        @if (signedIn()) {
          <img
            [userPicture]="subjectId"
            [displayName]="initials"
            [size]="64"
            class="grid size-9 shrink-0 place-items-center rounded-full bg-primary/10 text-sm
                   font-semibold text-primary"
          />
        } @else {
          <!-- Guests have no picture on the identity server — the avatar is just the letter. -->
          <span
            class="grid size-9 shrink-0 place-items-center rounded-full bg-base-300 text-sm
                   font-semibold text-base-content/70"
            aria-hidden="true"
          >
            G
          </span>
        }
        @if (!collapsed()) {
          <span class="min-w-0 flex-1 text-left">
            <span class="block truncate text-sm font-medium text-base-content">
              {{ displayName() }}
            </span>
            @if (signedIn()) {
              @if (email) {
                <span class="block truncate text-[0.68rem] text-base-content/45">{{ email }}</span>
              }
            } @else {
              <span class="block truncate text-[0.68rem] text-base-content/45">Not signed in</span>
            }
          </span>
          <svg
            viewBox="0 0 24 24"
            fill="none"
            class="size-4 shrink-0 text-base-content/40"
            aria-hidden="true"
          >
            <path
              d="M6 15l6-6 6 6"
              stroke="currentColor"
              stroke-width="2"
              stroke-linecap="round"
              stroke-linejoin="round"
            />
          </svg>
        }
      </div>

      <ul
        tabindex="0"
        class="dropdown-content menu z-30 mb-2 w-60 rounded-box border border-base-300
               bg-base-100 p-2 shadow-lg"
      >
        @if (signedIn()) {
          <li class="menu-title">
            <span class="truncate text-base-content/60">{{ email || displayName() }}</span>
          </li>
          <li>
            <a routerLink="/profile" (click)="navigated.emit()">
              <svg viewBox="0 0 24 24" fill="none" class="size-4" aria-hidden="true">
                <path
                  d="M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8zM5 20a7 7 0 0 1 14 0"
                  stroke="currentColor"
                  stroke-width="1.8"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                />
              </svg>
              Profile
            </a>
          </li>
          <li>
            <button type="button" (click)="logout()">
              <svg viewBox="0 0 24 24" fill="none" class="size-4" aria-hidden="true">
                <path
                  d="M15 17l5-5-5-5M20 12H9M12 3H6a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h6"
                  stroke="currentColor"
                  stroke-width="1.8"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                />
              </svg>
              Sign out
            </button>
          </li>
        } @else {
          <li class="menu-title">
            <span class="text-base-content/60">You're chatting as a guest</span>
          </li>
          <li>
            <button type="button" (click)="signIn()">
              <svg viewBox="0 0 24 24" fill="none" class="size-4" aria-hidden="true">
                <path
                  d="M9 17l-5-5 5-5M4 12h11M12 3h6a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-6"
                  stroke="currentColor"
                  stroke-width="1.8"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                />
              </svg>
              Log in
            </button>
          </li>
          <li>
            <button type="button" (click)="signUp()">
              <svg viewBox="0 0 24 24" fill="none" class="size-4" aria-hidden="true">
                <path
                  d="M10 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8zM3 20a7 7 0 0 1 14 0M19 8v6M16 11h6"
                  stroke="currentColor"
                  stroke-width="1.8"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                />
              </svg>
              Sign up
            </button>
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
  protected readonly displayName = computed(() =>
    this.signedIn() ? this.auth.getDisplayName() || 'You' : 'Guest',
  );
  protected readonly email = this.auth.getEmail() ?? '';
  protected readonly subjectId = this.auth.getSubjectId() ?? '';
  protected readonly initials = initialsOf(this.auth.getDisplayName() || 'You');

  protected signIn(): void {
    this.navigated.emit();
    this.auth.signinRedirect({ location: '/' });
  }

  protected signUp(): void {
    this.navigated.emit();
    this.auth.signinRedirect({ location: '/', promptRegister: true });
  }

  protected logout(): void {
    this.navigated.emit();
    this.auth.signoutRedirect();
  }
}
