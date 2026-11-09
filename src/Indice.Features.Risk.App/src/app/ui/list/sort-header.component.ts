import { Component, computed, input, output } from '@angular/core';
import { LucideArrowDown, LucideArrowUp, LucideArrowUpDown } from '@lucide/angular';
import { SortDirection } from './list-state';

/** Sortable table header button: `<th><app-sort-header field="createdAt" label="Created" … /></th>`. */
@Component({
  selector: 'app-sort-header',
  imports: [LucideArrowDown, LucideArrowUp, LucideArrowUpDown],
  template: `
    <button type="button" class="-mx-1 inline-flex items-center gap-1 rounded px-1 hover:text-base-content" [class.text-base-content]="active()"
      (click)="sortChange.emit(field())">
      {{ label() }}
      @if (!active()) {
        <svg lucideArrowUpDown class="size-3.5 opacity-40"></svg>
      } @else if (dir() === 'asc') {
        <svg lucideArrowUp class="size-3.5"></svg>
      } @else {
        <svg lucideArrowDown class="size-3.5"></svg>
      }
    </button>
  `
})
export class SortHeaderComponent {
  public readonly field = input.required<string>();
  public readonly label = input.required<string>();
  public readonly sort = input.required<string>();
  public readonly dir = input.required<SortDirection>();
  public readonly sortChange = output<string>();
  protected readonly active = computed(() => this.sort() === this.field());
}
