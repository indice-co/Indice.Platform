import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/** One line of the chart. `color` is a CSS colour, typically a theme var such as `var(--color-info)`. */
export interface SparklineSeries {
  label: string;
  color: string;
  values: number[];
}

/** Chart width in viewBox units; the SVG stretches to its container. */
const WIDTH = 600;
/** Keeps the stroke from being clipped at the top and bottom edges. */
const PAD = 2;

/** Dependency-free multi-line chart for the dashboard's live queue depth. */
@Component({
  selector: 'app-sparkline-chart',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <svg
      [attr.viewBox]="'0 0 ' + width + ' ' + height()"
      preserveAspectRatio="none"
      class="w-full"
      [style.height.px]="height()"
      role="img"
      [attr.aria-label]="ariaLabel()"
    >
      @for (y of gridLines(); track $index) {
        <line
          x1="0"
          [attr.x2]="width"
          [attr.y1]="y"
          [attr.y2]="y"
          stroke="var(--color-base-300)"
          stroke-width="1"
          vector-effect="non-scaling-stroke"
        />
      }
      @for (line of lines(); track line.label) {
        <polyline
          fill="none"
          stroke-width="2"
          stroke-linejoin="round"
          stroke-linecap="round"
          vector-effect="non-scaling-stroke"
          [attr.stroke]="line.color"
          [attr.points]="line.points"
        />
      }
    </svg>
    <ul class="mt-3 flex flex-wrap gap-x-5 gap-y-1 text-xs text-base-content/70">
      @for (line of lines(); track line.label) {
        <li class="flex items-center gap-1.5">
          <span class="size-2 rounded-full" [style.background-color]="line.color"></span>
          {{ line.label }}
          <span class="font-mono font-medium text-base-content">{{ line.last }}</span>
        </li>
      }
    </ul>
  `,
})
export class SparklineChartComponent {
  readonly series = input.required<SparklineSeries[]>();
  readonly height = input(160);

  protected readonly width = WIDTH;

  private readonly max = computed(() =>
    Math.max(1, ...this.series().flatMap((s) => s.values)),
  );

  protected readonly gridLines = computed(() => {
    const h = this.height();
    return [0, 1, 2, 3].map((i) => PAD + ((h - 2 * PAD) * i) / 3);
  });

  protected readonly lines = computed(() => {
    const h = this.height();
    const max = this.max();
    return this.series().map((s) => {
      const n = s.values.length;
      const step = n > 1 ? WIDTH / (n - 1) : 0;
      const points = s.values
        .map((v, i) => {
          const x = n > 1 ? i * step : WIDTH;
          const y = h - PAD - (v / max) * (h - 2 * PAD);
          return `${round(x)},${round(y)}`;
        })
        .join(' ');
      return { label: s.label, color: s.color, points, last: n ? s.values[n - 1] : 0 };
    });
  });

  protected readonly ariaLabel = computed(() =>
    this.lines()
      .map((l) => `${l.label} ${l.last}`)
      .join(', '),
  );
}

function round(value: number): number {
  return Math.round(value * 10) / 10;
}
