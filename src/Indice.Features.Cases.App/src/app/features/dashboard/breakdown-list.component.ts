import { Component, computed, input } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { GroupByReportResult } from 'src/app/core/services/cases-api.service';

const MAX_ROWS = 7;

/**
 * Ranked horizontal bars for a server "group by" report. One hue (it is one measure), value at the bar tip,
 * labels in text tokens. Rows past the 7th are folded into "Other".
 */
@Component({
  selector: 'app-breakdown-list',
  imports: [DecimalPipe, TranslatePipe],
  template: `
    <ul class="space-y-2.5" [attr.aria-label]="label()">
      @for (row of rows(); track row.label) {
        <li class="grid grid-cols-[minmax(0,9rem)_1fr] items-center gap-3 text-sm sm:grid-cols-[minmax(0,11rem)_1fr]"
          [title]="row.label + ': ' + row.count">
          <span class="truncate text-base-content/80">{{ row.other ? ('dashboard.other' | translate) : row.label }}</span>
          <span class="flex items-center gap-2">
            <span class="h-2.5 rounded-r-[4px]" [style.width.%]="row.share"
              [style.min-width.px]="row.count > 0 ? 3 : 0"
              [style.background]="row.other ? 'var(--app-chart-6)' : 'var(--app-chart-1)'"></span>
            <span class="docket shrink-0 text-xs font-medium text-base-content/70">{{ row.count | number }}</span>
          </span>
        </li>
      }
    </ul>
  `
})
export class BreakdownListComponent {
  public readonly data = input.required<GroupByReportResult[]>();
  public readonly label = input<string>('');

  protected readonly rows = computed(() => {
    const sorted = [...this.data()]
      .map(r => ({ label: `${r.label ?? '-'}`, count: r.count ?? 0 }))
      .sort((a, b) => b.count - a.count);
    const head = sorted.slice(0, MAX_ROWS).map(r => ({ ...r, other: false }));
    const rest = sorted.slice(MAX_ROWS);
    if (rest.length) {
      head.push({ label: '__other', count: rest.reduce((sum, r) => sum + r.count, 0), other: true });
    }
    // bars share one scale: the largest row fills ~85% of the track, leaving room for the value
    const max = Math.max(1, ...head.map(r => r.count));
    return head.map(r => ({ ...r, share: (r.count / max) * 85 }));
  });
}
