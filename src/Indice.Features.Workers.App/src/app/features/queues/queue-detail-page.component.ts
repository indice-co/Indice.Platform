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
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { DatePipe, DecimalPipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { LucideChevronRight, LucidePlay, LucidePlus, LucideRotateCcw, LucideTrash2 } from '@lucide/angular';
import {
  EMPTY,
  catchError,
  combineLatest,
  finalize,
  switchMap,
  tap,
} from 'rxjs';

import { describeError } from '../../core/services/api-error';
import { LiveRefreshService } from '../../core/services/live-refresh.service';
import { PermissionsStore } from '../../core/services/permissions.store';
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
import { PayloadDialogComponent, PayloadDialogResult } from '../../shared/payload-dialog.component';
import { RelativeTimePipe } from '../../shared/relative-time.pipe';
import { SearchInputComponent } from '../../shared/search-input.component';
import { QueueMixComponent } from './queue-mix.component';
import {
  LIFECYCLE,
  STATUS_TONE,
  countOf,
  describeBulkResult,
  formatPolling,
  plural,
  shortTypeName,
  toStatus,
} from './queue-status';
import { QueueStatusIconComponent } from './queue-status-icon.component';

/** Default page size; the pager offers 20/50/100. */
const PAGE_SIZE = 20;

/** How each status reads and paints on this page. Literal class names, so Tailwind's scanner generates them. */
interface Stage {
  /** Under the count on the status card. */
  hint: string;
  /** Next to the list title. */
  detail: string;
  /** What a message's visible-at means in this status. */
  dateLabel: string;
  /** The selected status card. */
  selected: string;
  /** The attempts meter. */
  progress: string;
}

const STAGES: Record<QueueMessageStatus, Stage> = {
  Scheduled: {
    hint: 'Waiting for their time',
    detail: 'Delayed; each one becomes enqueued at its time.',
    dateLabel: 'Runs',
    selected: 'border-secondary bg-secondary/5',
    progress: 'progress-secondary',
  },
  Enqueued: {
    hint: 'Ready for a worker',
    detail: 'Visible now; the next free worker picks them up.',
    dateLabel: 'Ready since',
    selected: 'border-info bg-info/5',
    progress: 'progress-info',
  },
  Retrying: {
    hint: 'Failed, trying again',
    detail: 'Failed at least once and will be tried again.',
    dateLabel: 'Next attempt',
    selected: 'border-warning bg-warning/5',
    progress: 'progress-warning',
  },
  Poison: {
    hint: 'Out of retries',
    detail: 'Failed every attempt. Requeue them once the cause is fixed, or delete them.',
    dateLabel: 'Since',
    selected: 'border-error bg-error/5',
    progress: 'progress-error',
  },
  Dequeued: {
    hint: 'Awaiting cleanup',
    detail: 'Processed and waiting to be cleaned up.',
    dateLabel: 'Dequeued',
    selected: 'border-base-content/40 bg-base-200/50',
    progress: '',
  },
};

interface Fact {
  label: string;
  value: string;
  title?: string | null;
  mono?: boolean;
}

/**
 * One queue's messages, split by status (Hangfire's "Jobs" pages): the message lifecycle as status cards with
 * counts, the queue's settings, and a paged table with checkbox selection and bulk Requeue / Delete. The status
 * lives in `?status=` so a link can point straight at a queue's poison messages.
 */
@Component({
  selector: 'app-queue-detail-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    DecimalPipe,
    RouterLink,
    LucideChevronRight,
    LucidePlay,
    LucidePlus,
    LucideRotateCcw,
    LucideTrash2,
    ConfirmDialogComponent,
    EmptyStateComponent,
    PageHeaderComponent,
    PagerComponent,
    PayloadDialogComponent,
    QueueMixComponent,
    QueueStatusIconComponent,
    RelativeTimePipe,
    SearchInputComponent,
  ],
  template: `
    <section class="space-y-4 p-4 sm:p-6">
      <div class="breadcrumbs text-sm">
        <ul>
          <li><a routerLink="/queues">Queues</a></li>
          <li class="truncate">{{ name() }}</li>
        </ul>
      </div>

      <app-page-header [title]="name()" [subtitle]="subtitle()">
        @if (canManage()) {
          <button type="button" class="btn btn-sm btn-primary gap-1.5" (click)="payloadDialog.open()">
            <svg lucidePlus class="size-4"></svg>Add message
          </button>
        }
      </app-page-header>

      @if (error(); as message) {
        <div role="alert" class="alert alert-error alert-soft">{{ message }}</div>
      }

      <!-- Overview: the lifecycle as status cards that switch the list, the mix of the queue, and its settings. -->
      <div class="card bg-base-100 shadow-sm">
        <div class="card-body gap-4 p-4">
          <nav aria-label="Message status" class="grid grid-cols-2 gap-3 lg:flex lg:items-stretch lg:gap-2">
            @for (item of statusItems(); track item.status; let last = $last) {
              <a
                [routerLink]="[]"
                [queryParams]="{ status: item.status }"
                [attr.aria-current]="item.active ? 'page' : null"
                class="flex min-w-0 flex-col gap-2 rounded-box border p-3 transition-colors lg:flex-1"
                [class]="
                  item.active
                    ? item.stage.selected
                    : 'border-base-300 hover:border-base-content/25 hover:bg-base-200/40'
                "
              >
                <span class="flex items-center justify-between gap-2">
                  <span class="text-sm font-medium text-base-content/85">{{ item.status }}</span>
                  <span class="grid size-7 place-items-center rounded-field" [class]="item.tone.chip">
                    <app-queue-status-icon class="size-4" [status]="item.status" />
                  </span>
                </span>
                @if (queue()) {
                  <span
                    class="text-2xl font-semibold tabular-nums"
                    [class]="item.count ? item.tone.text : 'text-base-content/45'"
                    >{{ item.count | number }}</span
                  >
                } @else {
                  <span class="skeleton h-8 w-12"></span>
                }
                <span class="truncate text-xs text-base-content/70">
                  @if (item.status === 'Enqueued' && item.count && oldestReady()) {
                    Oldest ready {{ oldestReady() | relativeTime }}
                  } @else {
                    {{ item.stage.hint }}
                  }
                </span>
              </a>
              @if (!last) {
                <svg
                  lucideChevronRight
                  class="hidden size-4 shrink-0 self-center text-base-content/25 lg:block"
                  aria-hidden="true"
                ></svg>
              }
            }
          </nav>

          <app-queue-mix [queue]="queue()" />

          @if (facts().length) {
            <dl
              class="grid grid-cols-2 gap-x-6 gap-y-3 border-t border-base-200 pt-4 sm:grid-cols-3
                     lg:flex lg:flex-wrap lg:gap-x-10"
            >
              @for (fact of facts(); track fact.label) {
                <div class="min-w-0">
                  <dt class="text-xs text-base-content/70">{{ fact.label }}</dt>
                  <dd class="truncate text-sm" [class.font-mono]="fact.mono" [title]="fact.title ?? fact.value">
                    {{ fact.value }}
                  </dd>
                </div>
              }
            </dl>
          }
        </div>
      </div>

      <div class="card min-w-0 bg-base-100 shadow-sm">
        <div class="card-body gap-4 p-4">
          <div class="flex flex-wrap items-baseline gap-x-3 gap-y-1">
            <h2 class="card-title text-base">{{ currentStatus() }} messages</h2>
            <p class="text-sm text-base-content/70">{{ stage().detail }}</p>
          </div>

          <!-- Toolbar -->
          <div class="flex flex-wrap items-center gap-2">
            @if (canManage()) {
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
                class="btn btn-sm btn-soft btn-primary gap-1.5"
                [disabled]="!selected().size || busy()"
                (click)="requeueSelected()"
              >
                @if (currentStatus() === 'Scheduled') {
                  <svg lucidePlay class="size-4"></svg>
                } @else {
                  <svg lucideRotateCcw class="size-4"></svg>
                }
                {{ requeueLabel() }}
              </button>
              <button
                type="button"
                class="btn btn-sm btn-soft btn-error gap-1.5"
                [disabled]="!selected().size || busy()"
                (click)="deleteDialog.open()"
              >
                <svg lucideTrash2 class="size-4"></svg>
                Delete selected
              </button>
              @if (selected().size) {
                <span class="text-sm text-base-content/70">{{ selected().size }} selected</span>
              }
            }
            <app-search-input
              class="ml-auto w-full sm:w-64"
              placeholder="Search id or payload"
              [value]="search()"
              (search)="search.set($event)"
            />
          </div>

          @if (result(); as rs) {
            @if (rs.items.length) {
              <div class="overflow-x-auto">
                <table class="table table-sm">
                  <thead>
                    <tr>
                      @if (canManage()) {
                        <th class="w-8"><span class="sr-only">Select</span></th>
                      }
                      <th>Message</th>
                      <th>{{ stage().dateLabel }}</th>
                      <th>Attempts</th>
                    </tr>
                  </thead>
                  <tbody>
                    @for (message of rs.items; track message.id) {
                      <tr class="hover:bg-base-200">
                        @if (canManage()) {
                          <td>
                            <input
                              type="checkbox"
                              class="checkbox checkbox-sm"
                              [attr.aria-label]="'Select message ' + message.id"
                              [checked]="selected().has(message.id)"
                              (change)="toggle(message.id)"
                            />
                          </td>
                        }
                        <!-- max-w-0 + w-full: the payload takes the spare width and truncates instead of widening the table. -->
                        <td class="w-full max-w-0 min-w-48">
                          <a
                            [routerLink]="['/messages', message.id]"
                            class="link link-hover block truncate font-mono text-xs"
                            [title]="message.payloadPreview ?? ''"
                            >{{ message.payloadPreview || '(empty payload)' }}</a
                          >
                          <span class="font-mono text-[0.7rem] text-base-content/70" [title]="message.id">{{
                            message.id.slice(0, 8)
                          }}</span>
                        </td>
                        <td
                          class="whitespace-nowrap"
                          [title]="(message.date | date: 'yyyy-MM-dd HH:mm:ss' : 'UTC') + ' UTC'"
                        >
                          {{ message.date | relativeTime }}
                          <span class="block text-xs text-base-content/70">{{
                            message.date | date: 'MMM d, HH:mm'
                          }}</span>
                        </td>
                        <td class="whitespace-nowrap tabular-nums">
                          <span [class.text-base-content/70]="!message.dequeueCount">
                            {{ message.dequeueCount }}
                            @if (maxRetryCount() != null) {
                              / {{ maxRetryCount() }}
                            }
                          </span>
                          @if (maxRetryCount() && message.dequeueCount) {
                            <progress
                              class="progress mt-1 block h-1 w-16"
                              [class]="stage().progress"
                              [value]="message.dequeueCount"
                              [max]="maxRetryCount()"
                            ></progress>
                          }
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
            } @else if (search()) {
              <app-empty-state title="No messages match" [text]="noMatchText()" />
            } @else {
              <app-empty-state [title]="emptyTitle()" [text]="emptyText()" />
            }
          } @else if (!error()) {
            <div class="space-y-2">
              @for (i of skeletonRows; track i) {
                <div class="skeleton h-10 w-full"></div>
              }
            </div>
          }
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
    <app-payload-dialog
      #payloadDialog
      [title]="'Add a message to ' + name()"
      description="The worker of the queue processes it like any other message."
      hint="It must deserialize into the work item of the queue, or it ends up as poison."
      confirmLabel="Add message"
      confirmIcon="add"
      [askVisibleAt]="true"
      [busy]="busy()"
      (submitted)="enqueue($event)"
    />
  `,
})
export class QueueDetailPageComponent {
  private readonly api = inject(WorkersApiService);
  private readonly live = inject(LiveRefreshService);
  private readonly toasts = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  protected readonly canManage = inject(PermissionsStore).canManage;

  /** Route param `:name` — the logical queue name. */
  readonly name = input.required<string>();
  /** Query param `?status=`; anything unknown falls back to Enqueued. */
  readonly status = input<string | undefined>();

  protected readonly skeletonRows = [1, 2, 3, 4, 5];

  protected readonly currentStatus = computed(() => toStatus(this.status()));
  protected readonly stage = computed(() => STAGES[this.currentStatus()]);

  /** Applied search term; the search box debounces it. */
  protected readonly search = signal('');
  protected readonly size = signal(PAGE_SIZE);
  /** Back to page 1 whenever the queue, status or search changes — the old page may not exist. */
  protected readonly page = linkedSignal({
    source: () => [this.name(), this.currentStatus(), this.search()].join('\u0000'),
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
    search: this.search().trim(),
  }));

  /** `null` while a new query loads, so the template shows skeletons, not another status's rows. */
  protected readonly result = signal<ResultSet<QueueMessageListItem> | null>(null);
  protected readonly queue = signal<QueueInfo | null>(null);
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
  protected readonly oldestReady = computed(() => this.queue()?.oldestEnqueuedAt ?? null);

  /** Dequeued only exists in the obsolete EF store, so it is listed only when it has messages. */
  protected readonly statusItems = computed(() => {
    const queue = this.queue();
    const current = this.currentStatus();
    return LIFECYCLE.map((status) => ({
      status,
      count: countOf(queue, status),
      stage: STAGES[status],
      tone: STATUS_TONE[status],
      active: status === current,
    })).filter((item) => item.status !== 'Dequeued' || item.count > 0 || item.active);
  });

  /** Only the known parts — handler and polling settings live in the worker process. */
  protected readonly facts = computed<Fact[]>(() => {
    const queue = this.queue();
    if (!queue) {
      return [];
    }
    const facts: (Fact | null)[] = [
      queue.handlerType
        ? { label: 'Handler', value: shortTypeName(queue.handlerType)!, title: queue.handlerType, mono: true }
        : null,
      queue.workItemType
        ? { label: 'Work item', value: shortTypeName(queue.workItemType)!, title: queue.workItemType, mono: true }
        : null,
      queue.maxRetryCount != null ? { label: 'Max retries', value: `${queue.maxRetryCount}` } : null,
      queue.instanceCount != null ? { label: 'Workers', value: plural(queue.instanceCount, 'instance') } : null,
      queue.pollingInterval != null
        ? { label: 'Polls every', value: formatPolling(queue.pollingInterval) }
        : null,
    ];
    return facts.filter((fact): fact is Fact => !!fact);
  });

  protected readonly subtitle = computed(() => {
    const queue = this.queue();
    return queue ? `${plural(queue.total, 'message')} in the store` : null;
  });

  protected readonly emptyTitle = computed(
    () => `No ${this.currentStatus().toLowerCase()} messages`,
  );
  protected readonly emptyText = computed(() => EMPTY_TEXT[this.currentStatus()]);
  protected readonly noMatchText = computed(
    () => `Nothing in ${this.currentStatus()} matches “${this.search()}”.`,
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
  private readonly payloadDialog = viewChild.required<PayloadDialogComponent>('payloadDialog');

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
        if (!rs.items.length && this.page() > 1) {
          const lastPage = Math.max(1, Math.ceil(rs.count / this.size()));
          this.page.set(Math.min(this.page() - 1, lastPage));
          return;
        }
        this.result.set(rs);
        const present = new Set(rs.items.map((m) => m.id));
        this.selected.update((ids) => new Set([...ids].filter((id) => present.has(id))));
        this.messagesError.set(null);
        this.live.markUpdated();
      });

    // The status counts of this queue only — an index seek, not the whole queue table.
    combineLatest([toObservable(this.name), this.live.tick$])
      .pipe(
        switchMap(([name]) =>
          this.api.getQueue(name).pipe(
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

  protected enqueue(result: PayloadDialogResult): void {
    this.busy.set(true);
    this.api
      .enqueueMessage(this.name(), { payload: result.payload, visibleAt: result.visibleAt })
      .pipe(
        finalize(() => this.busy.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (message) => {
          this.payloadDialog().close();
          this.toasts.show('success', `Message added to ${this.name()}.`);
          this.router.navigate([], {
            relativeTo: this.route,
            queryParams: { status: message.status },
            queryParamsHandling: 'merge',
          });
          this.live.refreshNow();
        },
        error: (e: unknown) => this.toasts.show('error', describeError(e)),
      });
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

/** Why a status can be empty — mostly reassurance that nothing was lost. */
const EMPTY_TEXT: Record<QueueMessageStatus, string> = {
  Enqueued: 'Processed messages are removed from the store, so only waiting messages appear here.',
  Scheduled: 'No message in this queue is delayed.',
  Retrying: 'No message in this queue has failed and is waiting for another attempt.',
  Poison: 'No message in this queue has run out of retries.',
  Dequeued: 'No processed messages are waiting to be cleaned up.',
};
