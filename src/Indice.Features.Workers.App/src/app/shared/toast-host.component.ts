import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { LucideCircleCheck, LucideCircleX, LucideX } from '@lucide/angular';

import { ToastService } from '../core/services/toast.service';

/** Renders `ToastService` notifications bottom-right. The shell renders it once. */
@Component({
  selector: 'app-toast-host',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideCircleCheck, LucideCircleX, LucideX],
  template: `
    <div class="toast toast-end z-50" aria-live="polite">
      @for (toast of toasts.toasts(); track toast.id) {
        <div
          role="status"
          class="alert max-w-sm shadow-lg"
          [class.alert-success]="toast.kind === 'success'"
          [class.alert-error]="toast.kind === 'error'"
        >
          @if (toast.kind === 'error') {
            <svg lucideCircleX class="size-5 shrink-0" aria-hidden="true"></svg>
          } @else {
            <svg lucideCircleCheck class="size-5 shrink-0" aria-hidden="true"></svg>
          }
          <span class="text-sm">{{ toast.text }}</span>
          <button
            type="button"
            class="btn btn-ghost btn-xs btn-circle"
            (click)="toasts.dismiss(toast.id)"
            aria-label="Dismiss"
          >
            <svg lucideX class="size-3.5"></svg>
          </button>
        </div>
      }
    </div>
  `,
})
export class ToastHostComponent {
  protected readonly toasts = inject(ToastService);
}
