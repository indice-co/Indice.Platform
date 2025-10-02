// gauge-chart.component.ts
import { Component, Input, OnInit, ViewChild, ElementRef, OnChanges, SimpleChanges, OnDestroy } from '@angular/core';
import { Chart, ChartConfiguration, ArcElement, Tooltip, Legend, DoughnutController } from 'chart.js';

// Register needed components for Chart.js v3+
Chart.register(ArcElement, Tooltip, Legend, DoughnutController);

export interface GaugeChartItem { name: string; value: number; color: string; }

@Component({
    selector: 'app-gauge-chart',
    template: `<canvas #gaugeCanvas></canvas>`,
    styles: [`
    :host { display: block; }
    canvas { max-width: 400px; max-height: 400px; position: relative; left: 50%; transform: translateX(-50%); }
  `],
    standalone: false
})
export class DoughnutChartComponent implements OnInit, OnChanges, OnDestroy {
  @ViewChild('gaugeCanvas', { static: true }) gaugeCanvas!: ElementRef<HTMLCanvasElement>;

  /** Items to render. Each item: { name, value, color } */
  @Input() items: GaugeChartItem[] = [];
  @Input() options: any = {};

  private chart?: Chart<'doughnut'>;

  public ngOnInit(): void {
    this.createChart();
  }

  public ngOnChanges(changes: SimpleChanges): void {
    if (this.chart && changes['items']) {
      const { labels, data, colors } = this.extractChartData();
      this.chart.data.labels = labels;
      (this.chart.data.datasets[0].data as number[]) = data;
      (this.chart.data.datasets[0].backgroundColor as string[]) = colors;
      this.chart.update();
    }
  }

  public ngOnDestroy(): void {
    this.chart?.destroy();
  }

  private createChart(): void {
    let delayed: boolean;
    const { labels, data, colors } = this.extractChartData();
    const config: ChartConfiguration<'doughnut'> = {
      type: 'doughnut',
      data: {
        labels,
        datasets: [
          {
            data,
            backgroundColor: colors,
            borderWidth: 0
          }
        ]
      },
      options: {
        animation: {
          onComplete: () => {
            delayed = true;
          },
          delay: (context) => {
            let delay = 0;
            if (context.type === 'data' && context.mode === 'default' && !delayed) {
              delay = context.dataIndex * 100 + context.datasetIndex * 100;
            }
            return delay;
          },
        },
        responsive: true,
        cutout: '70%',
        rotation: -90,
        circumference: 180,
        plugins: {
          legend: { position: 'bottom' },
          tooltip: {
            callbacks: {
              label: (ctx: any) => `${ctx.label}: ${ctx.parsed}`
            }
          }
        },
        ...this.options
      }
    };
    this.chart = new Chart(this.gaugeCanvas.nativeElement, config);
  }

  private extractChartData(): { labels: string[]; data: number[]; colors: string[] } {
    const items = this.items && this.items.length ? this.items : [];
    return {
      labels: items.map(i => i.name),
      data: items.map(i => i.value),
      colors: items.map(i => i.color)
    };
  }
}
