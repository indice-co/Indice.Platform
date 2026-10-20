import { DecimalPipe } from '@angular/common';
import { Component, computed, input, output } from '@angular/core';
import { NgIcon } from '@ng-icons/core';
import { TranslatePipe } from '@ngx-translate/core';

/** "21–40 of 1,248", page buttons (with gaps on long ranges) and the page size. */
@Component({
  selector: 'app-pager',
  imports: [DecimalPipe, NgIcon, TranslatePipe],
  template: `
    <p class="text-quiet kbd-ref">
      @if (count() > 0) {
        {{ 'list.range' | translate: { from: (from() | number), to: (to() | number), count: (count() | number) } }}
      } @else {
        {{ 'list.noResults' | translate }}
      }
    </p>
    <div class="flex flex-wrap items-center gap-3">
      @if (pageSizeOptions().length) {
        <label class="flex items-center gap-2 text-quiet">
          <span class="hidden sm:inline">{{ 'list.perPage' | translate }}</span>
          <select class="select select-sm w-20" [value]="pageSize()" (change)="pageSizeChange.emit(+$any($event.target).value)">
            @for (size of pageSizeOptions(); track size) {
              <option [value]="size" [selected]="size === pageSize()">{{ size }}</option>
            }
          </select>
        </label>
      }
      @if (pages() > 1) {
        <nav class="join" [attr.aria-label]="'list.pagination' | translate">
          <button type="button" class="join-item btn btn-sm btn-square" [disabled]="page() <= 1" (click)="pageChange.emit(page() - 1)"
                  [attr.aria-label]="'list.previousPage' | translate">
            <ng-icon name="lucideChevronLeft" size="1rem" />
          </button>
          @for (item of window(); track $index) {
            @if (item === null) {
              <span class="join-item btn btn-sm btn-disabled pointer-events-none px-2" aria-hidden="true">…</span>
            } @else {
              <button type="button" class="join-item btn btn-sm kbd-ref min-w-9" [class.btn-active]="item === page()" [class.font-semibold]="item === page()"
                      [attr.aria-current]="item === page() ? 'page' : null" (click)="pageChange.emit(item)">{{ item }}</button>
            }
          }
          <button type="button" class="join-item btn btn-sm btn-square" [disabled]="page() >= pages()" (click)="pageChange.emit(page() + 1)"
                  [attr.aria-label]="'list.nextPage' | translate">
            <ng-icon name="lucideChevronRight" size="1rem" />
          </button>
        </nav>
      }
    </div>
  `,
  host: { class: 'flex flex-wrap items-center justify-between gap-3' }
})
export class PagerComponent {
  public readonly page = input(1);
  public readonly pageSize = input(20);
  public readonly count = input(0);
  public readonly pageSizeOptions = input<number[]>([10, 20, 50, 100]);
  public readonly pageChange = output<number>();
  public readonly pageSizeChange = output<number>();

  protected readonly pages = computed(() => Math.max(1, Math.ceil(this.count() / Math.max(1, this.pageSize()))));
  protected readonly from = computed(() => (this.page() - 1) * this.pageSize() + 1);
  protected readonly to = computed(() => Math.min(this.count(), this.page() * this.pageSize()));

  /** Page numbers to show; `null` marks a gap. Always the first, the last and two either side of the current page. */
  protected readonly window = computed<(number | null)[]>(() => {
    const total = this.pages();
    const current = this.page();
    const shown = new Set([1, total, current - 1, current, current + 1].filter(p => p >= 1 && p <= total));
    if (current <= 3) { [2, 3, 4].filter(p => p <= total).forEach(p => shown.add(p)); }
    if (current >= total - 2) { [total - 1, total - 2, total - 3].filter(p => p >= 1).forEach(p => shown.add(p)); }
    const sorted = [...shown].sort((a, b) => a - b);
    const result: (number | null)[] = [];
    sorted.forEach((p, i) => {
      if (i > 0 && p - sorted[i - 1] > 1) {
        result.push(null);
      }
      result.push(p);
    });
    return result;
  });
}
