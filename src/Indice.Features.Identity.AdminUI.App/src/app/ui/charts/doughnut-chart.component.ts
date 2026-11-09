import { Component, ElementRef, OnDestroy, effect, inject, input, viewChild } from '@angular/core';
import { ArcElement, Chart, DoughnutController, Tooltip } from 'chart.js';
import { ThemeService } from 'src/app/layout/theme.service';
import { chartChrome, resolveColor } from './chart-theme';

Chart.register(ArcElement, Tooltip, DoughnutController);

export interface DoughnutChartItem {
  name: string;
  value: number;
  /** Any CSS color, including theme variables such as `var(--app-channel-email)`. */
  color: string;
}

/**
 * Doughnut (or, with `half`, a semicircle gauge) breakdown. The legend is left to the host page, which usually
 * renders it as a list next to the chart. Re-renders when the theme changes.
 */
@Component({
  selector: 'app-doughnut-chart',
  host: { class: 'relative block' },
  template: `<canvas #canvas></canvas>`
})
export class DoughnutChartComponent implements OnDestroy {
  private readonly _canvas = viewChild.required<ElementRef<HTMLCanvasElement>>('canvas');
  private readonly _theme = inject(ThemeService);
  private _chart?: Chart<'doughnut'>;
  private _frame = 0;

  public readonly items = input<DoughnutChartItem[]>([]);
  public readonly half = input(false);
  public readonly cutout = input('72%');

  constructor() {
    effect(() => {
      const items = this.items();
      this._theme.isDark();
      // wait a frame so the theme attribute (applied by another effect) is in place before reading tokens
      cancelAnimationFrame(this._frame);
      this._frame = requestAnimationFrame(() => this.render(items));
    });
  }

  public ngOnDestroy(): void {
    cancelAnimationFrame(this._frame);
    this._chart?.destroy();
  }

  private render(items: DoughnutChartItem[]): void {
    this._chart?.destroy();
    const canvas = this._canvas().nativeElement;
    const chrome = chartChrome(canvas);
    const hasData = items.some(i => i.value > 0);
    this._chart = new Chart(canvas, {
      type: 'doughnut',
      data: {
        labels: hasData ? items.map(i => i.name) : [''],
        datasets: [{
          data: hasData ? items.map(i => i.value) : [1],
          backgroundColor: hasData ? items.map(i => resolveColor(i.color, canvas)) : [chrome.grid],
          borderColor: chrome.surface,
          borderWidth: 2,
          hoverOffset: hasData ? 4 : 0
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        cutout: this.cutout(),
        rotation: this.half() ? -90 : 0,
        circumference: this.half() ? 180 : 360,
        plugins: {
          legend: { display: false },
          tooltip: {
            enabled: hasData,
            bodyFont: { family: chrome.font },
            callbacks: { label: ctx => ` ${ctx.label}: ${ctx.parsed.toLocaleString()}` }
          }
        }
      }
    });
  }
}
