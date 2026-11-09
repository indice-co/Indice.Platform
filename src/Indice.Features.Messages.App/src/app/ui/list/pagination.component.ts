import { Component, computed, input, output } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideChevronLeft, LucideChevronRight } from '@lucide/angular';

/** Compact pager: range summary, page size picker and prev / page / next controls. */
@Component({
  selector: 'app-pagination',
  imports: [DecimalPipe, TranslatePipe, LucideChevronLeft, LucideChevronRight],
  host: { class: 'flex flex-wrap items-center justify-between gap-3 text-sm' },
  template: `
    <p class="text-base-content/70">
      @if (count() > 0) {
        {{ 'List.Range' | translate: { from: (from() | number), to: (to() | number), count: (count() | number) } }}
      } @else {
        {{ 'List.NoResults' | translate }}
      }
    </p>
    <div class="flex items-center gap-3">
      <label class="flex items-center gap-2 text-base-content/70">
        <span class="hidden sm:inline">{{ 'List.PerPage' | translate }}</span>
        <select class="select select-sm w-20" [value]="pageSize()" (change)="pageSizeChange.emit(+$any($event.target).value)">
          @for (size of pageSizes(); track size) {
            <option [value]="size" [selected]="size === pageSize()">{{ size }}</option>
          }
        </select>
      </label>
      <div class="join">
        <button type="button" class="join-item btn btn-sm btn-ghost" [disabled]="page() <= 1"
          [attr.aria-label]="'List.Previous' | translate" (click)="pageChange.emit(page() - 1)">
          <svg lucideChevronLeft class="size-4"></svg>
        </button>
        <span class="join-item btn btn-sm btn-ghost pointer-events-none tabular">{{ page() }} / {{ pageCount() }}</span>
        <button type="button" class="join-item btn btn-sm btn-ghost" [disabled]="page() >= pageCount()"
          [attr.aria-label]="'List.Next' | translate" (click)="pageChange.emit(page() + 1)">
          <svg lucideChevronRight class="size-4"></svg>
        </button>
      </div>
    </div>
  `
})
export class PaginationComponent {
  public readonly page = input.required<number>();
  public readonly pageSize = input.required<number>();
  public readonly count = input.required<number>();
  public readonly pageSizes = input<number[]>([10, 20, 50, 100]);
  public readonly pageChange = output<number>();
  public readonly pageSizeChange = output<number>();

  protected readonly pageCount = computed(() => Math.max(1, Math.ceil(this.count() / this.pageSize())));
  protected readonly from = computed(() => (this.page() - 1) * this.pageSize() + 1);
  protected readonly to = computed(() => Math.min(this.count(), this.page() * this.pageSize()));
}
