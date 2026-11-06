import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { catchError, distinctUntilChanged, filter, map, of, switchMap, tap } from 'rxjs';
import {
  LucideArrowLeft, LucideCheckCheck, LucideExternalLink, LucideInbox, LucideMail, LucideMailOpen, LucidePaperclip, LucideRefreshCw, LucideTrash
} from '@lucide/angular';
import { AppTranslatedToaster, ToastType } from 'src/app/shared/services/app-translated-toaster';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { FilterClause } from 'src/app/ui/list/filter-clause';
import { ListCountComponent } from 'src/app/ui/list/list-count.component';
import { ListQuery, ListState } from 'src/app/ui/list/list-state';
import { PaginationComponent } from 'src/app/ui/list/pagination.component';
import { SearchInputComponent } from 'src/app/ui/list/search-input.component';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';
import { InboxApiService } from './inbox-api.service';
import { InboxMessage, InboxMessagesQuery } from './inbox.models';
import { InboxStore } from './inbox.store';

type InboxStatusFilter = '' | 'unread' | 'read';

const STATUS_FILTER = 'status';
const TYPE_FILTER = 'type';
const EXPIRED_FILTER = 'expired';

/**
 * Personal inbox of the current user: every message received (new and past) through the inbox channel, with filters and a
 * reading pane. Built only on the user present `my/messages` api.
 */
@Component({
  selector: 'app-inbox',
  imports: [
    DatePipe, TranslatePipe, PageHeaderComponent, EmptyStateComponent, PaginationComponent, SearchInputComponent, ListCountComponent,
    LucideArrowLeft, LucideCheckCheck, LucideExternalLink, LucideInbox, LucideMail, LucideMailOpen, LucidePaperclip, LucideRefreshCw, LucideTrash
  ],
  template: `
    <app-page-header [title]="'Inbox.Title' | translate" [subtitle]="'Inbox.Subtitle' | translate">
      <app-list-count class="mt-0.5" [count]="list.items() ? list.count() : null" one="Inbox.CountOne" other="Inbox.CountOther" />
      <div actions>
        <button type="button" class="btn btn-ghost btn-sm" (click)="refresh()" [disabled]="list.loading()">
          <svg lucideRefreshCw class="size-4" [class.animate-spin]="list.loading()"></svg>{{ 'Inbox.Refresh' | translate }}
        </button>
        <button type="button" class="btn btn-sm" (click)="markAllAsRead()" [disabled]="!store.hasUnread()">
          <svg lucideCheckCheck class="size-4"></svg>{{ 'Inbox.MarkAllAsRead' | translate }}
        </button>
      </div>
    </app-page-header>

    <div class="mb-3 flex flex-col gap-2 lg:flex-row lg:items-center">
      <app-search-input class="lg:w-72" [value]="list.search()" [placeholder]="'Inbox.Search' | translate" (search)="list.setSearch($event)" />
      <div class="join" role="group" [attr.aria-label]="'Inbox.Status' | translate">
        @for (option of statusOptions; track option.value) {
          <button type="button" class="btn btn-sm join-item" [class.btn-active]="status() === option.value" (click)="setStatus(option.value)">
            {{ option.label | translate }}
          </button>
        }
      </div>
      <select class="select select-sm lg:w-56" [value]="typeId()" (change)="setType($any($event.target).value)" [attr.aria-label]="'Inbox.Type' | translate">
        <option value="">{{ 'Inbox.AllTypes' | translate }}</option>
        @for (type of messageTypes(); track type.id) {
          <option [value]="type.id">{{ type.name }}</option>
        }
      </select>
      <label class="label cursor-pointer gap-2 text-sm">
        <input type="checkbox" class="toggle toggle-sm" [checked]="showExpired()" (change)="setShowExpired($any($event.target).checked)" />
        {{ 'Inbox.ShowExpired' | translate }}
      </label>
    </div>

    <div class="grid gap-4 lg:grid-cols-[minmax(0,2fr)_minmax(0,3fr)]">
      <div class="panel overflow-hidden lg:block" [class.hidden]="selectedId()">
        <ul class="divide-y divide-base-300">
          @if (list.loading() && !list.items()?.length) {
            @for (row of [1, 2, 3, 4]; track row) {
              <li class="px-4 py-3"><div class="skeleton h-10 w-full"></div></li>
            }
          } @else {
            @for (message of list.items(); track message.id) {
              <li>
                <button type="button" class="flex w-full items-start gap-3 px-4 py-3 text-left hover:bg-base-200/60"
                  [class.bg-base-200]="message.id === selectedId()" [class.opacity-60]="list.loading()" (click)="select(message)">
                  <span class="mt-1.5 size-2 shrink-0 rounded-full" [class.bg-primary]="!message.isRead" aria-hidden="true"></span>
                  <span class="min-w-0 flex-1">
                    <span class="flex items-baseline justify-between gap-2">
                      <span class="truncate text-sm" [class.font-semibold]="!message.isRead">{{ message.title || ('Inbox.NoTitle' | translate) }}</span>
                      <span class="shrink-0 whitespace-nowrap text-xs text-base-content/60">{{ message.createdAt | date: 'short' }}</span>
                    </span>
                    <span class="mt-0.5 flex items-center gap-2 text-xs text-base-content/60">
                      @if (message.type?.name) { <span class="badge badge-ghost badge-xs">{{ message.type?.name }}</span> }
                      @if (!message.isRead) { <span class="sr-only">{{ 'Inbox.Unread' | translate }}</span> }
                      @if (message.attachmentUrl) { <svg lucidePaperclip class="size-3"></svg> }
                    </span>
                  </span>
                </button>
              </li>
            } @empty {
              <li>
                <app-empty-state [title]="'Inbox.EmptyTitle' | translate" [message]="'Inbox.EmptyMessage' | translate">
                  <svg icon lucideInbox class="size-5"></svg>
                </app-empty-state>
              </li>
            }
          }
        </ul>
        @if (list.count() > 0) {
          <app-pagination class="border-t border-base-300 px-4 py-3" [page]="list.page()" [pageSize]="list.pageSize()" [count]="list.count()"
            [pageSizes]="[10, 20, 50]" (pageChange)="list.setPage($event)" (pageSizeChange)="list.setPageSize($event)" />
        }
      </div>

      <div class="panel min-h-64 lg:block" [class.hidden]="!selectedId()">
        @let message = selected();
        @if (message) {
          <article class="flex h-full flex-col">
            <header class="flex flex-wrap items-start justify-between gap-3 border-b border-base-300 px-5 py-4">
              <div class="min-w-0">
                <button type="button" class="btn btn-ghost btn-xs mb-2 lg:hidden" (click)="select(null)">
                  <svg lucideArrowLeft class="size-3.5"></svg>{{ 'Inbox.BackToList' | translate }}
                </button>
                <h2 class="text-lg font-semibold text-balance">{{ message.title || ('Inbox.NoTitle' | translate) }}</h2>
                <p class="mt-1 flex flex-wrap items-center gap-2 text-xs text-base-content/60">
                  @if (message.sender) { <span>{{ message.sender }}</span><span aria-hidden="true">·</span> }
                  <span>{{ message.createdAt | date: 'medium' }}</span>
                  @if (message.type?.name) { <span class="badge badge-ghost badge-xs">{{ message.type?.name }}</span> }
                </p>
              </div>
              <div class="flex items-center gap-1">
                @if (message.isRead) {
                  <span class="tooltip tooltip-left" [attr.data-tip]="'Inbox.MarkAsUnread' | translate">
                    <button type="button" class="btn btn-ghost btn-sm btn-square" (click)="markAsUnread(message)" [attr.aria-label]="'Inbox.MarkAsUnread' | translate">
                      <svg lucideMail class="size-4"></svg>
                    </button>
                  </span>
                } @else {
                  <span class="tooltip tooltip-left" [attr.data-tip]="'Inbox.MarkAsRead' | translate">
                    <button type="button" class="btn btn-ghost btn-sm btn-square" (click)="markAsRead(message)" [attr.aria-label]="'Inbox.MarkAsRead' | translate">
                      <svg lucideMailOpen class="size-4"></svg>
                    </button>
                  </span>
                }
                <span class="tooltip tooltip-left" [attr.data-tip]="'Inbox.Delete' | translate">
                  <button type="button" class="btn btn-ghost btn-sm btn-square text-error" (click)="remove(message)" [attr.aria-label]="'Inbox.Delete' | translate">
                    <svg lucideTrash class="size-4"></svg>
                  </button>
                </span>
              </div>
            </header>
            <div class="prose prose-sm max-w-none flex-1 px-5 py-4" [innerHTML]="message.content || ''"></div>
            @if (actionLink() || attachmentUrl()) {
              <footer class="flex flex-wrap items-center gap-2 border-t border-base-300 px-5 py-3">
                @if (actionLink(); as href) {
                  <a class="btn btn-primary btn-sm" [href]="href" target="_blank" rel="noopener noreferrer">
                    <svg lucideExternalLink class="size-4"></svg>{{ message.actionLink?.text || ('Inbox.OpenLink' | translate) }}
                  </a>
                }
                @if (attachmentUrl(); as href) {
                  <a class="btn btn-sm" [href]="href" target="_blank" rel="noopener noreferrer">
                    <svg lucidePaperclip class="size-4"></svg>{{ 'Inbox.Attachment' | translate }}
                  </a>
                }
              </footer>
            }
          </article>
        } @else if (detailLoading()) {
          <div class="space-y-3 p-5">
            <div class="skeleton h-6 w-2/3"></div>
            <div class="skeleton h-4 w-1/3"></div>
            <div class="skeleton h-32 w-full"></div>
          </div>
        } @else {
          <app-empty-state [title]="'Inbox.SelectMessageTitle' | translate" [message]="'Inbox.SelectMessageMessage' | translate">
            <svg icon lucideMailOpen class="size-5"></svg>
          </app-empty-state>
        }
      </div>
    </div>
  `
})
export class InboxComponent implements OnInit {
  private readonly _api = inject(InboxApiService);
  private readonly _route = inject(ActivatedRoute);
  private readonly _router = inject(Router);
  private readonly _dialog = inject(DialogService);
  private readonly _toaster = inject(AppTranslatedToaster);
  protected readonly store = inject(InboxStore);

  protected readonly statusOptions: { value: InboxStatusFilter; label: string }[] = [
    { value: '', label: 'Inbox.All' },
    { value: 'unread', label: 'Inbox.Unread' },
    { value: 'read', label: 'Inbox.Read' }
  ];

  protected readonly list = new ListState<InboxMessage>({
    pageSize: 20,
    sort: 'createdAt',
    dir: 'desc',
    load: query => this._api.getMessages(this.toApiQuery(query))
  });

  protected readonly status = computed<InboxStatusFilter>(() => (this.filterValue(STATUS_FILTER) as InboxStatusFilter) || '');
  protected readonly typeId = computed(() => this.filterValue(TYPE_FILTER) ?? '');
  protected readonly showExpired = computed(() => this.filterValue(EXPIRED_FILTER) === 'true');
  protected readonly messageTypes = toSignal(this._api.getMessageTypes().pipe(catchError(() => of([]))), { initialValue: [] });

  protected readonly selectedId = toSignal(this._route.queryParamMap.pipe(map(params => params.get('message'))), { initialValue: null });
  protected readonly selected = signal<InboxMessage | null>(null);
  protected readonly detailLoading = signal(false);
  protected readonly actionLink = computed(() => { const m = this.selected(); return m ? this._api.resolveActionLink(m) : undefined; });
  protected readonly attachmentUrl = computed(() => { const m = this.selected(); return m ? this._api.resolveAttachmentUrl(m) : undefined; });

  constructor() {
    this._route.queryParamMap
      .pipe(
        map(params => params.get('message')),
        distinctUntilChanged(),
        tap(() => this.selected.set(null)),
        switchMap(id => {
          if (!id) {
            return of(null);
          }
          this.detailLoading.set(true);
          return this._api.getMessage(id).pipe(catchError(() => of(null)), tap(() => this.detailLoading.set(false)));
        }),
        takeUntilDestroyed()
      )
      .subscribe(message => {
        this.selected.set(message);
        if (message && !message.isRead) {
          this.markAsRead(message);
        }
      });
    this.store.changes$.pipe(takeUntilDestroyed()).subscribe(change => {
      if (change.kind === 'read' || change.kind === 'unread') {
        const item = this.list.items()?.find(m => m.id === change.messageId);
        if (item) {
          item.isRead = change.kind === 'read';
          this.list.items.update(items => items ? [...items] : items);
        }
        return;
      }
      if (change.kind === 'all-read') {
        // processed asynchronously by the server, so a reload could still return unread messages
        this.list.items.update(items => items?.map(m => ({ ...m, isRead: true })) ?? items);
        this.selected.update(m => m ? { ...m, isRead: true } : m);
        return;
      }
      this.list.refresh();
    });
  }

  public ngOnInit(): void {
    this.list.start();
  }

  protected refresh(): void {
    this.list.refresh();
    this.store.refresh();
  }

  protected select(message: InboxMessage | null): void {
    this._router.navigate([], { relativeTo: this._route, queryParams: { message: message?.id ?? null }, queryParamsHandling: 'merge' });
  }

  protected setStatus(value: InboxStatusFilter): void {
    this.setFilter(STATUS_FILTER, value);
  }

  protected setType(value: string): void {
    this.setFilter(TYPE_FILTER, value);
  }

  protected setShowExpired(value: boolean): void {
    this.setFilter(EXPIRED_FILTER, value ? 'true' : '');
  }

  protected markAsRead(message: InboxMessage): void {
    this.store.markAsRead(message).subscribe({ next: () => this.selected.update(m => m ? { ...m, isRead: true } : m), error: () => undefined });
  }

  protected markAsUnread(message: InboxMessage): void {
    this.store.markAsUnread(message).subscribe(() => this.selected.update(m => m ? { ...m, isRead: false } : m));
  }

  protected markAllAsRead(): void {
    this.store.markAllAsRead().subscribe(() => {
      this._toaster.show(ToastType.Success, 'Inbox.MarkAllAsReadSuccessTitle', 'Inbox.MarkAllAsReadSuccessMessage');
    });
  }

  protected remove(message: InboxMessage): void {
    this._dialog
      .confirm({ title: 'Inbox.Delete', message: 'Inbox.DeleteConfirmMessage', messageParams: { title: message.title ?? '' }, confirmLabel: 'Common.Delete', tone: 'danger' })
      .pipe(
        filter(confirmed => confirmed),
        switchMap(() => this.store.delete(message))
      )
      .subscribe(() => {
        this._toaster.show(ToastType.Success, 'Inbox.DeleteSuccessTitle', 'Inbox.DeleteSuccessMessage');
        this.select(null);
      });
  }

  private filterValue(member: string): string | undefined {
    const value = this.list.filters().find(f => f.member === member)?.value;
    return value === undefined || value === null ? undefined : String(value);
  }

  private setFilter(member: string, value: string): void {
    const filters = this.list.filters().filter(f => f.member !== member);
    if (value) {
      filters.push(new FilterClause(member, value, 'eq', 'string'));
    }
    this.list.setFilters(filters);
  }

  private toApiQuery(query: ListQuery): InboxMessagesQuery {
    const value = (member: string) => query.filters.find(f => f.member === member)?.value as string | undefined;
    const status = value(STATUS_FILTER);
    const typeId = value(TYPE_FILTER);
    return {
      page: query.page,
      size: query.pageSize,
      sort: query.dir === 'asc' ? query.sort : `${query.sort}-`,
      search: query.search || undefined,
      isRead: status === 'read' ? true : status === 'unread' ? false : undefined,
      typeId: typeId ? [typeId] : undefined,
      showExpired: String(value(EXPIRED_FILTER)) === 'true' || undefined
    };
  }
}
