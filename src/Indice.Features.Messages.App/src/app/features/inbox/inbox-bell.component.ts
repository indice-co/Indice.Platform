import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideBell, LucideCheckCheck, LucideInbox } from '@lucide/angular';
import { InboxMessage } from './inbox.models';
import { InboxStore } from './inbox.store';

/**
 * The classic notifications "bell": shows the number of unread inbox messages and a dropdown preview of the latest unread ones.
 * Starts tracking the unread count (real time notifications / rare polling) when rendered; the preview is only fetched when the bell is opened.
 */
@Component({
  selector: 'app-inbox-bell',
  imports: [DatePipe, RouterLink, TranslatePipe, LucideBell, LucideCheckCheck, LucideInbox],
  template: `
    <div class="dropdown dropdown-end">
      <button type="button" tabindex="0" class="btn btn-ghost btn-sm btn-square indicator" (focus)="store.loadPreview()"
        [attr.aria-label]="'Inbox.Notifications' | translate" [title]="'Inbox.Notifications' | translate">
        <svg lucideBell class="size-4"></svg>
        @if (store.unreadCount() > 0) {
          <span class="indicator-item badge badge-primary badge-xs tabular px-1" data-testid="inbox-unread-count">
            {{ store.unreadCount() > 99 ? '99+' : store.unreadCount() }}
          </span>
        }
      </button>
      <div tabindex="0" class="dropdown-content z-40 mt-2 w-80 overflow-hidden rounded-box border border-base-300 bg-base-100 shadow-lg sm:w-96">
        <div class="flex items-center justify-between gap-2 border-b border-base-300 px-4 py-2.5">
          <div>
            <p class="text-sm font-semibold">{{ 'Inbox.Notifications' | translate }}</p>
            <p class="text-xs text-base-content/60">
              {{ (store.unreadCount() === 1 ? 'Inbox.UnreadCountOne' : 'Inbox.UnreadCountOther') | translate: { count: store.unreadCount() } }}
            </p>
          </div>
          @if (store.unreadCount() > 0) {
            <button type="button" class="btn btn-ghost btn-xs" (click)="markAllAsRead()">
              <svg lucideCheckCheck class="size-3.5"></svg>{{ 'Inbox.MarkAllAsRead' | translate }}
            </button>
          }
        </div>
        <ul class="max-h-96 divide-y divide-base-300 overflow-y-auto">
          @if (store.previewLoading() && !store.previewLoaded()) {
            <li class="flex justify-center px-4 py-8"><span class="loading loading-spinner loading-sm text-base-content/40"></span></li>
          } @else {
            @for (message of store.latestUnread(); track message.id) {
              <li>
                <button type="button" class="flex w-full items-start gap-3 px-4 py-3 text-left hover:bg-base-200/70" (click)="open(message)">
                  <span class="mt-1.5 size-2 shrink-0 rounded-full bg-primary" aria-hidden="true"></span>
                  <span class="min-w-0 flex-1">
                    <span class="block truncate text-sm font-semibold">{{ message.title || ('Inbox.NoTitle' | translate) }}</span>
                    <span class="mt-0.5 flex items-center gap-2 text-xs text-base-content/60">
                      @if (message.type?.name) { <span class="truncate">{{ message.type?.name }}</span><span aria-hidden="true">·</span> }
                      <span class="whitespace-nowrap">{{ message.createdAt | date: 'short' }}</span>
                    </span>
                  </span>
                </button>
              </li>
            } @empty {
              <li class="flex flex-col items-center gap-2 px-4 py-8 text-center text-sm text-base-content/60">
                <svg lucideInbox class="size-6"></svg>
                {{ 'Inbox.NoUnreadMessages' | translate }}
              </li>
            }
          }
        </ul>
        <div class="border-t border-base-300 p-1">
          <a routerLink="/inbox" class="btn btn-ghost btn-sm btn-block" (click)="blur()">{{ 'Inbox.ViewAll' | translate }}</a>
        </div>
      </div>
    </div>
  `
})
export class InboxBellComponent implements OnInit, OnDestroy {
  private readonly _router = inject(Router);
  protected readonly store = inject(InboxStore);

  public ngOnInit(): void {
    this.store.start();
  }

  public ngOnDestroy(): void {
    this.store.stop();
  }

  protected open(message: InboxMessage): void {
    this.blur();
    this._router.navigate(['/inbox'], { queryParams: { message: message.id } });
  }

  protected markAllAsRead(): void {
    this.store.markAllAsRead().subscribe();
  }

  /** Closes the focus-driven daisyUI dropdown. */
  protected blur(): void {
    (document.activeElement as HTMLElement | null)?.blur();
  }
}
