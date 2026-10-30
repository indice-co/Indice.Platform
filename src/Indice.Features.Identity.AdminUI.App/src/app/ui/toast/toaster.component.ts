import { Component, inject } from '@angular/core';
import { LucideCircleAlert, LucideCircleCheck, LucideInfo, LucideTriangleAlert, LucideX } from '@lucide/angular';
import { TranslatePipe } from '@ngx-translate/core';
import { ToastService, ToastType } from './toast.service';

@Component({
  selector: 'app-toaster',
  imports: [LucideCircleAlert, LucideCircleCheck, LucideInfo, LucideTriangleAlert, LucideX, TranslatePipe],
  template: `
    <div class="toast toast-end toast-bottom z-[100]" aria-live="polite">
      @for (toast of toasts.toasts(); track toast.id) {
        <div role="status" class="alert alert-soft w-80 items-start shadow-lg sm:w-96"
          [class.alert-success]="toast.type === types.Success" [class.alert-error]="toast.type === types.Error"
          [class.alert-info]="toast.type === types.Info" [class.alert-warning]="toast.type === types.Warning">
          @switch (toast.type) {
            @case (types.Success) { <svg lucideCircleCheck class="size-5 shrink-0"></svg> }
            @case (types.Error) { <svg lucideCircleAlert class="size-5 shrink-0"></svg> }
            @case (types.Warning) { <svg lucideTriangleAlert class="size-5 shrink-0"></svg> }
            @default { <svg lucideInfo class="size-5 shrink-0"></svg> }
          }
          <div class="min-w-0 whitespace-normal">
            @if (toast.title) { <p class="font-semibold">{{ toast.title }}</p> }
            @if (toast.body) { <p class="text-sm break-words whitespace-pre-line opacity-90">{{ toast.body }}</p> }
          </div>
          <button type="button" class="btn btn-ghost btn-xs btn-circle" (click)="toasts.dismiss(toast.id)" [attr.aria-label]="'Ui.Toast.Dismiss' | translate">
            <svg lucideX class="size-3.5"></svg>
          </button>
        </div>
      }
    </div>
  `
})
export class ToasterComponent {
  protected readonly toasts = inject(ToastService);
  protected readonly types = ToastType;
}
