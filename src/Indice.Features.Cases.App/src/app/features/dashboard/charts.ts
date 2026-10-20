import { DecimalPipe } from '@angular/common';
import { Component, computed, input, output } from '@angular/core';
import { Params, RouterLink } from '@angular/router';
import { CaseStatus } from 'src/app/core/services/cases-api.service';
import { inkOf } from 'src/app/ui/status/case-status';
import { niceMax } from './dashboard-math';

export interface ChartDatum {
  key: string;
  label: string;
  value: number | null;
  /** Longer label for tooltips and tables (e.g. a date range). */
  detail?: string;
  /** CSS colour of the mark; defaults to the series colour. */
  color?: string;
  /** Opens the case list with these query params. */
  queryParams?: Params;
}

/**
 * Column chart of one series over time. Past periods in the neutral bar colour and the current (last) period in the
 * series hue, thin columns rounded at the data end, a hairline grid at 0 / half / max, values labelled only on the last
 * and the highest column; every column has a tooltip on hover and focus.
 */
@Component({
  selector: 'app-column-chart',
  imports: [DecimalPipe, RouterLink],
  template: `
    <div class="flex gap-3 pt-3">
      <div class="relative h-36 w-8 shrink-0 text-right" aria-hidden="true">
        @for (tick of ticks(); track tick) {
          <span class="absolute right-0 -translate-y-1/2 text-xs text-quiet kbd-ref" [style.bottom.%]="tick / max() * 100">{{ tick | number }}</span>
        }
      </div>
      <div class="min-w-0 flex-1">
        <div class="relative h-36">
          @for (tick of ticks(); track tick) {
            <div class="absolute inset-x-0 h-px" [style.bottom.%]="tick / max() * 100"
                 [style.background-color]="tick === 0 ? 'var(--cases-chart-axis)' : 'var(--cases-chart-grid)'"></div>
          }
          <div class="relative flex h-full items-end">
            @for (bar of bars(); track bar.key; let first = $first, last = $last) {
              <a class="group relative flex h-full flex-1 items-end justify-center rounded-field outline-offset-0"
                 [routerLink]="bar.queryParams ? '/cases' : null" [queryParams]="bar.queryParams"
                 (click)="activate.emit(bar)"
                 [attr.aria-label]="bar.detail + ': ' + (bar.value ?? '—')">
                <span class="relative w-[min(1.5rem,55%)] rounded-t-[4px] transition-[filter] group-hover:brightness-110 group-focus-visible:brightness-110"
                      [style.height.%]="(bar.value ?? 0) / max() * 100"
                      [style.min-height.px]="bar.value ? 2 : 0"
                      [style.background-color]="bar.color ?? (last ? 'var(--cases-chart-1)' : 'var(--cases-chart-bar)')">
                  @if (last || bar.value === peak()) {
                    <span class="absolute bottom-full left-1/2 mb-1 -translate-x-1/2 text-xs font-medium whitespace-nowrap kbd-ref">{{ bar.value | number }}</span>
                  }
                </span>
                <span class="chart-tooltip invisible opacity-0 transition-opacity group-hover:visible group-hover:opacity-100 group-focus-visible:visible group-focus-visible:opacity-100"
                      [class]="first ? 'left-0' : last ? 'right-0' : 'left-1/2 -translate-x-1/2'"
                      [style.bottom]="'calc(' + ((bar.value ?? 0) / max() * 100) + '% + 1.75rem)'" role="tooltip">
                  <strong class="kbd-ref">{{ bar.value === null ? '—' : (bar.value | number) }}</strong>
                  <span class="whitespace-nowrap">{{ bar.detail }}</span>
                </span>
              </a>
            }
          </div>
        </div>
        <div class="mt-2 flex" aria-hidden="true">
          <!-- On a phone every other label (counting back from the current period) keeps them apart. -->
          @for (bar of bars(); track bar.key; let i = $index, count = $count) {
            <span class="flex-1 truncate text-center text-xs text-quiet kbd-ref max-sm:overflow-visible" [class.max-sm:invisible]="(count - 1 - i) % 2 === 1">{{ bar.label }}</span>
          }
        </div>
      </div>
    </div>
  `
})
export class ColumnChartComponent {
  public readonly bars = input<ChartDatum[]>([]);
  public readonly activate = output<ChartDatum>();
  protected readonly peak = computed(() => Math.max(0, ...this.bars().map(b => b.value ?? 0)));
  protected readonly max = computed(() => niceMax(this.peak()));
  protected readonly ticks = computed(() => [0, this.max() / 2, this.max()]);
}

/**
 * Horizontal bars, label on the left and the value at the bar's tip. Nominal categories share the neutral bar colour with
 * the leader in the series hue; ordered ones (age bands) pass their own colours. Past `limit` rows the tail folds into
 * "Other".
 */
@Component({
  selector: 'app-bar-list',
  imports: [DecimalPipe, RouterLink],
  template: `
    <ul class="space-y-1">
      @for (row of rows(); track row.key; let first = $first) {
        <li>
          <a class="group grid grid-cols-[minmax(6rem,11rem)_minmax(0,1fr)] items-center gap-3 rounded-field px-2 py-1.5"
             [class]="row.queryParams ? 'hover:bg-base-200' : 'pointer-events-none'"
             [routerLink]="row.queryParams ? '/cases' : null" [queryParams]="row.queryParams" (click)="activate.emit(row)"
             [attr.title]="row.detail ?? row.label">
            <span class="truncate text-[0.8125rem]" [class]="row.queryParams ? 'group-hover:underline' : ''">{{ row.label }}</span>
            <span class="flex min-w-0 items-center gap-2">
              <span class="h-2.5 shrink-0 rounded-r-[4px]" [style.width.%]="(row.value ?? 0) / max() * 85"
                    [style.min-width.px]="row.value ? 2 : 0"
                    [style.background-color]="row.color ?? (first && row.value ? 'var(--cases-chart-1)' : 'var(--cases-chart-bar)')"></span>
              <span class="text-[0.8125rem] font-medium kbd-ref">{{ row.value === null ? '—' : (row.value | number) }}</span>
            </span>
          </a>
        </li>
      }
    </ul>
  `
})
export class BarListComponent {
  public readonly data = input<ChartDatum[]>([]);
  public readonly limit = input(8);
  /** Label of the folded tail. */
  public readonly otherLabel = input('Other');
  public readonly activate = output<ChartDatum>();

  protected readonly rows = computed(() => {
    const data = this.data();
    if (data.length <= this.limit()) {
      return data;
    }
    const head = data.slice(0, this.limit() - 1);
    const tail = data.slice(this.limit() - 1);
    return [...head, { key: '__other', label: this.otherLabel(), value: tail.reduce((sum, d) => sum + (d.value ?? 0), 0), color: 'var(--cases-chart-bar)' }];
  });
  protected readonly max = computed(() => Math.max(1, ...this.rows().map(r => r.value ?? 0)));
}

/**
 * One part-to-whole bar (2px surface gaps between segments) with a legend that doubles as the table: swatch, label,
 * count and share. Segments and legend rows open the case list when they carry query params.
 */
@Component({
  selector: 'app-stacked-bar',
  imports: [DecimalPipe, RouterLink],
  template: `
    <div class="flex h-3 w-full gap-[2px] overflow-hidden rounded-[4px] bg-base-200" role="img" [attr.aria-label]="ariaLabel()">
      @for (segment of visible(); track segment.key) {
        <a class="tooltip h-full transition-[filter] hover:brightness-110 focus-visible:brightness-110"
           [attr.data-tip]="segment.label + ': ' + (segment.value | number)"
           [style.flex-grow]="segment.value" [style.background-color]="segment.color"
           [routerLink]="segment.queryParams ? '/cases' : null" [queryParams]="segment.queryParams"
           [attr.aria-label]="segment.label + ': ' + segment.value"></a>
      }
    </div>
    <ul class="mt-4 space-y-0.5">
      @for (segment of data(); track segment.key) {
        <li>
          <a class="flex items-center gap-2.5 rounded-field px-2 py-1.5 text-[0.8125rem]"
             [class]="segment.queryParams ? 'hover:bg-base-200' : 'pointer-events-none'"
             [routerLink]="segment.queryParams ? '/cases' : null" [queryParams]="segment.queryParams">
            <span class="size-2.5 shrink-0 rounded-[2px]" [style.background-color]="segment.color"></span>
            <span class="min-w-0 flex-1 truncate">{{ segment.label }}</span>
            <span class="font-medium kbd-ref">{{ segment.value === null ? '—' : (segment.value | number) }}</span>
            <span class="w-11 text-right text-quiet kbd-ref">{{ share(segment) }}</span>
          </a>
        </li>
      }
      @if (totalLabel()) {
        <li class="flex items-center gap-2.5 border-t border-base-200 px-2 pt-2 text-[0.8125rem]">
          <span class="size-2.5 shrink-0"></span>
          <span class="min-w-0 flex-1 text-quiet">{{ totalLabel() }}</span>
          <span class="font-medium kbd-ref">{{ total() | number }}</span>
          <span class="w-11"></span>
        </li>
      }
    </ul>
  `
})
export class StackedBarComponent {
  public readonly data = input<ChartDatum[]>([]);
  public readonly ariaLabel = input('');
  /** Adds a total row under the legend when set. */
  public readonly totalLabel = input('');
  protected readonly total = computed(() => this.data().reduce((sum, d) => sum + (d.value ?? 0), 0));
  protected readonly visible = computed(() => this.data().filter(d => (d.value ?? 0) > 0));

  protected share(segment: ChartDatum): string {
    const total = this.total();
    return total ? `${Math.round(((segment.value ?? 0) / total) * 100)}%` : '';
  }
}

export interface RouteStation extends ChartDatum {
  status: CaseStatus | 'other';
}

/**
 * Where open cases are waiting: the open checkpoints as stops on one line, Submitted checkpoints before In progress ones,
 * each with the number of cases waiting there. The busiest stop carries the series hue. On narrow screens the line folds
 * into a list. Stops open the case list filtered to their checkpoint.
 */
@Component({
  selector: 'app-checkpoint-route',
  imports: [DecimalPipe, RouterLink],
  template: `
    <ol class="route" [style.--stops]="stations().length">
      @for (stop of stations(); track stop.key) {
        <li class="route-stop" [class]="ink(stop.status)" [class.is-peak]="stop.key === peakKey()">
          <a class="route-link" [routerLink]="stop.queryParams ? '/cases' : null" [queryParams]="stop.queryParams" (click)="activate.emit(stop)"
             [attr.aria-label]="stop.label + ': ' + (stop.value ?? '—')">
            <span class="route-count kbd-ref">{{ stop.value === null ? '—' : (stop.value | number) }}</span>
            <span class="route-dot" aria-hidden="true"></span>
            <span class="route-label">
              {{ stop.label }}
              @if (stop.key === peakKey() && peakLabel()) {
                <span class="route-note">{{ peakLabel() }}</span>
              }
            </span>
          </a>
        </li>
      }
    </ol>
  `
})
export class CheckpointRouteComponent {
  public readonly stations = input<RouteStation[]>([]);
  /** Caption under the busiest stop ("Most waiting"). */
  public readonly peakLabel = input('');
  public readonly activate = output<ChartDatum>();

  /** The busiest stop, when one stop clearly leads (ties mark nothing). */
  protected readonly peakKey = computed(() => {
    const values = this.stations().map(stop => stop.value ?? 0);
    const peak = Math.max(0, ...values);
    return peak > 0 && values.filter(value => value === peak).length === 1 ? this.stations()[values.indexOf(peak)].key : null;
  });

  protected ink(status: RouteStation['status']): string {
    return status === 'other' ? 'ink-deleted' : inkOf(status);
  }
}
