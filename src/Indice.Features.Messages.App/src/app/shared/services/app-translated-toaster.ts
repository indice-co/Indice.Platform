import { Injectable, inject } from '@angular/core';
import { ToastService, ToastType } from 'src/app/ui/toast/toast.service';

export { ToastType } from 'src/app/ui/toast/toast.service';

/**
 * Shows a toast whose title / body are translation keys (or plain text, e.g. a server provided `error.detail`).
 * Kept as the call-site friendly facade over {@link ToastService}.
 */
@Injectable({ providedIn: 'root' })
export class AppTranslatedToaster {
  private readonly _toasts = inject(ToastService);

  public show(type: ToastType, title?: string, body?: string, delay?: number, params?: Record<string, unknown>): void {
    this._toasts.show(type, title, body, delay, params);
  }
}
