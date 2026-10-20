import { Component, inject } from '@angular/core';
import { NgIcon } from '@ng-icons/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ToastService, ToastType } from './toast.service';

/** Renders the queued toasts. Errors are announced assertively, everything else politely. */
@Component({
  selector: 'app-toast-outlet',
  imports: [NgIcon, TranslatePipe],
  template: `
    <div class="toast toast-end toast-bottom z-[60] w-full max-w-sm p-4" aria-live="polite">
      @for (toast of toasts.toasts(); track toast.id) {
        <div class="alert alert-soft items-start border border-current/15 shadow-lg" [class]="alertClass[toast.type]"
             [attr.role]="toast.type === ToastType.Error ? 'alert' : 'status'">
          <ng-icon [name]="icon[toast.type]" size="1.25rem" class="mt-0.5" />
          <div class="min-w-0 text-left">
            @if (toast.title) {
              <p class="font-semibold">{{ toast.title }}</p>
            }
            @if (toast.body) {
              <p class="text-sm opacity-90">{{ toast.body }}</p>
            }
          </div>
          <button type="button" class="btn btn-ghost btn-xs btn-square -mr-1" (click)="toasts.dismiss(toast.id)"
                  [attr.aria-label]="'common.close' | translate">
            <ng-icon name="lucideX" size="1rem" />
          </button>
        </div>
      }
    </div>
  `
})
export class ToastOutletComponent {
  protected readonly toasts = inject(ToastService);
  protected readonly ToastType = ToastType;
  protected readonly alertClass: Record<ToastType, string> = {
    [ToastType.Success]: 'alert-success',
    [ToastType.Error]: 'alert-error',
    [ToastType.Info]: 'alert-info',
    [ToastType.Warning]: 'alert-warning'
  };
  protected readonly icon: Record<ToastType, string> = {
    [ToastType.Success]: 'lucideCircleCheck',
    [ToastType.Error]: 'lucideCircleX',
    [ToastType.Info]: 'lucideInfo',
    [ToastType.Warning]: 'lucideTriangleAlert'
  };
}
