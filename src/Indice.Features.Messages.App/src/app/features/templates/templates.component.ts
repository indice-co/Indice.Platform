import { Component, OnInit, computed, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { filter, switchMap } from 'rxjs';
import { LucideArrowDown, LucideArrowUp, LucideFileText, LucidePencil, LucidePlus, LucideTrash } from '@lucide/angular';
import { MessagesApiClient, TemplateListItem, TemplateType } from 'src/app/core/services/messages-api.service';
import { AppTranslatedToaster, ToastType } from 'src/app/shared/services/app-translated-toaster';
import { CHANNEL_KINDS, ChannelIconComponent, MessageTypeBadgeComponent } from 'src/app/ui/badges/badges';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { FilterClause } from 'src/app/ui/list/filter-clause';
import { ListState } from 'src/app/ui/list/list-state';
import { PaginationComponent } from 'src/app/ui/list/pagination.component';
import { SearchInputComponent } from 'src/app/ui/list/search-input.component';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';
import { TEMPLATE_TYPES, templateTypeBadge, templateTypeLabel } from './template-type';
import { ListCountComponent } from 'src/app/ui/list/list-count.component';

@Component({
  selector: 'app-templates',
  imports: [
    ListCountComponent, DatePipe, RouterLink, TranslatePipe, PageHeaderComponent, EmptyStateComponent, PaginationComponent, SearchInputComponent,
    ChannelIconComponent, MessageTypeBadgeComponent, LucideArrowDown, LucideArrowUp, LucideFileText, LucidePencil, LucidePlus, LucideTrash
  ],
  template: `
    <app-page-header [title]="'Templates.Title' | translate" [subtitle]="'Templates.Subtitle' | translate">
      <app-list-count class="mt-0.5" [count]="list.items() ? list.count() : null" one="Templates.CountOne" other="Templates.CountOther" />
      <div actions>
        <a class="btn btn-primary btn-sm" routerLink="add-template">
          <svg lucidePlus class="size-4"></svg>{{ 'Templates.CreateTemplateDialogTitle' | translate }}
        </a>
      </div>
    </app-page-header>

    <div class="mb-3 flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
      <app-search-input class="sm:w-72" [value]="list.search()" [placeholder]="'Shared.Search' | translate" (search)="list.setSearch($event)" />
      <div class="join" role="group" [attr.aria-label]="'Templates.KindLabel' | translate">
        <button type="button" class="btn btn-sm join-item" [class.btn-active]="!typeFilter()" (click)="setType(undefined)">
          {{ 'Templates.AllTypes' | translate }}
        </button>
        @for (type of types; track type) {
          <button type="button" class="btn btn-sm join-item" [class.btn-active]="typeFilter() === type" (click)="setType(type)">
            {{ typeLabel(type) | translate }}
          </button>
        }
      </div>
    </div>

    <div class="panel overflow-x-auto">
      <table class="table">
        <thead>
          <tr>
            <th>
              <button type="button" class="inline-flex items-center gap-1 uppercase" (click)="list.setSort('name')">
                {{ 'Templates.NameColumn' | translate }}
                @if (list.sort() === 'name') {
                  @if (list.dir() === 'asc') { <svg lucideArrowUp class="size-3.5"></svg> } @else { <svg lucideArrowDown class="size-3.5"></svg> }
                }
              </button>
            </th>
            <th>
              <button type="button" class="inline-flex items-center gap-1 uppercase" (click)="list.setSort('type')">
                {{ 'Templates.TypeColumn' | translate }}
                @if (list.sort() === 'type') {
                  @if (list.dir() === 'asc') { <svg lucideArrowUp class="size-3.5"></svg> } @else { <svg lucideArrowDown class="size-3.5"></svg> }
                }
              </button>
            </th>
            <th>{{ 'Templates.ChannelsColumn' | translate }}</th>
            <th>{{ 'Templates.MessageTypeColumn' | translate }}</th>
            <th>{{ 'Templates.UpdatedAtColumn' | translate }}</th>
            <th class="w-24 text-right"><span class="sr-only">{{ 'Templates.ActionsColumn' | translate }}</span></th>
          </tr>
        </thead>
        <tbody>
          @if (list.loading() && !list.items()?.length) {
            @for (row of [1, 2, 3, 4]; track row) {
              <tr><td colspan="6"><div class="skeleton h-4 w-full"></div></td></tr>
            }
          } @else {
            @for (item of list.items(); track item.id) {
              <tr class="hover:bg-base-200/60" [class.opacity-60]="list.loading()">
                <td>
                  <a class="link link-hover font-semibold" [routerLink]="[item.id]">{{ item.name }}</a>
                  @if (item.alias) {
                    <div><code class="rounded bg-base-200 px-1.5 py-0.5 text-xs">{{ item.alias }}</code></div>
                  }
                </td>
                <td><span class="badge badge-sm whitespace-nowrap" [class]="typeBadge(item.type)">{{ typeLabel(item.type) | translate }}</span></td>
                <td>
                  <div class="flex items-center gap-1">
                    @for (kind of channelKinds; track kind) {
                      <app-channel-icon [kind]="kind" [muted]="!item.channels?.includes($any(kind))" />
                    }
                  </div>
                </td>
                <td><app-message-type-badge [name]="item.messageType?.name" [classification]="item.messageType?.classification" /></td>
                <td class="whitespace-nowrap">
                  @if (item.updatedAt || item.createdAt) {
                    <div class="text-sm">{{ (item.updatedAt || item.createdAt) | date: 'medium' }}</div>
                    @if (item.updatedBy || item.createdBy) {
                      <div class="text-xs text-base-content/60">{{ item.updatedBy || item.createdBy }}</div>
                    }
                  } @else {
                    <span class="text-base-content/40">—</span>
                  }
                </td>
                <td class="text-right whitespace-nowrap">
                  <span class="tooltip" [attr.data-tip]="'Templates.EditTemplateTitle' | translate">
                    <a class="btn btn-ghost btn-xs btn-square" [routerLink]="[item.id]" [attr.aria-label]="'Templates.EditTemplateTitle' | translate">
                      <svg lucidePencil class="size-4"></svg>
                    </a>
                  </span>
                  <span class="tooltip tooltip-left" [attr.data-tip]="'Templates.DeleteTemplateTitle' | translate">
                    <button type="button" class="btn btn-ghost btn-xs btn-square text-error" (click)="delete(item)"
                      [attr.aria-label]="'Templates.DeleteTemplateTitle' | translate">
                      <svg lucideTrash class="size-4"></svg>
                    </button>
                  </span>
                </td>
              </tr>
            } @empty {
              <tr>
                <td colspan="6">
                  <app-empty-state [title]="'Shared.ListViewEmptyStateTitle' | translate" [message]="'Shared.ListViewEmptyStateSubTitle' | translate">
                    <svg icon lucideFileText class="size-5"></svg>
                    <a actions class="btn btn-sm" routerLink="add-template">
                      <svg lucidePlus class="size-4"></svg>{{ 'Templates.CreateTemplateDialogTitle' | translate }}
                    </a>
                  </app-empty-state>
                </td>
              </tr>
            }
          }
        </tbody>
      </table>
      @if (list.count() > 0) {
        <app-pagination class="border-t border-base-300 px-4 py-3" [page]="list.page()" [pageSize]="list.pageSize()" [count]="list.count()"
          (pageChange)="list.setPage($event)" (pageSizeChange)="list.setPageSize($event)" />
      }
    </div>
  `
})
export class TemplatesComponent implements OnInit {
  private readonly _api = inject(MessagesApiClient);
  private readonly _dialog = inject(DialogService);
  private readonly _toaster = inject(AppTranslatedToaster);

  protected readonly types = TEMPLATE_TYPES;
  protected readonly channelKinds = CHANNEL_KINDS;
  protected readonly typeLabel = templateTypeLabel;
  protected readonly typeBadge = templateTypeBadge;

  protected readonly list = new ListState<TemplateListItem>({
    pageSize: 20,
    sort: 'name',
    dir: 'asc',
    load: query => this._api.getTemplates(
      query.page, query.pageSize, query.dir === 'asc' ? query.sort : `${query.sort}-`, query.search || undefined, undefined, undefined,
      query.filters.find(f => f.member === 'type')?.value as TemplateType | undefined
    )
  });

  protected readonly typeFilter = computed(() => this.list.filters().find(f => f.member === 'type')?.value as TemplateType | undefined);

  public ngOnInit(): void {
    this.list.start();
  }

  protected setType(type: TemplateType | undefined): void {
    this.list.setFilters(type ? [new FilterClause('type', type)] : []);
  }

  protected delete(template: TemplateListItem): void {
    this._dialog
      .confirm({ title: 'Templates.DeleteTemplateTitle', message: 'Templates.DeleteConfirmMessage', messageParams: { name: template.name }, confirmLabel: 'Common.Delete', tone: 'danger' })
      .pipe(
        filter(confirmed => confirmed),
        switchMap(() => this._api.deleteTemplate(template.id!))
      )
      .subscribe(() => {
        this._toaster.show(ToastType.Success, 'Templates.DeleteSuccessTitle', 'Templates.DeleteSuccessMessage', undefined, { name: template.name });
        this.list.refresh();
      });
  }
}
