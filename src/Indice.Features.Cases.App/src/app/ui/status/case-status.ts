import { Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { CaseStatus } from 'src/app/core/services/cases-api.service';

/**
 * The case lifecycle is an ordered scale (Submitted → In progress → Completed, plus Deleted), drawn as ink that gets
 * denser as a case moves on. It deliberately does not use success / warning / error: completing a case is not "good"
 * (it can complete rejected) and those colours stay reserved for real outcomes.
 */
export const CASE_STATUS_INK: Record<CaseStatus, string> = {
  [CaseStatus.Submitted]: 'ink-submitted',
  [CaseStatus.InProgress]: 'ink-inprogress',
  [CaseStatus.Completed]: 'ink-completed',
  [CaseStatus.Deleted]: 'ink-deleted'
};

/** The lifecycle states in their natural order. */
export const CASE_STATUS_ORDER: CaseStatus[] = [CaseStatus.Submitted, CaseStatus.InProgress, CaseStatus.Completed, CaseStatus.Deleted];

export function inkOf(status: CaseStatus | string | null | undefined): string {
  return CASE_STATUS_INK[status as CaseStatus] ?? '';
}

export function statusLabelKey(status: CaseStatus | string | null | undefined): string {
  return `status.${status ?? 'unknown'}`;
}

/** A checkpoint (or bare status) as a dot of lifecycle ink and a label; used in tables and lists. */
@Component({
  selector: 'app-status-mark',
  imports: [TranslatePipe],
  template: `
    <span class="ink-dot"></span>
    <span class="min-w-0 truncate">{{ label() || (labelKey() | translate) }}</span>
  `,
  host: { class: 'inline-flex max-w-full items-center gap-2', '[class]': 'ink()' }
})
export class StatusMarkComponent {
  public readonly status = input<CaseStatus | string | null | undefined>();
  /** The checkpoint title; falls back to the translated status. */
  public readonly label = input<string | null | undefined>();
  protected readonly ink = computed(() => inkOf(this.status()));
  protected readonly labelKey = computed(() => statusLabelKey(this.status()));
}

/** The stamp: the case's current checkpoint as an ink impression in the lifecycle colour. */
@Component({
  selector: 'app-status-stamp',
  imports: [TranslatePipe],
  template: `{{ label() || (labelKey() | translate) }}`,
  host: {
    class: 'stamp',
    '[class]': 'ink()',
    '[class.stamp-sm]': 'small()',
    '[attr.title]': 'tooltip()'
  }
})
export class StatusStampComponent {
  public readonly status = input<CaseStatus | string | null | undefined>();
  public readonly label = input<string | null | undefined>();
  public readonly small = input(false);
  /** Extra context shown on hover (e.g. "Public status"). */
  public readonly tooltip = input<string | null | undefined>();
  protected readonly ink = computed(() => inkOf(this.status()));
  protected readonly labelKey = computed(() => statusLabelKey(this.status()));
}
