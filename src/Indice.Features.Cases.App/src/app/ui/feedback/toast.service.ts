import { Injectable, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

export enum ToastType {
  Success = 'success',
  Error = 'error',
  Info = 'info',
  Warning = 'warning'
}

export interface Toast {
  id: number;
  type: ToastType;
  title?: string;
  body?: string;
}

/**
 * Transient notifications shown in the bottom-right corner (see `ToastOutletComponent`).
 * `titleKey` / `bodyKey` are translation keys; plain text (e.g. a server `ProblemDetails.detail`) passes through unchanged,
 * because ngx-translate returns unknown keys as-is.
 */
@Injectable({ providedIn: 'root' })
export class ToastService {
  private nextId = 1;
  public readonly toasts = signal<Toast[]>([]);

  constructor(private readonly translate: TranslateService) { }

  public show(type: ToastType, titleKey?: string, bodyKey?: string, delay = 5000, params?: object): void {
    const toast: Toast = {
      id: this.nextId++,
      type,
      title: titleKey ? this.translate.instant(titleKey, params) : undefined,
      body: bodyKey ? this.translate.instant(bodyKey, params) : undefined
    };
    this.toasts.update(toasts => [...toasts.slice(-3), toast]);
    if (delay > 0) {
      setTimeout(() => this.dismiss(toast.id), delay);
    }
  }

  public dismiss(id: number): void {
    this.toasts.update(toasts => toasts.filter(t => t.id !== id));
  }
}
