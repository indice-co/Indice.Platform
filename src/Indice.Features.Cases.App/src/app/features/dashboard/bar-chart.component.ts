import { Component, DestroyRef, ElementRef, afterNextRender, effect, inject, input, untracked, viewChild } from '@angular/core';
import Chart from 'chart.js/auto';
import { ThemeService } from 'src/app/layout/theme.service';

/**
 * Single-series column chart (one hue, magnitude only), themed from CSS tokens at render time so it follows
 * light / dark switching. A visually hidden table carries the same values for assistive tech.
 */
@Component({
  selector: 'app-bar-chart',
  host: { class: 'block' },
  template: `
    <div class="relative" [style.height.px]="height()">
      <canvas #canvas role="img" [attr.aria-label]="label()"></canvas>
    </div>
    <table class="sr-only">
      <caption>{{ label() }}</caption>
      <tbody>
        @for (category of categories(); track $index) {
          <tr><th scope="row">{{ category }}</th><td>{{ values()[$index] }}</td></tr>
        }
      </tbody>
    </table>
  `
})
export class BarChartComponent {
  public readonly categories = input.required<string[]>();
  public readonly values = input.required<number[]>();
  public readonly label = input.required<string>();
  /** Index of a mark to emphasise (e.g. today); the rest are drawn in a lighter step of the same hue. */
  public readonly emphasis = input<number | undefined>(undefined);
  public readonly height = input(160);

  private readonly _canvas = viewChild.required<ElementRef<HTMLCanvasElement>>('canvas');
  private readonly _theme = inject(ThemeService);
  private _chart?: Chart;
  private _rendered = false;

  constructor() {
    afterNextRender(() => {
      this._rendered = true;
      this.render();
    });
    effect(() => {
      // re-render on data or theme change; the theme attribute is applied by another effect, so wait a frame
      this.categories();
      this.values();
      this.emphasis();
      this._theme.isDark();
      untracked(() => this._rendered && requestAnimationFrame(() => this.render()));
    });
    inject(DestroyRef).onDestroy(() => this._chart?.destroy());
  }

  private render(): void {
    const styles = getComputedStyle(this._canvas().nativeElement);
    const token = (name: string) => styles.getPropertyValue(name).trim();
    const series = token('--app-chart-1') || token('--color-primary');
    const muted = mix(series, 0.45);
    const ink = mix(token('--color-base-content'), 0.6);
    const grid = token('--color-base-300');
    const emphasis = this.emphasis();
    const colors = this.values().map((_, i) => emphasis === undefined || i === emphasis ? series : muted);
    const reduceMotion = matchMedia('(prefers-reduced-motion: reduce)').matches;

    this._chart?.destroy();
    this._chart = new Chart(this._canvas().nativeElement, {
      type: 'bar',
      data: {
        labels: this.categories(),
        datasets: [{
          label: this.label(),
          data: this.values(),
          backgroundColor: colors,
          hoverBackgroundColor: series,
          borderRadius: { topLeft: 4, topRight: 4 },
          borderSkipped: 'start',
          maxBarThickness: 24,
          categoryPercentage: 0.8,
          barPercentage: 0.9
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        animation: reduceMotion ? false : { duration: 300 },
        interaction: { mode: 'index', intersect: false },
        plugins: {
          legend: { display: false },
          tooltip: {
            backgroundColor: token('--color-neutral'),
            titleColor: token('--color-neutral-content'),
            bodyColor: token('--color-neutral-content'),
            displayColors: false,
            padding: 8,
            cornerRadius: 6
          }
        },
        scales: {
          x: {
            grid: { display: false },
            border: { color: grid },
            ticks: { color: ink, font: { family: styles.fontFamily, size: 11 }, maxRotation: 0, autoSkipPadding: 8 }
          },
          y: {
            beginAtZero: true,
            border: { display: false },
            grid: { color: grid, lineWidth: 1 },
            ticks: { color: ink, font: { family: styles.fontFamily, size: 11 }, precision: 0, maxTicksLimit: 4 }
          }
        },
        layout: { padding: { top: 4 } }
      } as any
    });
  }
}

/** `color` at `alpha` opacity; works for hex and any CSS color the canvas understands. */
function mix(color: string, alpha: number): string {
  const hex = /^#([0-9a-f]{6})$/i.exec(color);
  if (hex) {
    const n = parseInt(hex[1], 16);
    return `rgba(${(n >> 16) & 255}, ${(n >> 8) & 255}, ${n & 255}, ${alpha})`;
  }
  return `color-mix(in oklab, ${color} ${alpha * 100}%, transparent)`;
}
