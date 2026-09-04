import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';

import { ConversationListItem } from '../services/dex-api.service';
import { PoweredByComponent } from './powered-by.component';
import { SidebarAccountComponent } from './sidebar-account.component';

/** How many sessions the collapsed icon rail shows as dots before it stops. */
const COLLAPSED_LIMIT = 8;

/**
 * The application rail: brand, new chat, search, the caller's conversations and the account
 * anchor at the bottom. Presentational — the shell owns the data and the routing.
 *
 * Two shapes share one component: `collapsed` renders the desktop icon strip, and the same
 * expanded markup serves both the wide desktop rail and the mobile drawer.
 */
@Component({
  selector: 'app-sidebar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, RouterLink, PoweredByComponent, SidebarAccountComponent],
  template: `
    <aside class="flex h-full w-full flex-col overflow-hidden bg-base-100">
      <!-- Brand + collapse control -->
      <div class="flex shrink-0 flex-col gap-1 px-3 pt-3">
        <div class="flex items-center gap-2.5" [class.justify-center]="collapsed()">
          <a routerLink="/" class="flex min-w-0 items-center gap-2.5" (click)="navigated.emit()">
            <img src="dex-logo.png" alt="Dex" class="size-8 shrink-0 rounded-full" />
            @if (!collapsed()) {
              <span class="min-w-0 leading-tight">
                <span class="block text-lg font-semibold tracking-tight text-base-content">Dex</span>
                <span
                  class="-mt-0.5 block truncate font-mono text-[0.6rem] uppercase
                         tracking-[0.22em] text-base-content/45"
                >
                  knowledge assistant
                </span>
              </span>
            }
          </a>
          @if (!collapsed()) {
            <button
              type="button"
              class="btn btn-ghost btn-sm btn-circle ml-auto hidden text-base-content/50
                     md:inline-flex"
              (click)="toggle.emit()"
              aria-label="Collapse sidebar"
              aria-expanded="true"
              title="Collapse sidebar"
            >
              <svg viewBox="0 0 24 24" fill="none" class="size-5" aria-hidden="true">
                <path
                  d="M15 6l-6 6 6 6"
                  stroke="currentColor"
                  stroke-width="2"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                />
              </svg>
            </button>
          }
        </div>
        @if (collapsed()) {
          <button
            type="button"
            class="btn btn-ghost btn-sm btn-circle mx-auto text-base-content/50"
            (click)="toggle.emit()"
            aria-label="Expand sidebar"
            aria-expanded="false"
            title="Expand sidebar"
          >
            <svg viewBox="0 0 24 24" fill="none" class="size-5" aria-hidden="true">
              <path
                d="M9 6l6 6-6 6"
                stroke="currentColor"
                stroke-width="2"
                stroke-linecap="round"
                stroke-linejoin="round"
              />
            </svg>
          </button>
        }
      </div>

      <!-- New chat -->
      <div class="shrink-0 px-3 pt-3">
        @if (collapsed()) {
          <button
            type="button"
            class="btn btn-primary btn-circle mx-auto flex shadow-sm"
            (click)="onCreate()"
            aria-label="New chat"
            title="New chat"
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
            class="btn btn-primary btn-block justify-start gap-2 shadow-sm"
            (click)="onCreate()"
          >
            <svg viewBox="0 0 24 24" fill="none" class="size-4" aria-hidden="true">
              <path
                d="M12 5v14M5 12h14"
                stroke="currentColor"
                stroke-width="2"
                stroke-linecap="round"
              />
            </svg>
            New chat
          </button>
        }
      </div>

      <!-- Search -->
      <div class="shrink-0 px-3 py-3">
        @if (collapsed()) {
          <button
            type="button"
            class="btn btn-ghost btn-circle mx-auto flex text-base-content/50"
            (click)="toggle.emit()"
            aria-label="Search conversations"
            title="Search conversations"
          >
            <svg viewBox="0 0 24 24" fill="none" class="size-4" aria-hidden="true">
              <circle cx="11" cy="11" r="7" stroke="currentColor" stroke-width="2" />
              <path d="m20 20-3-3" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
            </svg>
          </button>
        } @else {
          <label
            class="flex items-center gap-2 rounded-field border border-base-300 bg-base-200 px-3"
          >
            <svg
              viewBox="0 0 24 24"
              fill="none"
              class="size-4 text-base-content/40"
              aria-hidden="true"
            >
              <circle cx="11" cy="11" r="7" stroke="currentColor" stroke-width="2" />
              <path d="m20 20-3-3" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
            </svg>
            <input
              type="search"
              class="w-full bg-transparent py-2 text-sm outline-none
                     placeholder:text-base-content/40"
              placeholder="Search conversations"
              [value]="search()"
              (input)="onSearch($event)"
            />
          </label>
        }
      </div>

      <!-- Delete failures surface here; the rail is the only place the list is shown. -->
      @if (error(); as message) {
        <div class="shrink-0 px-3 pb-2">
          <div role="alert" class="alert alert-error alert-soft px-3 py-2 text-xs">
            <span class="flex-1">{{ collapsed() ? '!' : message }}</span>
            <button
              type="button"
              class="btn btn-ghost btn-xs btn-circle"
              (click)="dismissError.emit()"
              aria-label="Dismiss"
            >
              &#10005;
            </button>
          </div>
        </div>
      }

      <!-- Conversations -->
      <nav class="dex-scroll flex-1 overflow-x-hidden overflow-y-auto pb-3" [class.px-2]="!collapsed()">
        @if (loading()) {
          <div class="space-y-2 px-2 pt-2">
            @for (i of skeletons; track i) {
              <div
                class="animate-pulse rounded-field bg-base-200"
                [class.h-12]="!collapsed()"
                [class.h-9]="collapsed()"
              ></div>
            }
          </div>
        } @else if (collapsed()) {
          <ul class="flex w-full max-w-full flex-col items-center gap-1 pt-1">
            @for (s of collapsedSessions(); track s.id) {
              <li class="tooltip tooltip-right max-w-full" [attr.data-tip]="titleOf(s)">
                <button
                  type="button"
                  class="grid size-9 place-items-center rounded-field transition-colors
                         hover:bg-base-200"
                  [class.bg-base-200]="s.id === activeId()"
                  (click)="choose(s)"
                  [attr.aria-label]="titleOf(s)"
                >
                  <span
                    class="size-2 rounded-full transition-colors"
                    [class.bg-primary]="s.id === activeId()"
                    [class.bg-base-300]="s.id !== activeId()"
                  ></span>
                </button>
              </li>
            }
          </ul>
        } @else if (filtered().length === 0) {
          <p class="px-3 pt-8 text-center text-sm text-base-content/45">
            {{ sessions().length === 0 ? 'No conversations yet.' : 'No matches.' }}
          </p>
        } @else {
          <ul class="space-y-0.5">
            @for (s of filtered(); track s.id) {
              <li>
                <div
                  class="group flex cursor-pointer items-center gap-2 rounded-field px-3 py-2.5
                         transition-colors"
                  [class.bg-base-200]="s.id === activeId()"
                  [class.hover:bg-base-200]="s.id !== activeId()"
                  (click)="choose(s)"
                >
                  <span
                    class="mt-1.5 size-1.5 shrink-0 self-start rounded-full transition-colors"
                    [class.bg-primary]="s.id === activeId()"
                    [class.bg-base-300]="s.id !== activeId()"
                  ></span>
                  <div class="min-w-0 flex-1">
                    <p class="truncate text-sm font-medium text-base-content">{{ titleOf(s) }}</p>
                    <p class="mt-0.5 truncate font-mono text-[0.68rem] text-base-content/45">
                      {{ s.lastActivityAt | date: 'MMM d · HH:mm' }}
                    </p>
                  </div>
                  <!-- Always visible on touch; hover/focus-revealed from md up. -->
                  <button
                    type="button"
                    class="btn btn-ghost btn-xs btn-circle text-base-content/40 transition
                           hover:text-error md:opacity-0 md:group-hover:opacity-100
                           md:focus-visible:opacity-100"
                    (click)="askRemove($event, s)"
                    aria-label="Delete conversation"
                  >
                    <svg viewBox="0 0 24 24" fill="none" class="size-4" aria-hidden="true">
                      <path
                        d="M5 7h14M10 11v6M14 11v6M6 7l1 12a2 2 0 0 0 2 2h6a2 2 0 0 0 2-2l1-12M9 7V4h6v3"
                        stroke="currentColor"
                        stroke-width="1.7"
                        stroke-linecap="round"
                        stroke-linejoin="round"
                      />
                    </svg>
                  </button>
                </div>
              </li>
            }
          </ul>
        }
      </nav>

      <!-- Account anchor -->
      <div class="shrink-0 border-t border-base-300 px-2 py-2">
        @if (!collapsed()) {
          <div class="pb-1.5">
            <app-powered-by />
          </div>
        }
        <app-sidebar-account [collapsed]="collapsed()" (navigated)="navigated.emit()" />
      </div>
    </aside>

    <!-- Deletion is server-side and irreversible, so it always goes through a confirmation. -->
    <dialog #confirmDialog class="modal" (close)="pending.set(null)">
      <div class="modal-box max-w-sm">
        <h3 class="text-lg font-semibold text-base-content">Delete conversation?</h3>
        <p class="mt-2 text-sm text-base-content/70">
          &ldquo;{{ pendingTitle() }}&rdquo; will be removed permanently. This cannot be undone.
        </p>
        <div class="modal-action">
          <button type="button" class="btn btn-ghost" (click)="closeDialog()">Cancel</button>
          <button type="button" class="btn btn-error" (click)="confirmRemove()">Delete</button>
        </div>
      </div>
      <form method="dialog" class="modal-backdrop">
        <button>close</button>
      </form>
    </dialog>
  `,
})
export class AppSidebarComponent {
  /** The caller's conversations, most-recently-active first. */
  readonly sessions = input<ConversationListItem[]>([]);
  /** The open conversation, highlighted in the list. */
  readonly activeId = input<string | null>(null);
  /** Renders skeleton rows while the list is being fetched. */
  readonly loading = input(false);
  /** Desktop icon-rail mode; always `false` inside the mobile drawer. */
  readonly collapsed = input(false);
  /** A list-level failure to surface above the conversations. */
  readonly error = input<string | null>(null);

  /** A conversation was picked. */
  readonly select = output<string>();
  /** A conversation was confirmed for deletion. */
  readonly removed = output<string>();
  /** The new-chat action was invoked. */
  readonly create = output<void>();
  /** The collapse/expand control was used. */
  readonly toggle = output<void>();
  /** The error banner was dismissed. */
  readonly dismissError = output<void>();
  /** Something navigational happened — the shell closes the mobile drawer on it. */
  readonly navigated = output<void>();

  protected readonly skeletons = [1, 2, 3, 4];
  protected readonly search = signal('');
  /** The conversation awaiting confirmation, or `null` when the dialog is closed. */
  protected readonly pending = signal<ConversationListItem | null>(null);

  private readonly confirmDialog =
    viewChild.required<ElementRef<HTMLDialogElement>>('confirmDialog');

  protected readonly filtered = computed(() => {
    const query = this.search().trim().toLowerCase();
    const list = this.sessions();
    if (!query) {
      return list;
    }
    return list.filter((s) => (s.title ?? '').toLowerCase().includes(query));
  });

  /** The handful of most-recent conversations the collapsed rail shows as dots. */
  protected readonly collapsedSessions = computed(() => this.sessions().slice(0, COLLAPSED_LIMIT));

  /** The pending conversation's display title — empty while the dialog is closed. */
  protected readonly pendingTitle = computed(() => {
    const item = this.pending();
    return item ? this.titleOf(item) : '';
  });

  protected titleOf(item: ConversationListItem): string {
    return item.title || 'Untitled conversation';
  }

  protected onSearch(event: Event): void {
    this.search.set((event.target as HTMLInputElement).value);
  }

  protected onCreate(): void {
    this.create.emit();
    this.navigated.emit();
  }

  protected choose(item: ConversationListItem): void {
    if (item.id) {
      this.select.emit(item.id);
      this.navigated.emit();
    }
  }

  protected askRemove(event: MouseEvent, item: ConversationListItem): void {
    event.stopPropagation();
    if (!item.id) {
      return;
    }
    this.pending.set(item);
    this.confirmDialog().nativeElement.showModal();
  }

  protected confirmRemove(): void {
    const item = this.pending();
    if (item?.id) {
      this.removed.emit(item.id);
    }
    this.closeDialog();
  }

  protected closeDialog(): void {
    this.confirmDialog().nativeElement.close();
  }
}
