import { Injectable, signal } from '@angular/core';

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

/** App-wide toast notifications. */
@Injectable({ providedIn: 'root' })
export class ToastService {
  private _nextId = 1;
  public readonly toasts = signal<Toast[]>([]);

  public show(type: ToastType, title?: string, body?: string, delay = 6000): void {
    const toast: Toast = { id: this._nextId++, type, title, body };
    this.toasts.update(list => [...list, toast]);
    setTimeout(() => this.dismiss(toast.id), delay);
  }

  public success(title?: string, body?: string): void {
    this.show(ToastType.Success, title, body);
  }

  public error(title?: string, body?: string): void {
    this.show(ToastType.Error, title, body, 8000);
  }

  public info(title?: string, body?: string): void {
    this.show(ToastType.Info, title, body);
  }

  public warning(title?: string, body?: string): void {
    this.show(ToastType.Warning, title, body);
  }

  public dismiss(id: number): void {
    this.toasts.update(list => list.filter(t => t.id !== id));
  }
}
