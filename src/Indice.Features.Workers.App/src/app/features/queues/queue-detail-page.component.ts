import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  input,
  linkedSignal,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import {
  EMPTY,
  catchError,
  combineLatest,
  debounceTime,
  finalize,
  map,
  switchMap,
  tap,
} from 'rxjs';

import { describeError } from '../../core/services/api-error';
import { LiveRefreshService } from '../../core/services/live-refresh.service';
import { ToastService } from '../../core/services/toast.service';
import {
  QueueInfo,
  QueueMessageListItem,
  QueueMessageStatus,
  ResultSet,
  WorkersApiService,
} from '../../core/services/workers-api.service';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog.component';
import { EmptyStateComponent } from '../../shared/empty-state.component';
import { PageHeaderComponent } from '../../shared/page-header.component';
import { PagerComponent } from '../../shared/pager.component';
import { RelativeTimePipe } from '../../shared/relative-time.pipe';
import {
  COUNT_BADGE,
  MESSAGE_STATUSES,
  countOf,
  describeBulkResult,
  plural,
  shortTypeName,
  toStatus,
} from './queue-status';

/** Default page size; the pager offers 20/50/100. */
const PAGE_SIZE = 20;

/**
 * One queue's messages, split by status (Hangfire's "Jobs" pages): a status menu with counts, a
 * paged table with checkbox selection, and bulk Requeue / Delete. The status lives in `?status=`
 * so a link can point straight at a queue's poison messages.
 */
@Component({
  selector: 'app-queue-detail-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    RouterLink,
    ConfirmDialogComponent,
    EmptyStateComponent,
    PageHeaderComponent,
    PagerComponent,
    RelativeTimePipe,
  ],
  template: `
    <section class="space-y-4 p-4 sm:p-6">
      <div class="breadcrumbs text-sm">
        <ul>
          <li><a routerLink="/queues">Queues</a></li>
          <li class="truncate">{{ name() }}</li>
        </ul>
      </div>

      <app-page-header [title]="name()" [subtitle]="subtitle()" />

      @if (error(); as message) {
        <div role="alert" class="alert alert-error alert-soft">{{ message }}</div>
      }

      <!-- Mobile status switcher; the side menu takes over from lg up. -->
      <div class="overflow-x-auto lg:hidden">
        <div role="tablist" class="tabs tabs-box w-max">
          @for (item of statusItems(); track item.status) {
            <a
              role="tab"
              class="tab gap-2 whitespace-nowrap"
              [class.tab-active]="item.status === currentStatus()"
              [attr.aria-selected]="item.status === currentStatus()"
              [routerLink]="[]"
              [queryParams]="{ status: item.status }"
            >
              {{ item.status }}
              <span class="badge badge-xs" [class]="countClass(item)">{{ item.count }}</span>
            </a>
          }
        </div>
      </div>

      <div class="gap-6 lg:grid lg:grid-cols-[13rem_1fr]">
        <ul class="menu hidden w-full self-start rounded-box bg-base-100 shadow-sm lg:flex">
          @for (item of statusItems(); track item.status) {
            <li>
              <a
                [routerLink]="[]"
                [queryParams]="{ status: item.status }"
                [class.menu-active]="item.status === currentStatus()"
                [attr.aria-current]="item.status === currentStatus() ? 'page' : null"
              >
                <span class="flex-1">{{ item.status }}</span>
                <span class="badge badge-sm" [class]="countClass(item)">{{ item.count }}</span>
              </a>
            </li>
          }
        </ul>

        <div class="card min-w-0 bg-base-100 shadow-sm">
          <div class="card-body gap-4 p-4">
            <!-- Toolbar -->
            <div class="flex flex-wrap items-center gap-2">
              <label class="flex items-center px-2" [title]="'Select all on this page'">
                <input
                  type="checkbox"
                  class="checkbox checkbox-sm"
                  aria-label="Select all on this page"
                  [checked]="allSelected()"
                  [indeterminate]="someSelected()"
                  [disabled]="!items().length"
                  (change)="toggleAll()"
                />
              </label>
              <button
                type="button"
                class="btn btn-sm"
                [disabled]="!selected().size || busy()"
                (click)="requeueSelected()"
              >
                {{ requeueLabel() }}
              </button>
              <button
                type="button"
                class="btn btn-sm btn-error btn-outline"
                [disabled]="!selected().size || busy()"
                (click)="deleteDialog.open()"
              >
                Delete selected
              </button>
              @if (selected().size) {
                <span class="text-sm text-base-content/60">{{ selected().size }} selected</span>
              }
              <label class="input input-sm ml-auto w-full sm:w-64">
                <svg viewBox="0 0 24 24" fill="none" class="size-4 opacity-50" aria-hidden="true">
                  <path
                    d="M11 18a7 7 0 1 0 0-14 7 7 0 0 0 0 14zM20 20l-4-4"
                    stroke="currentColor"
                    stroke-width="2"
                    stroke-linecap="round"
                  />
                </svg>
                <input
                  type="search"
                  placeholder="Search id or payload"
                  aria-label="Search id or payload"
                  [value]="search()"
                  (input)="search.set($any($event.target).value)"
                />
              </label>
            </div>

            @if (result(); as rs) {
              @if (rs.items.length) {
                <div class="overflow-x-auto">
                  <table class="table table-sm">
                    <thead>
                      <tr>
                        <th class="w-8"><span class="sr-only">Select</span></th>
                        <th>Id</th>
                        <th>Available</th>
                        <th>Attempts</th>
                        <th>Payload</th>
                      </tr>
                    </thead>
                    <tbody>
                      @for (message of rs.items; track message.id) {
                        <tr class="hover:bg-base-200">
                          <td>
                            <input
                              type="checkbox"
                              class="checkbox checkbox-sm"
                              [attr.aria-label]="'Select message ' + message.id"
                              [checked]="selected().has(message.id)"
                              (change)="toggle(message.id)"
                            />
                          </td>
                          <td>
                            <a
                              [routerLink]="['/messages', message.id]"
                              class="link link-hover font-mono text-xs"
                              [title]="message.id"
                              >{{ message.id.slice(0, 8) }}</a
                            >
                          </td>
                          <td
                            class="whitespace-nowrap"
                            [title]="(message.date | date: 'yyyy-MM-dd HH:mm:ss' : 'UTC') + ' UTC'"
                          >
                            {{ message.date | relativeTime }}
                          </td>
                          <td class="whitespace-nowrap tabular-nums">
                            {{ message.dequeueCount }}
                            @if (maxRetryCount() != null) {
                              / {{ maxRetryCount() }}
                            }
                          </td>
                          <td>
                            <span
                              class="block max-w-md truncate font-mono text-xs text-base-content/70"
                              >{{ message.payloadPreview }}</span
                            >
                          </td>
                        </tr>
                      }
                    </tbody>
                  </table>
                </div>
                <app-pager
                  [page]="page()"
                  [size]="size()"
                  [count]="rs.count"
                  (pageChange)="page.set($event)"
                  (sizeChange)="setSize($event)"
                />
              } @else if (debouncedSearch()) {
                <app-empty-state title="No messages match" [text]="noMatchText()" />
              } @else {
                <app-empty-state [title]="emptyTitle()" [text]="emptyText()" />
              }
            } @else if (!error()) {
              <div class="space-y-2">
                @for (i of skeletonRows; track i) {
                  <div class="skeleton h-8 w-full"></div>
                }
              </div>
            }
          </div>
        </div>
      </div>
    </section>

    <app-confirm-dialog
      #requeueDialog
      [title]="requeueLabel() + '?'"
      [message]="requeueMessage()"
      [confirmLabel]="requeueLabel()"
      confirmClass="btn-primary"
      (confirmed)="requeue()"
    />
    <app-confirm-dialog
      #deleteDialog
      title="Delete messages?"
      [message]="deleteMessage()"
      confirmLabel="Delete"
      (confirmed)="delete()"
    />
  `,
})
export class QueueDetailPageComponent {
  private readonly api = inject(WorkersApiService);
  private readonly live = inject(LiveRefreshService);
  private readonly toasts = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  /** Route param `:name` — the logical queue name. */
  readonly name = input.required<string>();
  /** Query param `?status=`; anything unknown falls back to Enqueued. */
  readonly status = input<string | undefined>();

  protected readonly skeletonRows = [1, 2, 3, 4, 5];

  protected readonly currentStatus = computed(() => toStatus(this.status()));

  protected readonly search = signal('');
  protected readonly debouncedSearch = toSignal(toObservable(this.search).pipe(debounceTime(300)), {
    initialValue: '',
  });
  protected readonly size = signal(PAGE_SIZE);
  /** Back to page 1 whenever the queue, status or search changes — the old page may not exist. */
  protected readonly page = linkedSignal({
    source: () => [this.name(), this.currentStatus(), this.debouncedSearch()].join('\u0000'),
    computation: () => 1,
  });
  /** Cleared when the queue or status changes; a refresh only drops the ids that left the page. */
  protected readonly selected = linkedSignal<string, ReadonlySet<string>>({
    source: () => `${this.name()}\u0000${this.currentStatus()}`,
    computation: () => new Set<string>(),
  });

  private readonly query = computed(() => ({
    name: this.name(),
    status: this.currentStatus(),
    page: this.page(),
    size: this.size(),
    search: this.debouncedSearch().trim(),
  }));

  /** `null` while a new query loads, so the template shows skeletons, not another tab's rows. */
  protected readonly result = signal<ResultSet<QueueMessageListItem> | null>(null);
  private readonly queue = signal<QueueInfo | null>(null);
  private readonly messagesError = signal<string | null>(null);
  private readonly queueError = signal<string | null>(null);
  protected readonly error = computed(() => this.messagesError() ?? this.queueError());
  protected readonly busy = signal(false);

  protected readonly items = computed(() => this.result()?.items ?? []);
  protected readonly allSelected = computed(() => {
    const items = this.items();
    return items.length > 0 && items.every((m) => this.selected().has(m.id));
  });
  protected readonly someSelected = computed(() => this.selected().size > 0 && !this.allSelected());

  /** Requeueing a delayed message makes it visible now, so Hangfire's wording fits better there. */
  protected readonly requeueLabel = computed(() =>
    this.currentStatus() === 'Scheduled' ? 'Enqueue now' : 'Requeue selected',
  );

  protected readonly maxRetryCount = computed(() => this.queue()?.maxRetryCount ?? null);

  /** Dequeued only exists in the obsolete EF store, so it is listed only when it has messages. */
  protected readonly statusItems = computed(() => {
    const queue = this.queue();
    const current = this.currentStatus();
    return MESSAGE_STATUSES.map((status) => ({ status, count: countOf(queue, status) })).filter(
      (item) => item.status !== 'Dequeued' || item.count > 0 || current === 'Dequeued',
    );
  });

  /** Only the known parts — handler and polling settings live in the worker process. */
  protected readonly subtitle = computed(() => {
    const queue = this.queue();
    if (!queue) {
      return null;
    }
    const parts = [
      shortTypeName(queue.handlerType),
      queue.maxRetryCount != null ? `max ${queue.maxRetryCount} retries` : null,
      queue.instanceCount != null ? plural(queue.instanceCount, 'instance') : null,
    ].filter((part): part is string => !!part);
    return parts.length ? parts.join(' · ') : `${plural(queue.total, 'message')} waiting`;
  });

  protected readonly emptyTitle = computed(
    () => `No ${this.currentStatus().toLowerCase()} messages`,
  );
  protected readonly emptyText = computed(() => EMPTY_TEXT[this.currentStatus()]);
  protected readonly noMatchText = computed(
    () => `Nothing in ${this.currentStatus()} matches “${this.debouncedSearch()}”.`,
  );

  protected readonly requeueMessage = computed(
    () =>
      `${plural(this.selected().size, 'message')} will be visible in ${this.name()} again, ` +
      'with their attempts reset.',
  );
  protected readonly deleteMessage = computed(
    () => `Permanently delete ${plural(this.selected().size, 'message')}. They won't be processed.`,
  );

  private readonly requeueDialog = viewChild.required<ConfirmDialogComponent>('requeueDialog');

  constructor() {
    combineLatest([
      toObservable(this.query).pipe(tap(() => this.result.set(null))),
      this.live.tick$,
    ])
      .pipe(
        switchMap(([q]) =>
          this.api
            .getQueueMessages(q.name, {
              status: q.status,
              page: q.page,
              size: q.size,
              search: q.search || undefined,
            })
            .pipe(
              catchError((e: unknown) => {
                this.messagesError.set(describeError(e));
                return EMPTY;
              }),
            ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((rs) => {
        this.result.set(rs);
        const present = new Set(rs.items.map((m) => m.id));
        this.selected.update((ids) => new Set([...ids].filter((id) => present.has(id))));
        this.messagesError.set(null);
        this.live.markUpdated();
      });

    // The status counts and the queue's settings come from the queue list. A queue with no waiting
    // messages isn't in it at all, which reads as zero everywhere.
    combineLatest([toObservable(this.name), this.live.tick$])
      .pipe(
        switchMap(([name]) =>
          this.api.getQueues().pipe(
            map((queues) => queues.find((q) => q.name === name) ?? null),
            catchError((e: unknown) => {
              this.queueError.set(describeError(e));
              return EMPTY;
            }),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((queue) => {
        this.queue.set(queue);
        this.queueError.set(null);
      });
  }

  protected countClass(item: { status: QueueMessageStatus; count: number }): string {
    return item.count ? COUNT_BADGE[item.status] : 'badge-ghost';
  }

  protected toggle(id: string): void {
    this.selected.update((ids) => {
      const next = new Set(ids);
      if (!next.delete(id)) {
        next.add(id);
      }
      return next;
    });
  }

  protected toggleAll(): void {
    this.selected.set(this.allSelected() ? new Set() : new Set(this.items().map((m) => m.id)));
  }

  protected setSize(size: number): void {
    this.size.set(size);
    this.page.set(1);
  }

  /** A single message is requeued straight away; a batch asks first. */
  protected requeueSelected(): void {
    if (this.selected().size > 1) {
      this.requeueDialog().open();
    } else {
      this.requeue();
    }
  }

  protected requeue(): void {
    const verb = this.currentStatus() === 'Scheduled' ? 'enqueued' : 'requeued';
    this.run([...this.selected()], (ids) => this.api.requeueMessages(ids), verb);
  }

  protected delete(): void {
    this.run([...this.selected()], (ids) => this.api.deleteMessages(ids), 'deleted');
  }

  private run(
    ids: string[],
    action: (ids: string[]) => ReturnType<WorkersApiService['deleteMessages']>,
    verb: 'requeued' | 'enqueued' | 'deleted',
  ): void {
    if (!ids.length) {
      return;
    }
    this.busy.set(true);
    action(ids)
      .pipe(
        finalize(() => this.busy.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: ({ affected }) => {
          this.toasts.show('success', describeBulkResult(verb, affected, ids.length));
          this.selected.set(new Set());
          this.live.refreshNow();
        },
        error: (e: unknown) => this.toasts.show('error', describeError(e)),
      });
  }
}

/** Why a status tab can be empty — mostly reassurance that nothing was lost. */
const EMPTY_TEXT: Record<QueueMessageStatus, string> = {
  Enqueued: 'Processed messages are removed from the store, so only waiting messages appear here.',
  Scheduled: 'No message in this queue is delayed.',
  Retrying: 'No message in this queue has failed and is waiting for another attempt.',
  Poison: 'No message in this queue has run out of retries.',
  Dequeued: 'No processed messages are waiting to be cleaned up.',
};
