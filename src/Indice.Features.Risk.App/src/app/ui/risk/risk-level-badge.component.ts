import { Component, computed, input } from '@angular/core';

/** Risk level pill (API `RiskLevel`: None, Low, Medium, High) tinted with the `--app-risk-*` theme tokens. */
@Component({
  selector: 'app-risk-level-badge',
  host: { class: 'inline-flex' },
  template: `
    @if (level()) {
      <span class="badge gap-1.5 border-0 font-semibold whitespace-nowrap text-base-content" [class.badge-sm]="size() === 'sm'" [class.badge-lg]="size() === 'lg'"
        [style.background]="'color-mix(in oklab, ' + color() + ' 14%, transparent)'">
        <span class="risk-dot" [attr.data-level]="level()"></span>{{ level() }}
      </span>
    } @else {
      <span class="text-base-content/40">—</span>
    }
  `
})
export class RiskLevelBadgeComponent {
  public readonly level = input<string | undefined>();
  public readonly size = input<'sm' | 'md' | 'lg'>('sm');
  protected readonly color = computed(() => {
    switch (this.level()) {
      case 'Low': return 'var(--app-risk-low)';
      case 'Medium': return 'var(--app-risk-medium)';
      case 'High': return 'var(--app-risk-high)';
      default: return 'var(--app-risk-none)';
    }
  });
}
