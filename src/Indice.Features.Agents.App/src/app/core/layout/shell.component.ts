import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  afterRenderEffect,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterOutlet } from '@angular/router';
import { AuthService } from '@indice/ng-auth';
import { filter } from 'rxjs';

import { AuthGuestService } from '../auth/auth-guest.service';
import { injectSignedIn } from '../auth/auth-state';
import { ConversationsStore } from '../services/conversations.store';
import { AppSidebarComponent } from './app-sidebar.component';

/** Where the desktop rail remembers whether it was left collapsed. */
const COLLAPSED_KEY = 'dex.rail.collapsed';

/**
 * App shell, shared by guests and signed-in users: a conversation rail on the left over a routed
 * page. The rail frames every route, collapses to an icon strip from `md` up, and slides in as a
 * drawer below it. While nobody is signed in, a slim bar atop the main column — the message
 * centered, a Log in button at the right — invites the visitor to sign in.
 */
@Component({
  selector: 'app-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, RouterOutlet, AppSidebarComponent],
  host: {
    '(document:keydown.escape)': 'onEscape()',
  },
  template: `
    <div class="flex h-screen bg-base-200">
      <!-- Desktop rail. Inert behind an open drawer, so "modal" is true and Tab can't reach it. -->
      <div
        class="hidden shrink-0 border-r border-base-300 transition-[width] duration-200 md:block"
        [class.w-16]="collapsed()"
        [class.w-72]="!collapsed()"
        [class.lg:w-80]="!collapsed()"
        [attr.inert]="drawerOpen() ? '' : null"
      >
        <app-sidebar
          [sessions]="store.sessions()"
          [activeId]="store.activeId()"
          [loading]="store.loading()"
          [error]="store.error()"
          [collapsed]="collapsed()"
          (select)="openConversation($event)"
          (removed)="removeConversation($event)"
          (create)="newChat()"
          (toggle)="toggleCollapsed()"
          (dismissError)="store.clearError()"
        />
      </div>

      <div class="flex min-w-0 flex-1 flex-col" [attr.inert]="drawerOpen() ? '' : null">
        <!-- Mobile bar: the rail is off-canvas below md, so the burger is the only way in. -->
        <header
          class="flex h-14 shrink-0 items-center gap-3 border-b border-base-300 bg-base-100 px-3
                 md:hidden"
        >
          <button
            #burger
            type="button"
            class="btn btn-ghost btn-sm btn-circle text-base-content/70"
            (click)="drawerOpen.set(true)"
            aria-label="Open conversations"
            [attr.aria-expanded]="drawerOpen()"
          >
            <svg viewBox="0 0 24 24" fill="none" class="size-5" aria-hidden="true">
              <path
                d="M4 7h16M4 12h16M4 17h16"
                stroke="currentColor"
                stroke-width="2"
                stroke-linecap="round"
              />
            </svg>
          </button>

          <a routerLink="/" class="flex min-w-0 items-center gap-2">
            <img src="dex-logo.png" alt="Dex" class="size-7 shrink-0 rounded-full" />
            <span class="text-base font-semibold tracking-tight text-base-content">Dex</span>
          </a>

          @if (signedIn() !== false) {
            <button
              type="button"
              class="btn btn-primary btn-sm btn-circle ml-auto shadow-sm"
              (click)="newChat()"
              aria-label="New chat"
            >
              <svg viewBox="0 0 24 24" fill="none" class="size-4" aria-hidden="true">
                <path
                  d="M12 5v14M5 12h14"
                  stroke="currentColor"
                  stroke-width="2"
                  stroke-linecap="round"
                />
              </svg>
            </button>
          } @else {
            <button
              type="button"
              class="btn btn-primary btn-sm ml-auto rounded-full shadow-sm"
              (click)="signIn()"
            >
              Sign in
            </button>
          }
        </header>

        <!-- Guest CTA: a slim bar atop the main column — the message centered, the Log in button at
             the right — only while nobody is signed in. Hidden (not rendered) until the OIDC user is
             known, so a signed-in user never sees it flash. -->
        @if (signedIn() === false) {
          <div
            class="hidden h-12 shrink-0 grid-cols-3 items-center border-b border-base-300
                   bg-base-100 px-4 sm:px-6 md:grid"
          >
            <span
              class="col-start-2 hidden justify-self-center text-sm text-base-content/60 sm:inline"
            >
              You're chatting as a guest.
            </span>
            <button
              type="button"
              class="btn btn-primary btn-sm col-start-3 justify-self-end rounded-full"
              (click)="signIn()"
            >
              Sign in
            </button>
          </div>
        }

        <main class="min-h-0 flex-1">
          <router-outlet />
        </main>
      </div>

      <!-- Mobile drawer -->
      @if (drawerOpen()) {
        <div
          class="fixed inset-0 z-40 bg-black/40 md:hidden"
          (click)="closeDrawer()"
          aria-hidden="true"
        ></div>
      }
      <!--
        aria-modal is claimed only while the drawer is open — and only then is it true, because the
        rail and main column above are inert. Closed, it is inert rather than aria-hidden:
        aria-hidden over focusable content is a WCAG anti-pattern, and inert covers both trees.
      -->
      <div
        #drawer
        class="fixed inset-y-0 left-0 z-50 w-72 max-w-[85vw] border-r border-base-300 shadow-xl
               transition-transform duration-200 md:hidden"
        [class.-translate-x-full]="!drawerOpen()"
        [attr.inert]="drawerOpen() ? null : ''"
        [attr.aria-modal]="drawerOpen() ? 'true' : null"
        tabindex="-1"
        role="dialog"
        aria-label="Conversations"
      >
        <app-sidebar
          [sessions]="store.sessions()"
          [activeId]="store.activeId()"
          [loading]="store.loading()"
          [error]="store.error()"
          [collapsed]="false"
          (select)="openConversation($event)"
          (removed)="removeConversation($event)"
          (create)="newChat()"
          (dismissError)="store.clearError()"
          (navigated)="closeDrawer()"
        />
      </div>
    </div>
  `,
})
export class ShellComponent {
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly auth = inject(AuthService);
  private readonly guest = inject(AuthGuestService);

  /** The shared conversation list — the rail reads it, the chat page renders the active thread. */
  protected readonly store = inject(ConversationsStore);
  /** `false` for guests and anonymous visitors; `undefined` until the OIDC user has been read. */
  protected readonly signedIn = injectSignedIn();
  /** Desktop icon-rail state, remembered across reloads. */
  protected readonly collapsed = signal(readCollapsed());
  /** Whether the mobile off-canvas rail is showing. */
  protected readonly drawerOpen = signal(false);

  private readonly drawer = viewChild.required<ElementRef<HTMLElement>>('drawer');
  private readonly burger = viewChild.required<ElementRef<HTMLButtonElement>>('burger');
  /** Guards the restore so the first render doesn't yank focus to the burger. */
  private drawerWasOpen = false;

  constructor() {
    // afterRender, not effect: the `inert` attribute must be off the drawer before we focus into
    // it — focusing inside a still-inert subtree is a no-op.
    afterRenderEffect(() => {
      const open = this.drawerOpen();
      if (open) {
        this.drawer().nativeElement.focus();
      } else if (this.drawerWasOpen) {
        this.burger().nativeElement.focus();
      }
      this.drawerWasOpen = open;
    });

    // Anonymous visitors have nothing to list — and no credential to list it with. The chat page
    // refreshes the rail once their first turn has minted a guest session.
    if (this.auth.getAuthorizationHeaderValue() || this.guest.isActive) {
      this.store.refresh();
    }
    // A back gesture must not leave the drawer covering the page it navigated to.
    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => this.drawerOpen.set(false));
  }

  protected signIn(): void {
    this.auth.signinRedirect({ location: this.router.url });
  }

  protected toggleCollapsed(): void {
    const next = !this.collapsed();
    this.collapsed.set(next);
    try {
      localStorage.setItem(COLLAPSED_KEY, String(next));
    } catch {
      // Private browsing can refuse storage — the preference just won't survive the reload.
    }
  }

  protected closeDrawer(): void {
    this.drawerOpen.set(false);
  }

  protected openConversation(id: string): void {
    this.store.select(id);
    this.router.navigate(['/']);
  }

  protected newChat(): void {
    this.store.startNew();
    this.router.navigate(['/']);
    this.closeDrawer();
  }

  protected removeConversation(id: string): void {
    this.store.remove(id);
  }

  /** Escape closes the drawer — unless a modal (the delete confirmation) is claiming the key. */
  protected onEscape(): void {
    if (!document.querySelector('dialog[open]')) {
      this.closeDrawer();
    }
  }
}

/** Read the remembered rail state; defaults to expanded when storage is unavailable or unset. */
function readCollapsed(): boolean {
  try {
    return localStorage.getItem(COLLAPSED_KEY) === 'true';
  } catch {
    return false;
  }
}
