import { Injectable, TemplateRef } from '@angular/core';

@Injectable({
    providedIn: 'root'
})
export class ToastService {
    public toasts: any[] = [];

    public showStandard(textOrTemplate: string | TemplateRef<any>, delay?: number): void {
        this.show(textOrTemplate, { delay: delay || 5000 });
    }

    public showSuccess(textOrTemplate: string | TemplateRef<any>, delay?: number): void {
        this.show(textOrTemplate, { classname: 'bg-success text-light', delay: delay || 5000 });
    }

    public showDanger(textOrTemplate: string | TemplateRef<any>, delay?: number): void {
        this.show(textOrTemplate, { classname: 'bg-danger text-light', delay: delay || 5000 });
    }

    public remove(toast: any): void {
        this.toasts = this.toasts.filter(x => x !== toast);
    }

    private show(textOrTpl: string | TemplateRef<any>, options: any = {}): void {
        this.toasts.push({ textOrTpl, ...options });
    }
}
