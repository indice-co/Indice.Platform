import { Component, input } from '@angular/core';
import { LucideCircleCheck, LucideCircleMinus } from '@lucide/angular';

/** Yes / no indicator for boolean table cells. */
@Component({
  selector: 'app-bool-icon',
  imports: [LucideCircleCheck, LucideCircleMinus],
  host: { class: 'inline-flex align-middle' },
  template: `
    @if (value()) {
      <svg lucideCircleCheck class="size-5 text-success" aria-label="Yes"></svg>
    } @else {
      <svg lucideCircleMinus class="size-5 text-base-content/30" aria-label="No"></svg>
    }
  `
})
export class BoolIconComponent {
  public readonly value = input<boolean | null | undefined>(false);
}

export type StatusTone = 'neutral' | 'primary' | 'secondary' | 'success' | 'warning' | 'error' | 'info';

/** Small soft pill with a leading dot, e.g. `<app-status-badge tone="error">Blocked</app-status-badge>`. */
@Component({
  selector: 'app-status-badge',
  host: { class: 'inline-flex' },
  template: `
    <span class="badge badge-sm badge-soft gap-1.5 font-medium whitespace-nowrap" [class]="toneClass()">
      <span class="size-1.5 rounded-full bg-current"></span><ng-content />
    </span>
  `
})
export class StatusBadgeComponent {
  public readonly tone = input<StatusTone>('neutral');

  protected toneClass(): string {
    return {
      neutral: 'badge-neutral',
      primary: 'badge-primary',
      secondary: 'badge-secondary',
      success: 'badge-success',
      warning: 'badge-warning',
      error: 'badge-error',
      info: 'badge-info'
    }[this.tone()];
  }
}
