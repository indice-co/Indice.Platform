import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { DatePipe } from '@angular/common';

import { ConversationListItem } from '../../core/services/dex-api.service';
import { PoweredByComponent } from '../../core/layout/powered-by.component';

/** Conversation rail: new-chat action, search, and the caller's session list. */
@Component({
  selector: 'app-chat-sidebar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, PoweredByComponent],
  template: `
    <aside class="flex h-full w-full flex-col bg-base-100">
      <div class="px-3 pt-3">
        <button
          type="button"
          class="btn btn-primary btn-block justify-start gap-2 shadow-sm"
          (click)="create.emit()"
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
      </div>

      <div class="px-3 py-3">
        <label class="flex items-center gap-2 rounded-field border border-base-300 bg-base-200 px-3">
          <svg viewBox="0 0 24 24" fill="none" class="size-4 text-base-content/40" aria-hidden="true">
            <circle cx="11" cy="11" r="7" stroke="currentColor" stroke-width="2" />
            <path d="m20 20-3-3" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
          </svg>
          <input
            type="search"
            class="w-full bg-transparent py-2 text-sm outline-none placeholder:text-base-content/40"
            placeholder="Search conversations"
            [value]="search()"
            (input)="onSearch($event)"
          />
        </label>
      </div>

      <nav class="dex-scroll flex-1 overflow-y-auto px-2 pb-3">
        @if (loading()) {
          <div class="space-y-2 px-1 pt-2">
            @for (i of [1, 2, 3, 4]; track i) {
              <div class="h-12 animate-pulse rounded-field bg-base-200"></div>
            }
          </div>
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
                    <p class="truncate text-sm font-medium text-base-content">
                      {{ s.title || 'Untitled conversation' }}
                    </p>
                    <p class="mt-0.5 truncate font-mono text-[0.68rem] text-base-content/45">
                      {{ s.lastActivityAt | date: 'MMM d · HH:mm' }}
                    </p>
                  </div>
                  <button
                    type="button"
                    class="btn btn-ghost btn-xs btn-circle text-base-content/40 opacity-0
                           transition group-hover:opacity-100 hover:text-error"
                    (click)="onRemove($event, s)"
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

      <div class="shrink-0 border-t border-base-300 px-3 py-2">
        <app-powered-by />
      </div>
    </aside>
  `,
})
export class ChatSidebarComponent {
  readonly sessions = input<ConversationListItem[]>([]);
  readonly activeId = input<string | null>(null);
  readonly loading = input(false);

  readonly select = output<string>();
  readonly removed = output<string>();
  readonly create = output<void>();

  protected readonly search = signal('');
  protected readonly filtered = computed(() => {
    const query = this.search().trim().toLowerCase();
    const list = this.sessions();
    if (!query) {
      return list;
    }
    return list.filter((s) => (s.title ?? '').toLowerCase().includes(query));
  });

  protected onSearch(event: Event): void {
    this.search.set((event.target as HTMLInputElement).value);
  }

  protected choose(item: ConversationListItem): void {
    if (item.id) {
      this.select.emit(item.id);
    }
  }

  protected onRemove(event: MouseEvent, item: ConversationListItem): void {
    event.stopPropagation();
    if (item.id) {
      this.removed.emit(item.id);
    }
  }
}
