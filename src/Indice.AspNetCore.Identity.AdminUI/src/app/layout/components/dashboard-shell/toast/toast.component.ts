import { Component, TemplateRef } from '@angular/core';

import { ToastService } from '../../../services/app-toast.service';

@Component({
    selector: 'app-toasts',
    templateUrl: './toast.component.html',
    styleUrls: ['./toast.component.scss'],
    host: { '[class.ngb-toasts]': 'true' }
})
export class AppToastsComponent {
    constructor(public toastService: ToastService) { }

    public isTemplate(toast: any): boolean {
        return toast.textOrTpl instanceof TemplateRef;
    }
}
