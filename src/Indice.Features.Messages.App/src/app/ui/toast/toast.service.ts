import { Injectable, inject, signal } from '@angular/core';
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
 * App-wide toast notifications. `title` / `body` may be translation keys or plain text: ngx-translate returns
 * unknown keys unchanged, so server provided messages can be passed straight through.
 */
@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly _translate = inject(TranslateService);
  private _nextId = 1;
  public readonly toasts = signal<Toast[]>([]);

  public show(type: ToastType, title?: string, body?: string, delay = 6000, params?: Record<string, unknown>): void {
    const toast: Toast = {
      id: this._nextId++,
      type,
      title: title ? this._translate.instant(title, params) : undefined,
      body: body ? this._translate.instant(body, params) : undefined
    };
    this.toasts.update(list => [...list, toast]);
    setTimeout(() => this.dismiss(toast.id), delay);
  }

  public success(title?: string, body?: string): void {
    this.show(ToastType.Success, title, body);
  }

  public error(title?: string, body?: string): void {
    this.show(ToastType.Error, title, body);
  }

  public info(title?: string, body?: string): void {
    this.show(ToastType.Info, title, body);
  }

  public dismiss(id: number): void {
    this.toasts.update(list => list.filter(t => t.id !== id));
  }
}
