import { Component, ElementRef, OnDestroy, effect, inject, input, viewChild } from '@angular/core';
import { CategoryScale, Chart, Filler, LineController, LineElement, LinearScale, PointElement, Tooltip } from 'chart.js';
import { ThemeService } from 'src/app/layout/theme.service';
import { chartChrome, resolveColor, withAlpha } from './chart-theme';

Chart.register(LineController, LineElement, PointElement, LinearScale, CategoryScale, Filler, Tooltip);

export interface LineSeries {
  label: string;
  data: number[];
  /** Any CSS color, including theme variables such as `var(--app-chart-failed)`. */
  color: string;
}

/**
 * Stacked area line chart: every series is filled down to the one below it, so the top line is the total.
 * The legend is left to the host page. Re-renders when the theme changes.
 */
@Component({
  selector: 'app-stacked-line-chart',
  host: { class: 'relative block' },
  template: `<canvas #canvas></canvas>`
})
export class StackedLineChartComponent implements OnDestroy {
  private readonly _canvas = viewChild.required<ElementRef<HTMLCanvasElement>>('canvas');
  private readonly _theme = inject(ThemeService);
  private _chart?: Chart<'line'>;
  private _frame = 0;

  public readonly labels = input<string[]>([]);
  public readonly series = input<LineSeries[]>([]);

  constructor() {
    effect(() => {
      const labels = this.labels();
      const series = this.series();
      this._theme.isDark();
      // wait a frame so the theme attribute (applied by another effect) is in place before reading tokens
      cancelAnimationFrame(this._frame);
      this._frame = requestAnimationFrame(() => this.render(labels, series));
    });
  }

  public ngOnDestroy(): void {
    cancelAnimationFrame(this._frame);
    this._chart?.destroy();
  }

  private render(labels: string[], series: LineSeries[]): void {
    this._chart?.destroy();
    const canvas = this._canvas().nativeElement;
    const chrome = chartChrome(canvas);
    this._chart = new Chart(canvas, {
      type: 'line',
      data: {
        labels,
        datasets: series.map((s, index) => {
          const color = resolveColor(s.color, canvas);
          return {
            label: s.label,
            data: s.data,
            borderColor: color,
            backgroundColor: withAlpha(color, index === 0 ? 0.22 : 0.3),
            fill: index === 0 ? 'origin' : '-1',
            tension: 0.35,
            borderWidth: 2,
            pointRadius: 0,
            pointHoverRadius: 4,
            pointHoverBackgroundColor: color,
            pointHoverBorderColor: chrome.surface
          };
        })
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        interaction: { mode: 'index', intersect: false },
        scales: {
          x: {
            grid: { display: false },
            border: { display: false },
            ticks: { color: withAlpha(chrome.text, 0.55), font: { family: chrome.font, size: 11 }, maxRotation: 0, autoSkipPadding: 16 }
          },
          y: {
            stacked: true,
            beginAtZero: true,
            border: { display: false },
            grid: { color: chrome.grid },
            ticks: { color: withAlpha(chrome.text, 0.55), font: { family: chrome.font, size: 11 }, precision: 0, maxTicksLimit: 5 }
          }
        },
        plugins: {
          legend: { display: false },
          tooltip: {
            titleFont: { family: chrome.font },
            bodyFont: { family: chrome.font },
            callbacks: { label: ctx => ` ${ctx.dataset.label}: ${(ctx.parsed.y ?? 0).toLocaleString()}` }
          }
        }
      }
    });
  }
}
