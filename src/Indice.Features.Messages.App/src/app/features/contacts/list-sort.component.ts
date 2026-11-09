import { Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideArrowDownWideNarrow, LucideArrowUpNarrowWide } from '@lucide/angular';
import { ListState } from 'src/app/ui/list/list-state';

export interface SortOption {
  /** API sort field. */
  value: string;
  /** Translation key. */
  label: string;
}

/** Sort field picker plus ascending / descending toggle bound to a {@link ListState}. */
@Component({
  selector: 'app-list-sort',
  imports: [TranslatePipe, LucideArrowDownWideNarrow, LucideArrowUpNarrowWide],
  host: { class: 'join' },
  template: `
    <select class="select select-sm join-item w-auto" [attr.aria-label]="'Shared.SortBy' | translate"
      (change)="list().setSort($any($event.target).value, list().dir())">
      @for (option of options(); track option.value) {
        <option [value]="option.value" [selected]="option.value === list().sort()">{{ option.label | translate }}</option>
      }
    </select>
    <button type="button" class="btn btn-sm join-item btn-square" (click)="list().setSort(list().sort(), list().dir() === 'asc' ? 'desc' : 'asc')"
      [attr.aria-label]="(list().dir() === 'asc' ? 'Shared.SortAscending' : 'Shared.SortDescending') | translate"
      [attr.title]="(list().dir() === 'asc' ? 'Shared.SortAscending' : 'Shared.SortDescending') | translate">
      @if (list().dir() === 'asc') {
        <svg lucideArrowUpNarrowWide class="size-4"></svg>
      } @else {
        <svg lucideArrowDownWideNarrow class="size-4"></svg>
      }
    </button>
  `
})
export class ListSortComponent {
  public readonly list = input.required<ListState<any>>();
  public readonly options = input.required<SortOption[]>();
}
