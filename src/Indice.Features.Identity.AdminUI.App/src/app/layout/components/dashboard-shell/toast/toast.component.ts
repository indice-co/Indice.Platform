import { Component, TemplateRef, ChangeDetectionStrategy } from '@angular/core';

import { ToastService } from '../../../services/app-toast.service';

@Component({
    selector: 'app-toasts',
    templateUrl: './toast.component.html',
    styleUrls: ['./toast.component.scss'],
    host: { '[class.ngb-toasts]': 'true' },
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class AppToastsComponent {
    constructor(public toastService: ToastService) { }

    public isTemplate(toast: any): boolean {
        return toast.textOrTpl instanceof TemplateRef;
    }
}
