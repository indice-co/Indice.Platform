import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

/** Page sizes the size picker offers. */
export const PAGE_SIZES = [20, 50, 100] as const;

/** Server-side paging control: « Prev · Page x of y · Next », a size picker and "Showing a–b of n". */
@Component({
  selector: 'app-pager',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-wrap items-center justify-between gap-3 text-sm">
      <span class="text-base-content/60">
        @if (count() > 0) {
          Showing {{ from() }}–{{ to() }} of {{ count() }}
        } @else {
          No items
        }
      </span>
      <div class="flex items-center gap-3">
        <select
          class="select select-sm w-auto"
          aria-label="Page size"
          [value]="size()"
          (change)="onSize($event)"
        >
          @for (s of sizes; track s) {
            <option [value]="s" [selected]="s === size()">{{ s }} / page</option>
          }
        </select>
        <div class="join">
          <button
            type="button"
            class="btn btn-sm join-item"
            [disabled]="page() <= 1"
            (click)="pageChange.emit(page() - 1)"
            aria-label="Previous page"
          >
            «
          </button>
          <span class="btn btn-sm join-item pointer-events-none no-animation">
            Page {{ page() }} of {{ pages() }}
          </span>
          <button
            type="button"
            class="btn btn-sm join-item"
            [disabled]="page() >= pages()"
            (click)="pageChange.emit(page() + 1)"
            aria-label="Next page"
          >
            »
          </button>
        </div>
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
