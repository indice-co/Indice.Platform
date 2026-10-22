import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { ToastService } from '../core/services/toast.service';

/** Renders `ToastService` notifications bottom-right. The shell renders it once. */
@Component({
  selector: 'app-toast-host',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="toast toast-end z-50" aria-live="polite">
      @for (toast of toasts.toasts(); track toast.id) {
        <div
          role="status"
          class="alert"
          [class.alert-success]="toast.kind === 'success'"
          [class.alert-error]="toast.kind === 'error'"
        >
          <span>{{ toast.text }}</span>
          <button
            type="button"
            class="btn btn-ghost btn-xs btn-circle"
            (click)="toasts.dismiss(toast.id)"
            aria-label="Dismiss"
          >
            &#10005;
          </button>
        </div>
      }
    </div>
  `,
})
export class ToastHostComponent {
  protected readonly toasts = inject(ToastService);
}
