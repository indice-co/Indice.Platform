import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { LucideChevronLeft, LucideChevronRight } from '@lucide/angular';

/** Page sizes the size picker offers. */
export const PAGE_SIZES = [20, 50, 100] as const;

/** Server-side paging control, the same as the Risk UI's: "a–b of n", a rows-per-page picker and ‹ page / pages ›. */
@Component({
  selector: 'app-pager',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DecimalPipe, LucideChevronLeft, LucideChevronRight],
  host: { class: 'flex flex-wrap items-center justify-between gap-3 text-sm' },
  template: `
    <p class="text-base-content/70">
      @if (count() > 0) {
        <span class="tabular-nums">{{ from() | number }}–{{ to() | number }}</span> of
        <span class="font-semibold tabular-nums text-base-content">{{ count() | number }}</span>
      } @else {
        No results
      }
    </p>
    <div class="flex items-center gap-3">
      <label class="flex items-center gap-2 text-base-content/70">
        <span class="hidden sm:inline">Rows per page</span>
        <select class="select select-sm w-20" (change)="onSize($event)">
          @for (s of sizes; track s) {
            <option [value]="s" [selected]="s === size()">{{ s }}</option>
          }
        </select>
      </label>
      <div class="join">
        <button
          type="button"
          class="join-item btn btn-sm btn-ghost"
          [disabled]="page() <= 1"
          aria-label="Previous page"
          (click)="pageChange.emit(page() - 1)"
        >
          <svg lucideChevronLeft class="size-4"></svg>
        </button>
        <span class="join-item btn btn-sm btn-ghost pointer-events-none tabular-nums">{{ page() }} / {{ pages() }}</span>
        <button
          type="button"
          class="join-item btn btn-sm btn-ghost"
          [disabled]="page() >= pages()"
          aria-label="Next page"
          (click)="pageChange.emit(page() + 1)"
        >
          <svg lucideChevronRight class="size-4"></svg>
        </button>
      </div>
    </div>
  `,
})
export class PagerComponent {
  /** 1-based. */
  readonly page = input(1);
  readonly size = input(20);
  /** Total items across all pages. */
  readonly count = input(0);

  readonly pageChange = output<number>();
  readonly sizeChange = output<number>();

  protected readonly sizes = PAGE_SIZES;
  protected readonly pages = computed(() => Math.max(1, Math.ceil(this.count() / this.size())));
  protected readonly from = computed(() => (this.page() - 1) * this.size() + 1);
  protected readonly to = computed(() => Math.min(this.count(), this.page() * this.size()));

  protected onSize(event: Event): void {
    this.sizeChange.emit(Number((event.target as HTMLSelectElement).value));
  }
}
