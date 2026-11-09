import { Component, computed, input } from '@angular/core';
import { LucideArrowDown, LucideArrowUp, LucideChevronsUpDown } from '@lucide/angular';
import { ListState } from './list-state';

/**
 * Sortable column header bound to a {@link ListState}:
 * `<th app-sort-header [list]="list" field="name">Name</th>`.
 */
@Component({
  selector: 'th[app-sort-header]',
  imports: [LucideArrowDown, LucideArrowUp, LucideChevronsUpDown],
  host: {
    '[attr.aria-sort]': "active() ? (list().dir() === 'asc' ? 'ascending' : 'descending') : null"
  },
  template: `
    <button type="button" class="th-sort" [class.text-base-content]="active()" (click)="list().setSort(field())">
      <ng-content />
      @if (!active()) {
        <svg lucideChevronsUpDown class="size-3.5 opacity-40"></svg>
      } @else if (list().dir() === 'asc') {
        <svg lucideArrowUp class="size-3.5 text-primary"></svg>
      } @else {
        <svg lucideArrowDown class="size-3.5 text-primary"></svg>
      }
    </button>
  `
})
export class SortHeaderComponent {
  public readonly list = input.required<ListState<any>>();
  public readonly field = input.required<string>();
  protected readonly active = computed(() => this.list().sort() === this.field());
}
