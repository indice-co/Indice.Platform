import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { Callout, CalloutSeverity } from './part-contracts';

/**
 * Renders a callout part as a daisyUI alert — a disclaimer, a policy warning, or a caveat about the answer, set apart
 * from the prose it accompanies.
 *
 * The body is plain text with line breaks preserved, deliberately not markdown: prose typography inside an alert reads
 * as a second answer rather than as a notice.
 */
@Component({
  selector: 'app-chat-callout',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (callout(); as notice) {
      <div role="note" class="alert w-full items-start text-sm" [class]="alertClass()">
        <svg viewBox="0 0 24 24" fill="none" class="size-5 shrink-0" aria-hidden="true">
          <circle cx="12" cy="12" r="9" stroke="currentColor" stroke-width="2" />
          @if (notice.severity === 'success') {
            <path d="m8 12 3 3 5-6" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
          } @else if (notice.severity === 'info') {
            <path d="M12 11v5M12 8h.01" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
          } @else {
            <path d="M12 8v5M12 16h.01" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
          }
        </svg>
        <div class="min-w-0">
          @if (notice.title) {
            <div class="font-semibold">{{ notice.title }}</div>
          }
          <div class="whitespace-pre-line break-words">{{ notice.text }}</div>
        </div>
      </div>
    }
  `,
})
export class ChatCalloutComponent {
  /** The notice to render, or null when the part carried nothing renderable. */
  readonly callout = input<Callout | null>(null);

  protected readonly alertClass = computed(() => ALERT_CLASSES[this.callout()?.severity ?? 'info']);
}

/** Written out in full so Tailwind's scanner keeps every variant — a `alert-${severity}` template would be purged. */
const ALERT_CLASSES: Record<CalloutSeverity, string> = {
  info: 'alert-info',
  success: 'alert-success',
  warning: 'alert-warning',
  error: 'alert-error',
};
