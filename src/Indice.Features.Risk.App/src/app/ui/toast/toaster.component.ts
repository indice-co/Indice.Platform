import { Component, inject } from '@angular/core';
import { LucideCircleAlert, LucideCircleCheck, LucideInfo, LucideTriangleAlert, LucideX } from '@lucide/angular';
import { ToastService } from './toast.service';

@Component({
  selector: 'app-toaster',
  imports: [LucideCircleAlert, LucideCircleCheck, LucideInfo, LucideTriangleAlert, LucideX],
  template: `
    <div class="toast toast-end toast-bottom z-[100]" aria-live="polite">
      @for (toast of toasts.toasts(); track toast.id) {
        <div role="status" class="alert alert-soft w-80 items-start shadow-lg sm:w-96"
          [class.alert-success]="toast.type === 'success'" [class.alert-error]="toast.type === 'error'"
          [class.alert-info]="toast.type === 'info'" [class.alert-warning]="toast.type === 'warning'">
          @switch (toast.type) {
            @case ('success') { <svg lucideCircleCheck class="size-5 shrink-0"></svg> }
            @case ('error') { <svg lucideCircleAlert class="size-5 shrink-0"></svg> }
            @case ('warning') { <svg lucideTriangleAlert class="size-5 shrink-0"></svg> }
            @default { <svg lucideInfo class="size-5 shrink-0"></svg> }
          }
          <div class="min-w-0 whitespace-normal">
            @if (toast.title) { <p class="font-semibold">{{ toast.title }}</p> }
            @if (toast.body) { <p class="text-sm whitespace-pre-line opacity-90">{{ toast.body }}</p> }
          </div>
          <button type="button" class="btn btn-ghost btn-xs btn-circle" (click)="toasts.dismiss(toast.id)" aria-label="Dismiss">
            <svg lucideX class="size-3.5"></svg>
          </button>
        </div>
      }
    </div>
  `
})
export class ToasterComponent {
  protected readonly toasts = inject(ToastService);
}
